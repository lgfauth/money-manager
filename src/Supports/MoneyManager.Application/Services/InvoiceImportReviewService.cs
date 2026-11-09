using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

/// <summary>
/// Transforma o resultado da IA nos dados de revisão da importação: resolve datas, aplica as regras
/// de fatura fechada, "nada no futuro" e 60 dias, confere fechamento/vencimento com o cadastro do
/// cartão, agrupa e pré-seleciona titulares e associa categorias. Usado pelo Worker e,
/// na confirmação, para revalidar itens e cartão contra o cadastro atual.
/// </summary>
public interface IInvoiceImportReviewService
{
    Task<InvoiceImportReviewResult> BuildReviewAsync(
        string userId,
        CreditCard card,
        InvoiceDocumentAnalysisResultDto analysis,
        DateOnly today);

    /// <summary>
    /// Alertas de conferência do fechamento/vencimento do documento com <c>ClosingDay</c>/<c>BillingDueDay</c>.
    /// Diferença de até 3 dias é informativa; acima disso, bloqueante.
    /// </summary>
    IReadOnlyList<InvoiceImportWarning> CheckCardSchedule(InvoiceImportDocumentInfo invoice, CreditCard card);

    /// <summary>
    /// Códigos de alerta das regras de item (data, descrição, valor, futuro, 60 dias) para o fechamento informado.
    /// </summary>
    IReadOnlyList<InvoiceImportWarningCode> ValidateItem(InvoiceImportItem item, DateTime closingDate, DateOnly today);
}

/// <summary>
/// Resultado da montagem da revisão. Em falha, <see cref="FailureMessage"/> traz o motivo amigável
/// e o job deve ir para <c>Failed</c>.
/// </summary>
public sealed record InvoiceImportReviewResult(
    bool IsSuccess,
    string? FailureMessage,
    string? ReferenceMonth,
    InvoiceImportDocumentInfo? Invoice,
    List<InvoiceImportHolder> Holders,
    List<InvoiceImportWarning> Warnings,
    List<InvoiceImportItem> Items)
{
    public static InvoiceImportReviewResult Failure(string message) =>
        new(false, message, null, null, [], [], []);
}

public class InvoiceImportReviewService : IInvoiceImportReviewService
{
    public const string NotClosedMessage =
        "A fatura ainda não foi fechada. Importe a fatura depois da data de fechamento.";
    public const string NoItemsMessage =
        "Nenhum lançamento foi encontrado na fatura. Verifique o arquivo e envie novamente.";
    public const string UnknownReferenceMonthMessage =
        "Não foi possível identificar o mês da fatura (fechamento ou vencimento). Envie a fatura completa novamente.";

    /// <summary>Nome do grupo de itens sem titular quando a fatura também tem itens com titular.</summary>
    public const string UnidentifiedHolderName = "Titular não identificado";

    /// <summary>Compras à vista ou 1ª parcela com data anterior a hoje − 60 dias não podem ser importadas.</summary>
    public const int MaxDaysInPast = 60;

    /// <summary>Tolerância de dias entre o documento e o cadastro do cartão (fins de semana e feriados).</summary>
    public const int ScheduleToleranceDays = 3;

    private static readonly HashSet<InvoiceImportWarningCode> BlockingItemCodes =
    [
        InvoiceImportWarningCode.MissingDate,
        InvoiceImportWarningCode.MissingDescription,
        InvoiceImportWarningCode.InvalidAmount,
        InvoiceImportWarningCode.FutureDate,
        InvoiceImportWarningCode.OutOfAllowedPeriod,
        InvoiceImportWarningCode.AlreadyImported,
        InvoiceImportWarningCode.NotFoundInSpreadsheet
    ];

    private readonly IUnitOfWork _unitOfWork;

    public InvoiceImportReviewService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>Alertas de item que impedem a confirmação sem correção.</summary>
    public static bool IsBlocking(InvoiceImportWarningCode code) => BlockingItemCodes.Contains(code);

