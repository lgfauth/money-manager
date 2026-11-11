using System.Globalization;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.Services;

/// <summary>
/// Resultado da conferência de uma leitura com os totais impressos na fatura.
/// <paramref name="IsApplicable"/> é falso quando o documento não traz subtotal de titular nem total do período.
/// <paramref name="Divergence"/> soma os módulos das diferenças (titulares + total do período): critério de
/// escolha da melhor leitura quando nenhuma confere.
/// </summary>
public sealed record InvoiceImportReconciliationResult(
    bool IsApplicable,
    bool Matches,
    decimal Divergence,
    int DivergentHolders,
    bool PeriodDivergent,
    IReadOnlyList<InvoiceImportWarning> Warnings)
{
    public static readonly InvoiceImportReconciliationResult NotApplicable = new(false, true, 0m, 0, false, []);
}

/// <summary>
/// Confere a soma dos lançamentos lidos com os totais impressos: por titular (<see cref="InvoiceImportHolder.PrintedTotalAmount"/>)
/// e no total dos lançamentos do período (<see cref="InvoiceImportDocumentInfo.PeriodTotalAmount"/>). Estornos contam negativos.
/// </summary>
public static class InvoiceImportReconciliation
{
    /// <summary>Diferença a partir da qual os valores não conferem.</summary>
    public const decimal Tolerance = 0.01m;

    private static readonly CultureInfo PtBr = new("pt-BR");

    public static InvoiceImportReconciliationResult Check(
        IReadOnlyList<InvoiceImportHolder> holders,
        IReadOnlyList<InvoiceImportItem> items,
        decimal? periodTotal)
    {
        var printedHolders = holders.Where(h => h.PrintedTotalAmount is not null).ToList();
        if (printedHolders.Count == 0 && periodTotal is null)
            return InvoiceImportReconciliationResult.NotApplicable;

        var warnings = new List<InvoiceImportWarning>();
        var divergence = 0m;
        var divergentHolders = 0;

        foreach (var holder in printedHolders)
        {
            var diff = Math.Abs(holder.TotalAmount - holder.PrintedTotalAmount!.Value);
            if (diff < Tolerance)
                continue;

            divergence += diff;
            divergentHolders++;
            warnings.Add(new InvoiceImportWarning
            {
                Code = InvoiceImportWarningCode.HolderTotalMismatch,
                Message = $"A soma dos lançamentos de {holder.Name} ({Money(holder.TotalAmount)}) não confere com o total da fatura para este titular ({Money(holder.PrintedTotalAmount.Value)}). Confira os lançamentos antes de importar.",
                IsBlocking = false
            });
        }

        var periodDivergent = false;
        if (periodTotal is { } period)
        {
            var sum = items.Sum(i => i.Type == CreditCardTransactionType.Refund ? -i.Amount : i.Amount);
            var diff = Math.Abs(sum - period);
            if (diff >= Tolerance)
            {
                divergence += diff;
                periodDivergent = true;
                warnings.Add(new InvoiceImportWarning
                {
                    Code = InvoiceImportWarningCode.PeriodTotalMismatch,
                    Message = $"A soma de todos os lançamentos lidos ({Money(sum)}) não confere com o total de lançamentos da fatura ({Money(period)}). Confira os lançamentos antes de importar.",
                    IsBlocking = false
                });
            }
        }

        return new InvoiceImportReconciliationResult(true, warnings.Count == 0, divergence, divergentHolders, periodDivergent, warnings);
    }

    private static string Money(decimal value) => value.ToString("C", PtBr);
}
