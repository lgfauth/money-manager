namespace MoneyManager.Web.Services.Localization;

public interface ILocalizationService
{
    string CurrentCulture { get; }

    Task InitializeAsync();
    Task SetCultureAsync(string culture);

    string Get(string key);
    string Get(string key, params object[] args);
}
