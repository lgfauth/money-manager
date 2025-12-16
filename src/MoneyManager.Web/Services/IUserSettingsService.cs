using MoneyManager.Domain.Entities;

namespace MoneyManager.Web.Services;

public interface IUserSettingsService
{
    Task<UserSettings> GetSettingsAsync();
    Task<UserSettings> UpdateSettingsAsync(UserSettings settings);
}
