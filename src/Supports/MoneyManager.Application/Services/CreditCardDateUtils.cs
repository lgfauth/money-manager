namespace MoneyManager.Application.Services;

internal static class CreditCardDateUtils
{
    public static DateTime SafeDay(int year, int month, int day)
    {
        var lastDay = DateTime.DaysInMonth(year, month);
        return new DateTime(year, month, Math.Min(day, lastDay), 0, 0, 0, DateTimeKind.Utc);
    }

    public static DateTime SafeInstallmentDate(DateTime purchaseDate, int monthOffset)
    {
        var target = purchaseDate.AddMonths(monthOffset);
        return SafeDay(target.Year, target.Month, purchaseDate.Day);
    }

    public static string FormatReferenceMonth(int year, int month)
        => $"{year:D4}-{month:D2}";

    public static string FormatReferenceMonth(DateTime date)
        => FormatReferenceMonth(date.Year, date.Month);

    public static (int Year, int Month) ParseReferenceMonth(string referenceMonth)
    {
        var parts = referenceMonth.Split('-');
        return (int.Parse(parts[0]), int.Parse(parts[1]));
    }

    public static string AddMonths(string referenceMonth, int months)
    {
        var (year, month) = ParseReferenceMonth(referenceMonth);
        var baseDate = new DateTime(year, month, 1);
        var result = baseDate.AddMonths(months);
        return FormatReferenceMonth(result.Year, result.Month);
    }

    public static string ReferenceMonthForPurchaseDate(DateTime purchaseDate, int closingDay)
    {
        if (purchaseDate.Day <= closingDay)
        {
            return FormatReferenceMonth(purchaseDate.Year, purchaseDate.Month);
        }

        var next = purchaseDate.AddMonths(1);
        return FormatReferenceMonth(next.Year, next.Month);
    }

    public static DateTime ComputeClosingDate(string referenceMonth, int closingDay)
    {
        var (year, month) = ParseReferenceMonth(referenceMonth);
        return SafeDay(year, month, closingDay);
    }

    public static DateTime ComputeDueDate(string referenceMonth, int closingDay, int billingDueDay)
    {
        var (year, month) = ParseReferenceMonth(referenceMonth);
        if (billingDueDay > closingDay)
        {
            return SafeDay(year, month, billingDueDay);
        }

        var next = new DateTime(year, month, 1).AddMonths(1);
        return SafeDay(next.Year, next.Month, billingDueDay);
    }
}
