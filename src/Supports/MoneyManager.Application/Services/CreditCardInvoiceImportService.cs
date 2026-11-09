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

    private static readonly HashSet<InvoiceImportStatus> DiscardableStatuses =
        [InvoiceImportStatus.AwaitingReview, InvoiceImportStatus.Failed];

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICreditCardImportEligibilityService _eligibility;
    private readonly ILogger<CreditCardInvoiceImportService> _logger;

    public CreditCardInvoiceImportService(
        IUnitOfWork unitOfWork,
        ICreditCardImportEligibilityService eligibility,
        ILogger<CreditCardInvoiceImportService> logger)
    {
        _unitOfWork = unitOfWork;
        _eligibility = eligibility;
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

        var sourceType = CreateInvoiceImportValidator.ResolveSourceType(request.Files[0].ContentType)
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

        var files = request.Files
            .Select((f, index) => new CreditCardInvoiceImportFile
            {
                UserId = userId,
                ImportId = job.Id,
                Order = index,
                ContentType = CreateInvoiceImportValidator.NormalizeContentType(f.ContentType),
                SizeBytes = f.SizeBytes,
                Content = f.Content,
                CreatedAt = now
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
