using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.Services;

/// <summary>
/// Regras puras de duplicidade da importação de fatura: unificação de prints sobrepostos,
/// fingerprint de reimportação, correspondência um-para-um com transações existentes e
/// classificação de parcelamentos. Duplicados prováveis são sinalizados e desmarcados, nunca removidos
/// (exceto a unificação de prints, que remove a cópia vinda de outra imagem).
/// </summary>
internal static class InvoiceImportDuplicateDetector
{
    /// <summary>Tolerância de data entre a fatura e o cadastro (fuso e data de lançamento × data de compra).</summary>
    public const int DateToleranceDays = 1;

    // Diferença de arredondamento aceita no valor de parcela (a última parcela absorve os centavos)
    private const decimal InstallmentAmountTolerance = 0.01m;

    private static readonly InvoiceImportWarningCode[] DuplicateCodes =
    [
        InvoiceImportWarningCode.PossibleDuplicate,
        InvoiceImportWarningCode.AlreadyImported,
        InvoiceImportWarningCode.AlreadyRegistered,
        InvoiceImportWarningCode.PreviousInstallmentsNotRegistered
    ];

    /// <summary>Códigos que desmarcam o item por padrão, além dos bloqueantes.</summary>
    private static readonly HashSet<InvoiceImportWarningCode> DeselectingCodes =
    [
        InvoiceImportWarningCode.PossibleDuplicate,
        InvoiceImportWarningCode.AlreadyRegistered,
        InvoiceImportWarningCode.PreviousInstallmentsNotRegistered
    ];

    // --- 1. Prints sobrepostos ---

    /// <summary>
    /// Remove itens repetidos que vêm de imagens diferentes (mesma data, valor, tipo, parcela, titular e
    /// descrição normalizada). Itens iguais na mesma imagem/página/planilha são mantidos: de cada grupo
    /// ficam tantos itens quanto o maior número de repetições dentro de um único arquivo.
    /// </summary>
    public static List<InvoiceImportItem> MergeOverlappingScreenshots(List<InvoiceImportItem> items)
    {
        var result = new List<InvoiceImportItem>(items.Count);

        foreach (var group in items.GroupBy(OverlapKey))
        {
            var list = group.ToList();
            var keep = list.GroupBy(i => i.Source?.FileIndex ?? -1).Max(g => g.Count());
            result.AddRange(list.Take(keep));
        }

        // Mantém a ordem original do documento
        var kept = result.ToHashSet();
        return items.Where(kept.Contains).ToList();
    }

    private static string OverlapKey(InvoiceImportItem item) => string.Join('|',
        item.Date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "",
        item.Amount.ToString("0.00", CultureInfo.InvariantCulture),
        item.Type,
        item.InstallmentNumber,
        item.TotalInstallments,
        InvoiceImportText.Normalize(item.HolderName),
        InvoiceImportText.NormalizeDescription(item.Description));

    // --- 2. Fingerprint ---

    /// <summary>
    /// Calcula o fingerprint de cada item: hash de cartão + mês de referência + data + valor + tipo +
    /// descrição normalizada + parcela X/N + ordinal entre itens idênticos (na ordem da lista).
    /// </summary>
    public static void AssignFingerprints(string creditCardId, string referenceMonth, IEnumerable<InvoiceImportItem> items)
    {
        var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var item in items)
        {
            var baseKey = string.Join('|',
                creditCardId,
                referenceMonth,
                item.Date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "",
                item.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                item.Type,
                InvoiceImportText.NormalizeDescription(item.Description),
                $"{item.InstallmentNumber}/{item.TotalInstallments}");

            ordinals.TryGetValue(baseKey, out var ordinal);
            ordinals[baseKey] = ordinal + 1;

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{baseKey}|{ordinal}"));
            item.Fingerprint = Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }

    // --- 3/4. Duplicados e parcelamentos ---

