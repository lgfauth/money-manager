using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Application.Validators;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Exceptions;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

/// <summary>
/// Importação de fatura pela API: cria o job (o Worker processa), consulta, lista e descarta.
/// A validação dos arquivos é feita antes, no controller, com <see cref="CreateInvoiceImportValidator"/>.
/// </summary>
public interface ICreditCardInvoiceImportService
{
    /// <summary>
    /// Grava os arquivos e o job em <c>Queued</c>. Lança <see cref="KeyNotFoundException"/> se o cartão não
    /// for do usuário e <see cref="InvoiceImportConflictException"/> se o cartão for sincronizado pelo
    /// Open Finance ou já tiver uma importação em andamento.
    /// </summary>
    Task<InvoiceImportCreatedResponseDto> CreateAsync(string userId, CreateInvoiceImportRequestDto request);

    /// <summary>Importação do usuário; <see cref="KeyNotFoundException"/> se não existir ou for de outro usuário.</summary>
    Task<CreditCardInvoiceImportResponseDto> GetByIdAsync(string userId, string importId);

    /// <summary>Importações recentes do usuário, da mais nova para a mais antiga.</summary>
    Task<IReadOnlyList<CreditCardInvoiceImportResponseDto>> GetRecentAsync(string userId);

    /// <summary>Cartões do usuário que podem receber importação.</summary>
    Task<IReadOnlyList<InvoiceImportEligibleCardResponseDto>> GetEligibleCardsAsync(string userId);

    /// <summary>
    /// Descarta a importação (só em <c>AwaitingReview</c> ou <c>Failed</c>), sem criar transações.
    /// Outros estados lançam <see cref="InvoiceImportConflictException"/>.
    /// </summary>
    Task DiscardAsync(string userId, string importId);

    /// <summary>
    /// Confirma a importação: revalida titulares, itens, cadastro do cartão e duplicidade, cadastra os itens
    /// escolhidos (origem <c>invoice_import</c>) na fatura do mês importado e marca o job <c>Confirmed</c>.
    /// Itens já importados (fingerprint) são pulados, então repetir após uma falha não duplica nada.
    /// </summary>
    Task<CreditCardInvoiceImportResponseDto> ConfirmAsync(string userId, string importId, ConfirmInvoiceImportRequestDto request);
}

public class CreditCardInvoiceImportService : ICreditCardInvoiceImportService
{
    public const int RecentLimit = 20;

    public const string CardNotFoundMessage = "Cartão não encontrado.";
    public const string ImportNotFoundMessage = "Importação não encontrada.";
    public const string ActiveImportMessage =
        "Já existe uma fatura deste cartão em processamento. Aguarde a conclusão antes de enviar outra.";
    public const string CannotDiscardMessage =
        "Esta importação não pode ser descartada no estado atual.";
    public const string NotAwaitingReviewMessage =
        "Esta importação não está aguardando revisão; ela pode já ter sido confirmada ou estar em confirmação.";
    public const string HolderRequiredMessage =
        "Escolha de quais titulares os lançamentos serão importados.";
    public const string InvalidItemsMessage =
        "Alguns lançamentos selecionados precisam de correção antes da importação.";

    // Datas da fatura são do calendário brasileiro
    private const string BrazilTimeZoneId = "E. South America Standard Time";

    // Códigos recalculados por IInvoiceImportReviewService.ValidateItem após as edições do usuário
    private static readonly HashSet<InvoiceImportWarningCode> ItemValidationCodes =
    [
        InvoiceImportWarningCode.MissingDate,
        InvoiceImportWarningCode.MissingDescription,
        InvoiceImportWarningCode.InvalidAmount,
        InvoiceImportWarningCode.FutureDate,
        InvoiceImportWarningCode.OutOfAllowedPeriod
    ];

    private static readonly HashSet<InvoiceImportStatus> DiscardableStatuses =
        [InvoiceImportStatus.AwaitingReview, InvoiceImportStatus.Failed];

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICreditCardImportEligibilityService _eligibility;
    private readonly IInvoiceImportReviewService _review;
    private readonly IImageNormalizationService _imageNormalization;
    private readonly ISpreadsheetTextExtractor _spreadsheetExtractor;
    private readonly ICreditCardTransactionService _transactions;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CreditCardInvoiceImportService> _logger;

