using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

public class UserReportService : IUserReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserReport> CreateAsync(string userId, string userName, string category, string description, string? attachmentUrl, string? attachmentFileName)
    {
        var report = new UserReport
        {
            UserId = userId,
            UserName = userName,
            Category = category,
            Description = description,
            AttachmentUrl = attachmentUrl,
            AttachmentFileName = attachmentFileName
        };

        await _unitOfWork.UserReports.AddAsync(report);
        await _unitOfWork.SaveChangesAsync();

        return report;
    }

    public async Task<IEnumerable<UserReport>> GetAllAsync()
    {
        return await _unitOfWork.UserReports.GetAllAsync();
    }

    public async Task<IEnumerable<UserReport>> GetByUserAsync(string userId)
    {
        var all = await _unitOfWork.UserReports.GetAllAsync();
        return all.Where(r => r.UserId == userId);
    }
}
