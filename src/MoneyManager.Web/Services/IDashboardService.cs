namespace MoneyManager.Web.Services;

public interface IDashboardService
{
    Task<DashboardSummary> GetDashboardSummaryAsync();
}
