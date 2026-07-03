using Xunit;
using MoneyManager.Application.Services;

namespace MoneyManager.Tests.Application.Services;

public class CreditCardDateUtilsTests
{
    [Theory]
    [InlineData(2024, 1, 15, 15)]
    [InlineData(2024, 2, 31, 29)] // fevereiro bissexto limita ao dia 29
    [InlineData(2023, 2, 30, 28)] // fevereiro comum limita ao dia 28
    [InlineData(2024, 4, 31, 30)] // abril tem 30 dias
    public void SafeDay_ShouldClampDayToLastDayOfMonth(int year, int month, int day, int expectedDay)
    {
        var result = CreditCardDateUtils.SafeDay(year, month, day);

        Assert.Equal(year, result.Year);
        Assert.Equal(month, result.Month);
        Assert.Equal(expectedDay, result.Day);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void SafeInstallmentDate_WithSimpleOffset_ShouldKeepPurchaseDay()
    {
        var purchase = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var result = CreditCardDateUtils.SafeInstallmentDate(purchase, 2);

        Assert.Equal(new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void SafeInstallmentDate_WhenPurchaseDayExceedsTargetMonth_ShouldClampToLastDay()
    {
        var purchase = new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        var result = CreditCardDateUtils.SafeInstallmentDate(purchase, 1);

        // Janeiro 31 + 1 mês → fevereiro (29 dias em 2024)
        Assert.Equal(new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Theory]
    [InlineData(2024, 1, "2024-01")]
    [InlineData(2024, 12, "2024-12")]
    [InlineData(999, 5, "0999-05")]
    public void FormatReferenceMonth_ShouldZeroPad(int year, int month, string expected)
    {
        Assert.Equal(expected, CreditCardDateUtils.FormatReferenceMonth(year, month));
    }

    [Fact]
    public void FormatReferenceMonth_FromDate_ShouldUseYearAndMonth()
    {
        Assert.Equal("2025-07", CreditCardDateUtils.FormatReferenceMonth(new DateTime(2025, 7, 20)));
    }

    [Fact]
    public void ParseReferenceMonth_ShouldRoundTripWithFormat()
    {
        var (year, month) = CreditCardDateUtils.ParseReferenceMonth("2024-03");

        Assert.Equal(2024, year);
        Assert.Equal(3, month);
    }

    [Theory]
    [InlineData("2024-01", 1, "2024-02")]
    [InlineData("2024-12", 1, "2025-01")]
    [InlineData("2024-01", -1, "2023-12")]
    [InlineData("2024-06", 12, "2025-06")]
    public void AddMonths_ShouldHandleYearBoundaries(string reference, int months, string expected)
    {
        Assert.Equal(expected, CreditCardDateUtils.AddMonths(reference, months));
    }

    [Theory]
    [InlineData(2024, 3, 10, 15, "2024-03")] // compra antes do fechamento → mês corrente
    [InlineData(2024, 3, 15, 15, "2024-03")] // compra no dia do fechamento → mês corrente
    [InlineData(2024, 3, 16, 15, "2024-04")] // compra após o fechamento → mês seguinte
    [InlineData(2024, 12, 20, 15, "2025-01")] // virada de ano
    public void ReferenceMonthForPurchaseDate_ShouldRespectClosingDay(int year, int month, int day, int closingDay, string expected)
    {
        var purchase = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(expected, CreditCardDateUtils.ReferenceMonthForPurchaseDate(purchase, closingDay));
    }

    [Fact]
    public void ComputeClosingDate_ShouldUseClosingDayInReferenceMonth()
    {
        var result = CreditCardDateUtils.ComputeClosingDate("2024-02", 31);

        // Dia 31 limitado ao último dia de fevereiro
        Assert.Equal(new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void ComputeDueDate_WhenDueDayAfterClosingDay_ShouldStayInReferenceMonth()
    {
        var result = CreditCardDateUtils.ComputeDueDate("2024-03", 15, 22);

        Assert.Equal(new DateTime(2024, 3, 22, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void ComputeDueDate_WhenDueDayBeforeOrEqualClosingDay_ShouldMoveToNextMonth()
    {
        var result = CreditCardDateUtils.ComputeDueDate("2024-12", 25, 5);

        Assert.Equal(new DateTime(2025, 1, 5, 0, 0, 0, DateTimeKind.Utc), result);
    }
}