    public CreditCardInvoiceImportService(
        IUnitOfWork unitOfWork,
        ICreditCardImportEligibilityService eligibility,
        IInvoiceImportReviewService review,
        ICreditCardTransactionService transactions,
        IImageNormalizationService imageNormalization,
        ISpreadsheetTextExtractor spreadsheetExtractor,
        TimeProvider timeProvider,
        ILogger<CreditCardInvoiceImportService> logger)
    {
        _unitOfWork = unitOfWork;
        _eligibility = eligibility;
        _review = review;
        _transactions = transactions;
        _imageNormalization = imageNormalization;
        _spreadsheetExtractor = spreadsheetExtractor;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<InvoiceImportCreatedResponseDto> CreateAsync(string userId, CreateInvoiceImportRequestDto request)
    {
        if (!ObjectId.TryParse(request.CreditCardId, out _))
            throw new KeyNotFoundException(CardNotFoundMessage);

        var card = await _unitOfWork.CreditCards.GetByIdAsync(request.CreditCardId);
        if (card is null || card.IsDeleted || card.UserId != userId)
            throw new KeyNotFoundException(CardNotFoundMessage);

        if (await _eligibility.IsSyncedByOpenFinanceAsync(userId, card.Id))
            throw new InvoiceImportConflictException(CreditCardImportEligibilityService.SyncedCardMessage);

        if (await _unitOfWork.CreditCardInvoiceImports.GetActiveByCardAsync(userId, card.Id) is not null)
            throw new InvoiceImportConflictException(ActiveImportMessage);

        var sourceType = CreateInvoiceImportValidator.ResolveSourceType(request.Files[0])
                         ?? throw new InvalidOperationException(CreateInvoiceImportValidator.UnsupportedTypeMessage);

        var now = DateTime.UtcNow;
        var job = new CreditCardInvoiceImport
        {
            UserId = userId,
            CreditCardId = card.Id,
            Status = InvoiceImportStatus.Queued,
            SourceType = sourceType,
            FileCount = request.Files.Count,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Imagens: orientação, redução e JPEG ≤ 5 MB; planilha: só o texto convertido (o .xlsx não é gravado);
        // PDF é gravado como veio
        var files = request.Files
            .Select((f, index) =>
            {
                var (contentType, content) = sourceType switch
                {
                    InvoiceImportSourceType.Images => Normalize(f.Content),
                    InvoiceImportSourceType.Spreadsheet => ConvertSpreadsheet(f.Content),
                    _ => (CreateInvoiceImportValidator.NormalizeContentType(f.ContentType), f.Content)
                };

                return new CreditCardInvoiceImportFile
                {
                    UserId = userId,
                    ImportId = job.Id,
                    Order = index,
                    ContentType = contentType,
                    SizeBytes = content.LongLength,
                    Content = content,
                    CreatedAt = now
                };
            })
            .ToList();

        // Arquivos antes do job: o Worker nunca reserva um job cujos arquivos ainda não foram gravados.
        // Se o job falhar ao gravar, os arquivos órfãos são removidos (e o TTL de 24 h é a garantia final).
        await _unitOfWork.CreditCardInvoiceImportFiles.AddManyAsync(files);
        try
        {
            await _unitOfWork.CreditCardInvoiceImports.AddAsync(job);
        }
        catch
        {
            await DeleteFilesQuietlyAsync(userId, job.Id);
            throw;
        }

        _logger.LogInformation(
            "Importação de fatura {ImportId} criada para o usuário {UserId}: origem {SourceType}, {FileCount} arquivo(s), {TotalBytes} bytes",
            job.Id, userId, job.SourceType, job.FileCount, files.Sum(f => f.SizeBytes));

        return new InvoiceImportCreatedResponseDto { Id = job.Id, Status = job.Status.ToString() };
    }

    private (string ContentType, byte[] Content) ConvertSpreadsheet(byte[] content)
    {
        var spreadsheet = _spreadsheetExtractor.Extract(content);
        return (SpreadsheetText.ContentType, System.Text.Encoding.UTF8.GetBytes(SpreadsheetText.Format(spreadsheet)));
    }

    private (string ContentType, byte[] Content) Normalize(byte[] content)
    {
        var image = _imageNormalization.Normalize(content);
        return (image.ContentType, image.Content);
    }

    public async Task<CreditCardInvoiceImportResponseDto> GetByIdAsync(string userId, string importId)
    {
        var job = await _unitOfWork.CreditCardInvoiceImports.GetByIdForUserAsync(userId, importId)
                  ?? throw new KeyNotFoundException(ImportNotFoundMessage);
        return MapToDto(job);
    }

    public async Task<IReadOnlyList<CreditCardInvoiceImportResponseDto>> GetRecentAsync(string userId)
    {
        var jobs = await _unitOfWork.CreditCardInvoiceImports.GetRecentByUserAsync(userId, RecentLimit);
        return jobs
            .Where(j => j.UserId == userId && !j.IsDeleted)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<IReadOnlyList<InvoiceImportEligibleCardResponseDto>> GetEligibleCardsAsync(string userId)
    {
        var cards = await _eligibility.GetEligibleCardsAsync(userId);
        return cards
            .Select(c => new InvoiceImportEligibleCardResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Brand = c.Brand,
                Color = c.Color,
                ClosingDay = c.ClosingDay,
                BillingDueDay = c.BillingDueDay
            })
            .ToList();
    }

    public async Task DiscardAsync(string userId, string importId)
    {
        var job = await _unitOfWork.CreditCardInvoiceImports.GetByIdForUserAsync(userId, importId)
                  ?? throw new KeyNotFoundException(ImportNotFoundMessage);

        if (!DiscardableStatuses.Contains(job.Status))
            throw new InvoiceImportConflictException(CannotDiscardMessage);

        job.Status = InvoiceImportStatus.Discarded;
        job.IsDeleted = true;
        job.UpdatedAt = DateTime.UtcNow;

        try
        {
            // Versão otimista: se o Worker ou outra aba mudou o job no meio, nada é sobrescrito
            await _unitOfWork.CreditCardInvoiceImports.UpdateAsync(job);
        }
        catch (ConcurrencyException)
        {
            throw new InvoiceImportConflictException(CannotDiscardMessage);
        }

        // Em AwaitingReview/Failed os arquivos já foram apagados; a remoção aqui só cobre sobras
        await DeleteFilesQuietlyAsync(userId, job.Id);

        _logger.LogInformation("Importação de fatura {ImportId} descartada pelo usuário {UserId}", job.Id, userId);
    }

    public async Task<CreditCardInvoiceImportResponseDto> ConfirmAsync(
        string userId, string importId, ConfirmInvoiceImportRequestDto request)
    {
        var job = await _unitOfWork.CreditCardInvoiceImports.GetByIdForUserAsync(userId, importId)
                  ?? throw new KeyNotFoundException(ImportNotFoundMessage);

        if (job.Status != InvoiceImportStatus.AwaitingReview)
            throw new InvoiceImportConflictException(NotAwaitingReviewMessage);

        if (job.Invoice?.ClosingDate is not { } closingDate || string.IsNullOrEmpty(job.ReferenceMonth))
            throw new InvoiceImportConflictException(NotAwaitingReviewMessage);

        // 1. Cartão atual: do usuário, não excluído, não sincronizado e com fechamento/vencimento compatíveis
        var card = await _unitOfWork.CreditCards.GetByIdAsync(job.CreditCardId);
        if (card is null || card.IsDeleted || card.UserId != userId)
            throw new KeyNotFoundException(CardNotFoundMessage);

        if (await _eligibility.IsSyncedByOpenFinanceAsync(userId, card.Id))
            throw new InvoiceImportConflictException(CreditCardImportEligibilityService.SyncedCardMessage);

        var blockingSchedule = _review.CheckCardSchedule(job.Invoice, card).Where(w => w.IsBlocking).ToList();
        if (blockingSchedule.Count > 0)
            throw new InvoiceImportConflictException(string.Join(" ", blockingSchedule.Select(w => w.Message)));

        // 2. Titulares
        var holderNames = job.Holders.Select(h => h.Name).ToList();
        var selectedHolders = ResolveSelectedHolders(holderNames, request.SelectedHolders ?? []);
        bool IsIncluded(InvoiceImportItem item) =>
            selectedHolders is null
            || selectedHolders.Contains(item.HolderName ?? InvoiceImportReviewService.UnidentifiedHolderName);

        // 3. Itens com as edições do usuário
        var working = job.Items.Select(CloneItem).ToList();
        var byId = working.ToDictionary(i => i.Id, StringComparer.Ordinal);
        var errors = new List<string>();
        var chosen = new List<InvoiceImportItem>();

        foreach (var edit in request.Items)
        {
            if (!byId.TryGetValue(edit.Id, out var item))
            {
                errors.Add("Um dos lançamentos enviados não pertence a esta importação.");
                continue;
            }

            ApplyEdit(item, edit);
            if (IsIncluded(item))
                chosen.Add(item);
        }

        await ValidateCategoriesAsync(userId, chosen, errors);
        if (errors.Count > 0)
            throw new InvoiceImportValidationException(InvalidItemsMessage, errors.Distinct().ToList());

        // 4. Revalidação das regras de item e da duplicidade contra os dados atuais
        var today = GetBrazilToday();
        foreach (var item in working)
        {
            item.Warnings.RemoveAll(ItemValidationCodes.Contains);
            item.Warnings.AddRange(_review.ValidateItem(item, closingDate, today));
        }

        await _review.ApplyDuplicateChecksAsync(
            userId, card, job.ReferenceMonth, working, selectedHolders?.ToList());

        var toCreate = new List<InvoiceImportItem>();
        var skipped = 0;
        foreach (var item in chosen)
        {
            if (item.Warnings.Contains(InvoiceImportWarningCode.AlreadyImported))
            {
                skipped++;
                continue;
            }

            var blocking = item.Warnings.Where(InvoiceImportReviewService.IsBlocking).ToList();
            if (blocking.Count > 0)
                errors.Add($"{DescribeItem(item)}: {string.Join("; ", blocking.Select(DescribeWarning))}.");
            else
                toCreate.Add(item);
        }

        if (errors.Count > 0)
            throw new InvoiceImportValidationException(InvalidItemsMessage, errors);

        // 5. Troca atômica AwaitingReview → Confirming (versão otimista): duplo clique ou outra aba recebem 409
        job.Status = InvoiceImportStatus.Confirming;
        job.UpdatedAt = DateTime.UtcNow;
        try
        {
            await _unitOfWork.CreditCardInvoiceImports.UpdateAsync(job);
        }
        catch (ConcurrencyException)
        {
            throw new InvoiceImportConflictException(NotAwaitingReviewMessage);
        }

        // 6. Cadastro
        var affectedInvoices = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (var item in toCreate)
            {
                var created = await _transactions.CreateAsync(userId, BuildTransactionRequest(job, item));
                foreach (var tx in created)
                    affectedInvoices.Add(tx.InvoiceId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao cadastrar os lançamentos da importação {ImportId}; ela volta para revisão",
                job.Id);
            await RevertToReviewAsync(job);
            throw;
        }

        job.Status = InvoiceImportStatus.Confirmed;
        job.ConfirmedAt = DateTime.UtcNow;
        job.UpdatedAt = job.ConfirmedAt.Value;
        job.SelectedHolders = selectedHolders?.ToList() ?? holderNames;
        job.Items = working;
        job.Result = new InvoiceImportConfirmationResult
        {
            CreatedCount = toCreate.Count,
            SkippedCount = skipped,
            AffectedInvoiceIds = affectedInvoices.OrderBy(id => id, StringComparer.Ordinal).ToList()
        };
        await _unitOfWork.CreditCardInvoiceImports.UpdateAsync(job);

        _logger.LogInformation(
            "Importação de fatura {ImportId} confirmada pelo usuário {UserId}: {CreatedCount} lançamento(s) criado(s), {SkippedCount} já importado(s), {InvoiceCount} fatura(s) afetada(s)",
            job.Id, userId, toCreate.Count, skipped, affectedInvoices.Count);

        return MapToDto(job);
    }

    /// <summary>
    /// Titulares incluídos: null quando a fatura tem no máximo um titular (todos os itens entram).
    /// </summary>
    private static HashSet<string>? ResolveSelectedHolders(List<string> holderNames, List<string> requested)
    {
        if (holderNames.Count <= 1)
            return null;

        if (requested.Count == 0)
            throw new InvoiceImportValidationException(HolderRequiredMessage);

        var unknown = requested.Where(r => !holderNames.Contains(r, StringComparer.Ordinal)).ToList();
        if (unknown.Count > 0)
            throw new InvoiceImportValidationException(
                "Um dos titulares escolhidos não pertence a esta fatura.", unknown);

        return requested.ToHashSet(StringComparer.Ordinal);
    }

    private async Task ValidateCategoriesAsync(string userId, List<InvoiceImportItem> items, List<string> errors)
    {
        if (!items.Any(i => !string.IsNullOrEmpty(i.CategoryId)))
            return;

        var categoryIds = (await _unitOfWork.Categories.GetByUserIdAsync(userId))
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .Select(c => c.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var item in items.Where(i => !string.IsNullOrEmpty(i.CategoryId) && !categoryIds.Contains(i.CategoryId!)))
            errors.Add($"{DescribeItem(item)}: categoria não encontrada.");
    }

    private static void ApplyEdit(InvoiceImportItem item, ConfirmInvoiceImportItemRequestDto edit)
    {
        item.Date = edit.Date?.Date;
        item.Description = (edit.Description ?? string.Empty).Trim();
        item.Amount = edit.Amount;
        item.CategoryId = string.IsNullOrWhiteSpace(edit.CategoryId) ? null : edit.CategoryId;

        // "1 de 1" ou sem parcelas: compra à vista
        if (edit.TotalInstallments is > 1 && edit.InstallmentNumber is { } number)
        {
            item.InstallmentNumber = number;
            item.TotalInstallments = edit.TotalInstallments;
        }
        else
        {
            item.InstallmentNumber = null;
            item.TotalInstallments = null;
        }
    }

    private static CreateCreditCardTransactionRequestDto BuildTransactionRequest(CreditCardInvoiceImport job, InvoiceImportItem item)
    {
        var isRefund = item.Type == CreditCardTransactionType.Refund;
        var totalInstallments = isRefund ? 1 : item.TotalInstallments ?? 1;
        var amount = Math.Abs(item.Amount);

        return new CreateCreditCardTransactionRequestDto
        {
            CreditCardId = job.CreditCardId,
            Description = item.Description,
            CategoryId = item.CategoryId,
            PurchaseDate = DateTime.SpecifyKind(item.Date!.Value.Date, DateTimeKind.Utc),
            // O valor da fatura é o da parcela; o total da compra é parcela × N
            TotalAmount = amount * totalInstallments,
            TotalInstallments = totalInstallments,
            IsRefund = isRefund,
            Source = CreditCardTransactionService.InvoiceImportSource,
            TargetReferenceMonth = job.ReferenceMonth,
            // 1/N nova → parcelamento completo; X/N → só X..N, nada em fatura anterior
            FirstInstallmentNumber = isRefund ? 1 : item.InstallmentNumber ?? 1,
            InvoiceImportId = job.Id,
            ImportFingerprint = item.Fingerprint
        };
    }

    private async Task RevertToReviewAsync(CreditCardInvoiceImport job)
    {
        try
        {
            job.Status = InvoiceImportStatus.AwaitingReview;
            job.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CreditCardInvoiceImports.UpdateAsync(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Não foi possível devolver a importação {ImportId} para revisão", job.Id);
        }
    }

    private DateOnly GetBrazilToday()
    {
        var brazilNow = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(BrazilTimeZoneId));
        return DateOnly.FromDateTime(brazilNow.DateTime);
    }

    private static string DescribeItem(InvoiceImportItem item)
    {
        var description = string.IsNullOrWhiteSpace(item.Description) ? "Lançamento sem descrição" : item.Description;
        return item.Date is { } date ? $"{description} ({date:dd/MM})" : description;
    }

    private static string DescribeWarning(InvoiceImportWarningCode code) => code switch
    {
        InvoiceImportWarningCode.MissingDate => "informe a data",
        InvoiceImportWarningCode.MissingDescription => "informe a descrição",
        InvoiceImportWarningCode.InvalidAmount => "o valor deve ser maior que zero",
        InvoiceImportWarningCode.FutureDate => "a data não pode ser futura nem posterior ao fechamento da fatura",
        InvoiceImportWarningCode.OutOfAllowedPeriod =>
            $"compras à vista ou 1ª parcela com mais de {InvoiceImportReviewService.MaxDaysInPast} dias não podem ser importadas",
        InvoiceImportWarningCode.NotFoundInSpreadsheet => "o lançamento não foi encontrado na planilha",
        _ => "o lançamento precisa de correção"
    };

    private static InvoiceImportItem CloneItem(InvoiceImportItem i) => new()
    {
        Id = i.Id,
        Date = i.Date,
        Description = i.Description,
        Amount = i.Amount,
        Type = i.Type,
        InstallmentNumber = i.InstallmentNumber,
        TotalInstallments = i.TotalInstallments,
        HolderName = i.HolderName,
        HolderType = i.HolderType,
        CardLastDigits = i.CardLastDigits,
        CardType = i.CardType,
        Source = i.Source,
        CategoryHint = i.CategoryHint,
        CategoryId = i.CategoryId,
        Confidence = i.Confidence,
        Notes = i.Notes,
        Warnings = [.. i.Warnings],
        IsSelectedByDefault = i.IsSelectedByDefault,
        Fingerprint = i.Fingerprint
    };

    private async Task DeleteFilesQuietlyAsync(string userId, string importId)
    {
        try
        {
            await _unitOfWork.CreditCardInvoiceImportFiles.DeleteByImportAsync(userId, importId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Não foi possível remover os arquivos da importação {ImportId}; o TTL de 24 horas fará a limpeza",
                importId);
        }
    }

    internal static CreditCardInvoiceImportResponseDto MapToDto(CreditCardInvoiceImport job) => new()
    {
        Id = job.Id,
        CreditCardId = job.CreditCardId,
        Status = job.Status.ToString(),
        SourceType = job.SourceType.ToString(),
        FileCount = job.FileCount,
        ErrorMessage = job.ErrorMessage,
        ReferenceMonth = job.ReferenceMonth,
        Invoice = job.Invoice is null
            ? null
            : new InvoiceImportDocumentInfoResponseDto
            {
                Issuer = job.Invoice.Issuer,
                CardLastDigits = job.Invoice.CardLastDigits,
                ClosingDate = job.Invoice.ClosingDate,
                ClosingDateCalculated = job.Invoice.ClosingDateCalculated,
                DueDate = job.Invoice.DueDate,
                ReferenceMonth = job.Invoice.ReferenceMonth,
                TotalAmount = job.Invoice.TotalAmount,
                IsOpenOrPartial = job.Invoice.IsOpenOrPartial
            },
        Holders = job.Holders
            .Select(h => new InvoiceImportHolderResponseDto
            {
                Name = h.Name,
                Type = h.Type.ToString(),
                CardLastDigits = [.. h.CardLastDigits],
                ItemCount = h.ItemCount,
                TotalAmount = h.TotalAmount,
                IsSuggested = h.IsSuggested
            })
            .ToList(),
        Warnings = job.Warnings
            .Select(w => new InvoiceImportWarningResponseDto
            {
                Code = w.Code.ToString(),
                Message = w.Message,
                IsBlocking = w.IsBlocking
            })
            .ToList(),
        Items = job.Items.Select(MapItem).ToList(),
        SelectedHolders = [.. job.SelectedHolders],
        Result = job.Result is null
            ? null
            : new InvoiceImportConfirmationResultResponseDto
            {
                CreatedCount = job.Result.CreatedCount,
                SkippedCount = job.Result.SkippedCount,
                AffectedInvoiceIds = [.. job.Result.AffectedInvoiceIds]
            },
        CreatedAt = job.CreatedAt,
        UpdatedAt = job.UpdatedAt,
        ProcessedAt = job.ProcessedAt,
        ConfirmedAt = job.ConfirmedAt,
        Version = job.Version
    };

    private static InvoiceImportItemResponseDto MapItem(InvoiceImportItem item) => new()
    {
        Id = item.Id,
        Date = item.Date,
        Description = item.Description,
        Amount = item.Amount,
        Type = item.Type.ToString(),
        InstallmentNumber = item.InstallmentNumber,
        TotalInstallments = item.TotalInstallments,
        HolderName = item.HolderName,
        HolderType = item.HolderType.ToString(),
        CardLastDigits = item.CardLastDigits,
        CardType = item.CardType.ToString(),
        Source = item.Source is null
            ? null
            : new InvoiceImportSourceResponseDto
            {
                FileIndex = item.Source.FileIndex,
                Page = item.Source.Page,
                Sheet = item.Source.Sheet,
                Row = item.Source.Row
            },
        CategoryHint = item.CategoryHint,
        CategoryId = item.CategoryId,
        Confidence = item.Confidence,
        Notes = item.Notes,
        Warnings = item.Warnings
            .Select(code => new InvoiceImportItemWarningResponseDto
            {
                Code = code.ToString(),
                IsBlocking = InvoiceImportReviewService.IsBlocking(code)
            })
            .ToList(),
        IsSelectedByDefault = item.IsSelectedByDefault
    };
}
