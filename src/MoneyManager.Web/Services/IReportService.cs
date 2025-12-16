namespace MoneyManager.Web.Services;

public interface IReportService
{
    Task<ReportSummary> GetReportSummaryAsync(int months = 1);
}
