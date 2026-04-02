using MoneyManager.Domain.Entities;

namespace MoneyManager.Application.Services;

public interface IUserReportService
{
    Task<UserReport> CreateAsync(string userId, string userName, string category, string description, string? attachmentUrl, string? attachmentFileName);
    Task<IEnumerable<UserReport>> GetAllAsync();
    Task<IEnumerable<UserReport>> GetByUserAsync(string userId);
}
