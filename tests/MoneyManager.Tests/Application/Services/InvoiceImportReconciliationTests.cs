using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using Xunit;

namespace MoneyManager.Tests.Application.Services;

public class InvoiceImportReconciliationTests
{
    private static InvoiceImportHolder Holder(string name, decimal total, decimal? printed) =>
        new() { Name = name, TotalAmount = total, PrintedTotalAmount = printed };

    private static InvoiceImportItem Item(decimal amount, CreditCardTransactionType type = CreditCardTransactionType.Purchase) =>
        new() { Description = "X", Amount = amount, Type = type };

    [Fact]
    public void Check_WithoutPrintedTotals_ShouldNotApply()
    {
        // Act
        var result = InvoiceImportReconciliation.Check([Holder("ANA", 10m, null)], [Item(10m)], null);

        // Assert
        Assert.False(result.IsApplicable);
        Assert.True(result.Matches);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Check_WhenEverythingMatches_ShouldMatchWithoutWarnings()
    {
        // Arrange — caso Itaú: estorno de 1,12 soma negativo
        var items = new[] { Item(9839.64m), Item(1.12m), Item(1.12m, CreditCardTransactionType.Refund), Item(4172.31m) };
        var holders = new[] { Holder("LUAN FAUTH", 9839.64m, 9839.64m), Holder("SILVANA OLIVEIRA", 4172.31m, 4172.31m) };

        // Act
        var result = InvoiceImportReconciliation.Check(holders, items, 14011.95m);

        // Assert
        Assert.True(result.IsApplicable);
        Assert.True(result.Matches);
        Assert.Equal(0m, result.Divergence);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Check_WithDifferenceBelowOneCent_ShouldMatch()
    {
        // Act
        var result = InvoiceImportReconciliation.Check([Holder("ANA", 10.005m, 10m)], [Item(10.005m)], 10m);

        // Assert
        Assert.True(result.Matches);
    }

    [Fact]
    public void Check_WithDivergentHolder_ShouldWarnWithAmountsInBrazilianFormat()
    {
        // Act
        var result = InvoiceImportReconciliation.Check(
            [Holder("LUAN FAUTH", 10005.64m, 9839.64m), Holder("ANA", 5m, null)], [Item(10005.64m), Item(5m)], null);

        // Assert
        Assert.False(result.Matches);
        Assert.Equal(1, result.DivergentHolders);
        Assert.False(result.PeriodDivergent);
        Assert.Equal(166m, result.Divergence);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(InvoiceImportWarningCode.HolderTotalMismatch, warning.Code);
        Assert.False(warning.IsBlocking);
        Assert.Contains("LUAN FAUTH", warning.Message);
        Assert.Contains("10.005,64", warning.Message);
        Assert.Contains("9.839,64", warning.Message);
    }

    [Fact]
    public void Check_WithOnlyPeriodDivergent_ShouldWarnAboutPeriodTotal()
    {
        // Arrange — itens sem titular escapam da conferência por titular, mas não do total do período
        var holders = new[] { Holder("ANA", 100m, 100m), Holder(InvoiceImportReviewService.UnidentifiedHolderName, 20m, null) };

        // Act
        var result = InvoiceImportReconciliation.Check(holders, [Item(100m), Item(20m)], 100m);

        // Assert
        Assert.False(result.Matches);
        Assert.True(result.PeriodDivergent);
        Assert.Equal(0, result.DivergentHolders);
        Assert.Equal(20m, result.Divergence);
        Assert.Equal(InvoiceImportWarningCode.PeriodTotalMismatch, Assert.Single(result.Warnings).Code);
    }
}