    public async Task<InvoiceImportReviewResult> BuildReviewAsync(
        string userId,
        CreditCard card,
        InvoiceDocumentAnalysisResultDto analysis,
        DateOnly today)
    {
        var doc = analysis.Invoice;
        var todayDate = ToDate(today);

        // Documento que se declara fatura aberta/parcial nunca é importado
        if (doc.IsOpenOrPartial)
            return InvoiceImportReviewResult.Failure(NotClosedMessage);

        var warnings = new List<InvoiceImportWarning>();

        // 1. Fechamento, vencimento e mês de referência
        var docClosing = ResolveStandaloneDate(doc.ClosingDate, todayDate);
        var docDue = ResolveDueDate(doc.DueDate, docClosing, todayDate);

        string? referenceMonth;
        DateTime closingDate;
        var closingCalculated = false;

        if (docClosing is { } closing)
        {
            referenceMonth = NearestReferenceMonth(closing, r => CreditCardDateUtils.ComputeClosingDate(r, card.ClosingDay));
            closingDate = closing;
        }
        else
        {
            if (docDue is { } due)
            {
                referenceMonth = NearestReferenceMonth(due, r => CreditCardDateUtils.ComputeDueDate(r, card.ClosingDay, card.BillingDueDay));
            }
            else
            {
                // Sem fechamento e sem vencimento: usa o lançamento mais recente
                var latest = analysis.Items
                    .Select(i => ResolveStandaloneDate(i.Date, todayDate))
                    .Where(d => d is not null)
                    .Max();
                referenceMonth = latest is { } l
                    ? CreditCardDateUtils.ReferenceMonthForPurchaseDate(l, card.ClosingDay)
                    : null;
            }

            if (referenceMonth is null)
                return InvoiceImportReviewResult.Failure(UnknownReferenceMonthMessage);

            closingDate = CreditCardDateUtils.ComputeClosingDate(referenceMonth, card.ClosingDay);
            closingCalculated = true;
            warnings.Add(new InvoiceImportWarning
            {
                Code = InvoiceImportWarningCode.ClosingDateCalculated,
                Message = $"A fatura não informa a data de fechamento; usamos o dia {card.ClosingDay} do cadastro do cartão ({closingDate:dd/MM/yyyy}).",
                IsBlocking = false
            });
        }

        // 2. Fatura fechada: fechamento até hoje
        if (closingDate > todayDate)
            return InvoiceImportReviewResult.Failure(NotClosedMessage);

        if (analysis.Items.Count == 0)
            return InvoiceImportReviewResult.Failure(NoItemsMessage);

        var invoice = new InvoiceImportDocumentInfo
        {
            Issuer = doc.Issuer,
            CardLastDigits = doc.CardLastDigits,
            ClosingDate = closingDate,
            ClosingDateCalculated = closingCalculated,
            DueDate = docDue,
            ReferenceMonth = referenceMonth,
            TotalAmount = doc.TotalAmount,
            IsOpenOrPartial = false
        };

        // 3. Conferência com o cadastro do cartão
        warnings.AddRange(CheckCardSchedule(invoice, card));

        // 4. Pendências apontadas pela IA (informativas)
        warnings.AddRange(analysis.Issues.Select(issue => new InvoiceImportWarning
        {
            Code = InvoiceImportWarningCode.UnreadableContent,
            Message = issue,
            IsBlocking = false
        }));

        // 5. Itens
        var categories = await GetExpenseCategoriesAsync(userId);
        var items = analysis.Items
            .Select(i => BuildItem(i, closingDate, today, categories))
            .ToList();

        // 6. Titulares
        var holders = GroupHolders(items);
        await SuggestHoldersAsync(userId, card.Id, holders);

        return new InvoiceImportReviewResult(true, null, referenceMonth, invoice, holders, warnings, items);
    }

