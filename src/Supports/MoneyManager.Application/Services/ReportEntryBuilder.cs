using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.Services;

internal enum ReportEntrySource
{
    Account,
    CreditCard
}

internal enum AccountTransactionKind
{
    Income,
    Expense,
    Transfer,
    InvoicePayment,
    NotReportable
}

/// <summary>
/// Lançamento normalizado usado pelos relatórios: receitas e despesas de contas e de cartões
/// já na data em que devem ser apuradas, conforme o regime escolhido.
/// </summary>
internal sealed record ReportEntry(
    DateTime Date,
    decimal Amount,
    bool IsIncome,
    string? CategoryId,
    string Currency,
    ReportEntrySource Source,
    string SourceId,
    string Description,
    string? InstallmentLabel,
    bool IsPending);

internal static class ReportEntryBuilder
{
    // Categoria Open Finance "Credit card payment": débito em conta que quita a fatura do cartão.
    private const string CreditCardPaymentCategoryId = "05100000";

    // Categorias Open Finance "Same person transfer" (04xxxxxx): movimentação entre contas do próprio usuário.
    private const string SamePersonTransferCategoryPrefix = "04";

    public static AccountTransactionKind ClassifyAccountTransaction(
        Transaction transaction,
        IReadOnlySet<string> invoicePaymentTransactionIds,
        bool hasCreditCards)
    {
        if (transaction.Status == TransactionStatus.Cancelled)
            return AccountTransactionKind.NotReportable;

        if (invoicePaymentTransactionIds.Contains(transaction.Id))
            return AccountTransactionKind.InvoicePayment;

        // Só descarta o pagamento importado do banco quando o usuário tem cartões cadastrados:
        // sem eles, as compras não existem no sistema e o pagamento é a única despesa registrada.
        if (hasCreditCards && transaction.OpenBankingCategoryId == CreditCardPaymentCategoryId)
            return AccountTransactionKind.InvoicePayment;

        var isSamePersonTransfer = transaction.OpenBankingCategoryId?
            .StartsWith(SamePersonTransferCategoryPrefix, StringComparison.Ordinal) == true;

        if (transaction.Type == TransactionType.Transfer || isSamePersonTransfer)
            return AccountTransactionKind.Transfer;

        return transaction.Type switch
        {
            TransactionType.Income => AccountTransactionKind.Income,
            TransactionType.Expense => AccountTransactionKind.Expense,
            _ => AccountTransactionKind.NotReportable
        };
    }

    public static ReportEntry CreateAccountEntry(Transaction transaction, AccountTransactionKind kind)
        => new(
            transaction.Date.Date,
            transaction.Amount,
            kind == AccountTransactionKind.Income,
            transaction.CategoryId,
            transaction.Currency,
            ReportEntrySource.Account,
            transaction.AccountId,
            transaction.Description,
            null,
            false);

    /// <summary>
    /// Data de competência de uma parcela: a compra conta no mês de cada parcela.
    /// Transações importadas do banco já chegam uma por parcela (InstallmentNumber = 1).
    /// </summary>
    public static DateTime GetInstallmentDate(CreditCardTransaction item)
        => CreditCardDateUtils.SafeInstallmentDate(item.PurchaseDate.Date, Math.Max(1, item.InstallmentNumber) - 1);

    public static IEnumerable<ReportEntry> BuildAccrualCardEntries(
        IEnumerable<CreditCardTransaction> items,
        IReadOnlyDictionary<string, CreditCard> cards)
    {
        foreach (var item in items)
        {
            if (!cards.TryGetValue(item.CreditCardId, out var card))
                continue;

            yield return CreateCardEntry(item, card, GetInstallmentDate(item), item.InstallmentAmount);
        }
    }

    /// <summary>
    /// Regime de caixa: as compras de cada fatura paga contam na data do pagamento, mantendo a
    /// categoria de cada compra. Pagamentos parciais são distribuídos proporcionalmente entre as compras.
    /// </summary>
    public static IEnumerable<ReportEntry> BuildCashCardEntries(
        IEnumerable<CreditCardInvoice> paidInvoices,
        ILookup<string, CreditCardTransaction> itemsByInvoice,
        IReadOnlyDictionary<string, CreditCard> cards)
    {
        foreach (var invoice in paidInvoices)
        {
            if (!cards.TryGetValue(invoice.CreditCardId, out var card))
                continue;

            var paymentDate = GetInvoicePaymentDate(invoice);
            var items = itemsByInvoice[invoice.Id].ToList();
            var itemsTotal = items.Sum(item => item.InstallmentAmount);

            // Fatura sem compras importadas (ex.: anterior à conexão bancária): usa o valor pago,
            // sem categoria, para que o desembolso não desapareça do relatório.
            if (itemsTotal <= 0)
            {
                var invoiceAmount = invoice.PaidAmount ?? invoice.TotalAmount;
                if (invoiceAmount > 0)
                {
                    yield return new ReportEntry(
                        paymentDate,
                        invoiceAmount,
                        false,
                        null,
                        card.Currency,
                        ReportEntrySource.CreditCard,
                        card.Id,
                        $"Fatura {card.Name} ({invoice.ReferenceMonth})",
                        null,
                        false);
                }

                continue;
            }

            var paymentRatio = invoice.PaidAmount is > 0 ? invoice.PaidAmount.Value / itemsTotal : 1m;

            foreach (var item in items)
            {
                var amount = Math.Round(item.InstallmentAmount * paymentRatio, 2, MidpointRounding.AwayFromZero);
                yield return CreateCardEntry(item, card, paymentDate, amount) with { IsPending = false };
            }
        }
    }

    public static DateTime GetInvoicePaymentDate(CreditCardInvoice invoice)
        => (invoice.PaidAt ?? invoice.DueDate).Date;

    private static ReportEntry CreateCardEntry(CreditCardTransaction item, CreditCard card, DateTime date, decimal amount)
    {
        var installmentLabel = item.TotalInstallments > 1
            ? $"{item.InstallmentNumber}/{item.TotalInstallments}"
            : null;

        return new ReportEntry(
            date,
            amount,
            false,
            item.CategoryId,
            card.Currency,
            ReportEntrySource.CreditCard,
            card.Id,
            item.Description,
            installmentLabel,
            item.IsPending);
    }
}
