using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

public interface IUserSettingsService
{
    Task<UserSettings> GetSettingsAsync(string userId);
    Task<UserSettings> UpdateSettingsAsync(string userId, UserSettings settings);
    Task<UserSettings> GetOrCreateSettingsAsync(string userId);
}

public class UserSettingsService : IUserSettingsService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserSettingsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserSettings> GetSettingsAsync(string userId)
    {
        var settings = await _unitOfWork.UserSettings.GetAllAsync();
        var userSettings = settings.FirstOrDefault(s => s.UserId == userId);

        if (userSettings == null)
        {
            userSettings = await GetOrCreateSettingsAsync(userId);
        }

        return userSettings;
    }

    public async Task<UserSettings> GetOrCreateSettingsAsync(string userId)
    {
        var settings = await _unitOfWork.UserSettings.GetAllAsync();
        var userSettings = settings.FirstOrDefault(s => s.UserId == userId);

        if (userSettings == null)
        {
            userSettings = new UserSettings
            {
                UserId = userId,
                Currency = "BRL",
                DateFormat = "dd/MM/yyyy",
                MonthClosingDay = 1,
                EmailNotifications = true,
                NotifyRecurringProcessed = true,
                NotifyBudgetAlert = true,
                BudgetAlertThreshold = 80,
                NotifyCreditLimitAlert = true,
                CreditLimitAlertThreshold = 75,
                MonthlySummaryEmail = true,
                Theme = "auto",
                PrimaryColor = "#667eea"
            };

            await _unitOfWork.UserSettings.AddAsync(userSettings);
            await _unitOfWork.SaveChangesAsync();
        }

        return userSettings;
    }

    public async Task<UserSettings> UpdateSettingsAsync(string userId, UserSettings settings)
    {
        var existingSettings = await GetSettingsAsync(userId);

        existingSettings.Currency = settings.Currency;
        existingSettings.DateFormat = settings.DateFormat;
        existingSettings.MonthClosingDay = settings.MonthClosingDay;
        existingSettings.DefaultBudget = settings.DefaultBudget;
        existingSettings.EmailNotifications = settings.EmailNotifications;
        existingSettings.NotifyRecurringProcessed = settings.NotifyRecurringProcessed;
        existingSettings.NotifyBudgetAlert = settings.NotifyBudgetAlert;
        existingSettings.BudgetAlertThreshold = settings.BudgetAlertThreshold;
        existingSettings.NotifyCreditLimitAlert = settings.NotifyCreditLimitAlert;
        existingSettings.CreditLimitAlertThreshold = settings.CreditLimitAlertThreshold;
        existingSettings.MonthlySummaryEmail = settings.MonthlySummaryEmail;
        existingSettings.Theme = settings.Theme;
        existingSettings.PrimaryColor = settings.PrimaryColor;
        existingSettings.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.UserSettings.UpdateAsync(existingSettings);
        await _unitOfWork.SaveChangesAsync();

        return existingSettings;
    }
}