    public IReadOnlyList<InvoiceImportWarning> CheckCardSchedule(InvoiceImportDocumentInfo invoice, CreditCard card)
    {
        var warnings = new List<InvoiceImportWarning>();
        if (string.IsNullOrEmpty(invoice.ReferenceMonth))
            return warnings;

        // Fechamento calculado pelo próprio cadastro não tem o que conferir
        if (!invoice.ClosingDateCalculated && invoice.ClosingDate is { } docClosing)
        {
            var expected = CreditCardDateUtils.ComputeClosingDate(invoice.ReferenceMonth, card.ClosingDay);
            var warning = CompareScheduleDay(
                docClosing, expected, card.ClosingDay, InvoiceImportWarningCode.ClosingDayMismatch, "fechamento");
            if (warning is not null)
                warnings.Add(warning);
        }

        if (invoice.DueDate is { } docDue)
        {
            var expected = CreditCardDateUtils.ComputeDueDate(invoice.ReferenceMonth, card.ClosingDay, card.BillingDueDay);
            var warning = CompareScheduleDay(
                docDue, expected, card.BillingDueDay, InvoiceImportWarningCode.DueDayMismatch, "vencimento");
            if (warning is not null)
                warnings.Add(warning);
        }

        return warnings;
    }

    public IReadOnlyList<InvoiceImportWarningCode> ValidateItem(InvoiceImportItem item, DateTime closingDate, DateOnly today)
    {
        var codes = new List<InvoiceImportWarningCode>();
        var todayDate = ToDate(today);

        if (item.Date is null)
        {
            codes.Add(InvoiceImportWarningCode.MissingDate);
        }
        else
        {
            var date = item.Date.Value.Date;
            if (date > todayDate || date > closingDate.Date)
            {
                codes.Add(InvoiceImportWarningCode.FutureDate);
            }
            else if ((item.InstallmentNumber ?? 1) <= 1 && date < todayDate.AddDays(-MaxDaysInPast))
            {
                // Parcelas X > 1 são isentas: a data é a da compra original
                codes.Add(InvoiceImportWarningCode.OutOfAllowedPeriod);
            }
        }

        if (string.IsNullOrWhiteSpace(item.Description))
            codes.Add(InvoiceImportWarningCode.MissingDescription);

        if (item.Amount <= 0)
            codes.Add(InvoiceImportWarningCode.InvalidAmount);

        return codes;
    }

    // --- Itens ---

    private InvoiceImportItem BuildItem(
        InvoiceDocumentItemDto source,
        DateTime closingDate,
        DateOnly today,
        IReadOnlyDictionary<string, string> categories)
    {
        int? installmentNumber = source.InstallmentNumber;
        int? totalInstallments = source.TotalInstallments;
        if (totalInstallments is null or <= 1)
        {
            // "1 de 1" ou sem total: compra à vista
            installmentNumber = null;
            totalInstallments = null;
        }

        var item = new InvoiceImportItem
        {
            Date = ResolveItemDate(source.Date, closingDate, today),
            Description = source.Description.Trim(),
            Amount = source.Amount ?? 0m,
            Type = source.Type,
            InstallmentNumber = installmentNumber,
            TotalInstallments = totalInstallments,
            HolderName = string.IsNullOrWhiteSpace(source.HolderName) ? null : source.HolderName.Trim(),
            HolderType = source.HolderType,
            CardLastDigits = source.CardLastDigits,
            CardType = source.CardType,
            Source = source.Source is null
                ? null
                : new InvoiceImportSourceReference
                {
                    FileIndex = source.Source.FileIndex,
                    Page = source.Source.Page,
                    Sheet = source.Source.Sheet,
                    Row = source.Source.Row
                },
            CategoryHint = source.CategoryHint,
            CategoryId = MatchCategory(source.CategoryHint, categories),
            Confidence = source.Confidence,
            Notes = source.Notes
        };

        item.Warnings = ValidateItem(item, closingDate, today).ToList();
        item.IsSelectedByDefault = !item.Warnings.Any(IsBlocking);
        return item;
    }

    private static string? MatchCategory(string? hint, IReadOnlyDictionary<string, string> categories)
    {
        var key = InvoiceImportText.Normalize(hint);
        return key.Length > 0 && categories.TryGetValue(key, out var id) ? id : null;
    }