    /// <summary>
    /// Aplica os alertas de duplicidade aos itens incluídos (titulares escolhidos), contra as transações
    /// existentes do cartão. Remove alertas de duplicidade anteriores, então pode ser reaplicado na confirmação.
    /// Itens não incluídos ficam sem alertas de duplicidade e não "consomem" transações existentes.
    /// </summary>
    public static void Apply(
        IReadOnlyList<InvoiceImportItem> items,
        Func<InvoiceImportItem, bool> isIncluded,
        IReadOnlyCollection<CreditCardTransaction> existing,
        string? importedInvoiceId)
    {
        foreach (var item in items)
            item.Warnings.RemoveAll(c => DuplicateCodes.Contains(c));

        var candidates = items
            .Where(i => isIncluded(i) && i.Date is not null && i.Amount > 0
                        && !i.Warnings.Any(InvoiceImportReviewService.IsBlocking))
            .ToList();

        var consumed = new HashSet<string>(StringComparer.Ordinal);

        // a) Reimportação: fingerprint já gravado em transação existente
        var byFingerprint = existing
            .Where(t => !string.IsNullOrEmpty(t.ImportFingerprint))
            .GroupBy(t => t.ImportFingerprint!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        foreach (var item in candidates.ToList())
        {
            if (item.Fingerprint is not null && byFingerprint.TryGetValue(item.Fingerprint, out var tx))
            {
                item.Warnings.Add(InvoiceImportWarningCode.AlreadyImported);
                consumed.Add(tx.Id);
                candidates.Remove(item);
            }
        }

        // b) Parcela X/N (X > 1): procura a parcela na fatura importada
        foreach (var item in candidates.Where(i => i.InstallmentNumber > 1).ToList())
        {
            var match = importedInvoiceId is null
                ? null
                : existing.FirstOrDefault(t =>
                    !consumed.Contains(t.Id)
                    && t.InvoiceId == importedInvoiceId
                    && t.Type == item.Type
                    && t.InstallmentNumber == item.InstallmentNumber
                    && t.TotalInstallments == item.TotalInstallments
                    && Math.Abs(Math.Abs(t.InstallmentAmount) - item.Amount) <= InstallmentAmountTolerance);

            if (match is not null)
            {
                item.Warnings.Add(InvoiceImportWarningCode.AlreadyRegistered);
                consumed.Add(match.Id);
            }
            else
            {
                item.Warnings.Add(InvoiceImportWarningCode.PreviousInstallmentsNotRegistered);
            }
        }

        // c) Parcela 1/N: compra parcelada equivalente já cadastrada (qualquer registro do parcelamento)
        var firstInstallments = candidates.Where(i => i.InstallmentNumber == 1).ToList();
        var installmentGroups = existing
            .Where(t => t.TotalInstallments > 1)
            .GroupBy(t => t.ParentTransactionId ?? t.Id)
            .Select(g => g.OrderBy(t => t.InstallmentNumber).First())
            .ToList();

        MatchOneToOne(firstInstallments, installmentGroups, consumed, (item, tx) =>
            tx.Type == item.Type
            && tx.TotalInstallments == item.TotalInstallments
            && DaysApart(item.Date!.Value, tx.PurchaseDate) <= DateToleranceDays
            && (Math.Abs(Math.Abs(tx.InstallmentAmount) - item.Amount) <= InstallmentAmountTolerance || Math.Abs(tx.TotalAmount) == item.Amount),
            InvoiceImportWarningCode.AlreadyRegistered);

        // d) Compras à vista e estornos: valor exato, data ±1 dia, descrição como desempate
        var cashItems = candidates.Where(i => i.InstallmentNumber is null).ToList();
        var cashTransactions = existing.Where(t => t.TotalInstallments <= 1).ToList();

        MatchOneToOne(cashItems, cashTransactions, consumed, (item, tx) =>
            tx.Type == item.Type
            // Estornos são gravados com valor negativo na parcela
            && (Math.Abs(tx.TotalAmount) == item.Amount || Math.Abs(tx.InstallmentAmount) == item.Amount)
            && DaysApart(item.Date!.Value, tx.PurchaseDate) <= DateToleranceDays,
            InvoiceImportWarningCode.PossibleDuplicate);

        foreach (var item in items)
            item.IsSelectedByDefault = !item.Warnings.Any(c => InvoiceImportReviewService.IsBlocking(c) || DeselectingCodes.Contains(c));
    }

    /// <summary>
    /// Correspondência um-para-um: cada transação existente "consome" no máximo um item. Pares com
    /// descrição parecida e data mais próxima são escolhidos primeiro.
    /// </summary>
    private static void MatchOneToOne(
        List<InvoiceImportItem> items,
        List<CreditCardTransaction> transactions,
        HashSet<string> consumed,
        Func<InvoiceImportItem, CreditCardTransaction, bool> isMatch,
        InvoiceImportWarningCode code)
    {
        var pairs = (
            from item in items
            from tx in transactions
            where !consumed.Contains(tx.Id) && isMatch(item, tx)
            select new
            {
                Item = item,
                Tx = tx,
                DescriptionMismatch = InvoiceImportText.DescriptionsLookAlike(item.Description, tx.Description) ? 0 : 1,
                Days = DaysApart(item.Date!.Value, tx.PurchaseDate)
            })
            .OrderBy(p => p.DescriptionMismatch)
            .ThenBy(p => p.Days)
            .ToList();

        var matchedItems = new HashSet<InvoiceImportItem>();
        foreach (var pair in pairs)
        {
            if (matchedItems.Contains(pair.Item) || consumed.Contains(pair.Tx.Id))
                continue;

            pair.Item.Warnings.Add(code);
            matchedItems.Add(pair.Item);
            consumed.Add(pair.Tx.Id);
        }
    }

    private static double DaysApart(DateTime a, DateTime b) => Math.Abs((a.Date - b.Date).TotalDays);
}