    private async Task<IReadOnlyDictionary<string, string>> GetExpenseCategoriesAsync(string userId)
    {
        var categories = await _unitOfWork.Categories.GetByUserIdAsync(userId);
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var category in categories.Where(c => c.UserId == userId && !c.IsDeleted && c.Type == CategoryType.Expense))
        {
            map.TryAdd(InvoiceImportText.Normalize(category.Name), category.Id);
        }

        return map;
    }

    // --- Titulares ---

    /// <summary>
    /// Agrupa os itens por titular (nome normalizado e por prefixo) e padroniza o nome do item com o do grupo.
    /// Documento sem nenhum titular → lista vazia (titular implícito, sem escolha).
    /// </summary>
    internal static List<InvoiceImportHolder> GroupHolders(List<InvoiceImportItem> items)
    {
        var names = items
            .Where(i => i.HolderName is not null)
            .Select(i => i.HolderName!)
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(n => InvoiceImportText.Normalize(n).Length)
            .ThenBy(n => n, StringComparer.Ordinal)
            .ToList();

        if (names.Count == 0)
            return [];

        // Nome mais longo vira o nome do grupo; nomes cortados que são prefixo dele entram no mesmo grupo
        var canonical = new Dictionary<string, string>(StringComparer.Ordinal);
        var groupNames = new List<string>();
        foreach (var name in names)
        {
            var group = groupNames.FirstOrDefault(g => InvoiceImportText.NamesMatch(g, name));
            if (group is null)
            {
                group = name;
                groupNames.Add(name);
            }
            canonical[name] = group;
        }

        foreach (var item in items.Where(i => i.HolderName is not null))
            item.HolderName = canonical[item.HolderName!];

        var holders = groupNames
            .Select(name => BuildHolder(name, items.Where(i => i.HolderName == name).ToList()))
            .ToList();

        var unidentified = items.Where(i => i.HolderName is null).ToList();
        if (unidentified.Count > 0)
            holders.Add(BuildHolder(UnidentifiedHolderName, unidentified));

        return holders;
    }

    private static InvoiceImportHolder BuildHolder(string name, List<InvoiceImportItem> items)
    {
        var type = items.Any(i => i.HolderType == InvoiceImportHolderType.Primary)
            ? InvoiceImportHolderType.Primary
            : items.Any(i => i.HolderType == InvoiceImportHolderType.Additional)
                ? InvoiceImportHolderType.Additional
                : InvoiceImportHolderType.Unknown;

        return new InvoiceImportHolder
        {
            Name = name,
            Type = type,
            CardLastDigits = items
                .Where(i => !string.IsNullOrWhiteSpace(i.CardLastDigits))
                .Select(i => i.CardLastDigits!)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(d => d, StringComparer.Ordinal)
                .ToList(),
            ItemCount = items.Count,
            TotalAmount = items.Sum(i => i.Type == CreditCardTransactionType.Refund ? -i.Amount : i.Amount)
        };
    }

    /// <summary>
    /// Pré-seleção: titulares da última importação confirmada do cartão → nome do usuário → titular único → nenhum.
    /// </summary>
    private async Task SuggestHoldersAsync(string userId, string creditCardId, List<InvoiceImportHolder> holders)
    {
        if (holders.Count == 0)
            return;

        var lastConfirmed = await _unitOfWork.CreditCardInvoiceImports.GetLastConfirmedByCardAsync(userId, creditCardId);
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var userName = string.IsNullOrWhiteSpace(user?.FullName) ? user?.Name : user.FullName;

        ApplyHolderSuggestion(holders, lastConfirmed?.SelectedHolders ?? [], userName);
    }

    internal static void ApplyHolderSuggestion(List<InvoiceImportHolder> holders, IReadOnlyCollection<string> previousSelection, string? userName)
    {
        foreach (var holder in holders)
            holder.IsSuggested = false;

        // Grupo "não identificado" não corresponde a ninguém por nome
        var named = holders.Where(h => h.Name != UnidentifiedHolderName).ToList();

        var fromPrevious = named
            .Where(h => previousSelection.Any(p => InvoiceImportText.NamesMatch(p, h.Name)))
            .ToList();
        if (fromPrevious.Count > 0)
        {
            fromPrevious.ForEach(h => h.IsSuggested = true);
            return;
        }

        var byUserName = named.Where(h => InvoiceImportText.NamesMatch(userName, h.Name)).ToList();
        if (byUserName.Count > 0)
        {
            byUserName.ForEach(h => h.IsSuggested = true);
            return;
        }

        if (holders.Count == 1)
            holders[0].IsSuggested = true;
    }

    // --- Datas ---

    private static DateTime ToDate(DateOnly date) =>
        new(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);

    private static DateTime? TryDate(int year, int month, int day)
    {
        if (year is < 1 or > 9999 || month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
            return null;
        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>
    /// Data de item: sem ano, assume o ano corrente; se cair depois do fechamento, o ano anterior.
    /// </summary>
    internal static DateTime? ResolveItemDate(InvoiceDocumentDateDto? date, DateTime closingDate, DateOnly today)
    {
        if (date is null)
            return null;

        if (date.Year is { } year)
            return TryDate(year, date.Month, date.Day);

        var current = TryDate(today.Year, date.Month, date.Day);
        if (current is null || current > closingDate.Date)
            return TryDate(today.Year - 1, date.Month, date.Day);

        return current;
    }

    /// <summary>
    /// Data sem referência de fechamento (o próprio fechamento ou o lançamento mais recente):
    /// sem ano, assume o ano corrente; se cair depois de hoje, o ano anterior.
    /// </summary>
    private static DateTime? ResolveStandaloneDate(InvoiceDocumentDateDto? date, DateTime today)
    {
        if (date is null)
            return null;

        if (date.Year is { } year)
            return TryDate(year, date.Month, date.Day);

        var current = TryDate(today.Year, date.Month, date.Day);
        if (current is null || current > today)
            return TryDate(today.Year - 1, date.Month, date.Day);

        return current;
    }

    /// <summary>Vencimento sem ano: o primeiro ano em que ele fica depois do fechamento.</summary>
    private static DateTime? ResolveDueDate(InvoiceDocumentDateDto? date, DateTime? closing, DateTime today)
    {
        if (date is null)
            return null;

        if (date.Year is { } year)
            return TryDate(year, date.Month, date.Day);

        var baseYear = closing?.Year ?? today.Year;
        var candidate = TryDate(baseYear, date.Month, date.Day);
        if (closing is { } c && candidate is { } d && d < c)
            return TryDate(baseYear + 1, date.Month, date.Day);

        return candidate;
    }

    /// <summary>
    /// Mês de referência (yyyy-MM) cuja data calculada pelo cadastro do cartão fica mais perto da data do documento.
    /// Cobre fechamento/vencimento adiados para o mês seguinte por fim de semana ou feriado.
    /// </summary>
    private static string NearestReferenceMonth(DateTime documentDate, Func<string, DateTime> computeForMonth)
    {
        var baseMonth = CreditCardDateUtils.FormatReferenceMonth(documentDate);
        return new[] { 0, -1, 1 }
            .Select(offset => CreditCardDateUtils.AddMonths(baseMonth, offset))
            .OrderBy(r => Math.Abs((computeForMonth(r) - documentDate).TotalDays))
            .First();
    }

    private static InvoiceImportWarning? CompareScheduleDay(
        DateTime documentDate,
        DateTime expectedDate,
        int registeredDay,
        InvoiceImportWarningCode code,
        string label)
    {
        var difference = Math.Abs((documentDate.Date - expectedDate.Date).TotalDays);
        if (difference == 0)
            return null;

        if (difference <= ScheduleToleranceDays)
        {
            return new InvoiceImportWarning
            {
                Code = code,
                Message = $"O {label} da fatura foi dia {documentDate.Day}, e o cartão está cadastrado com dia {registeredDay} (diferença de até {ScheduleToleranceDays} dias, comum em fins de semana e feriados).",
                IsBlocking = false
            };
        }

        return new InvoiceImportWarning
        {
            Code = code,
            Message = $"O {label} da fatura é dia {documentDate.Day}, mas o cartão está cadastrado com dia {registeredDay}; ajuste o cadastro do cartão para continuar.",
            IsBlocking = true
        };
    }
}
