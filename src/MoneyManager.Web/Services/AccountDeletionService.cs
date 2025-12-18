using System.Net.Http.Json;

namespace MoneyManager.Web.Services;

public interface IAccountDeletionService
{
    Task<int> GetDataCountAsync();
    Task<bool> DeleteAccountAsync(string password, string confirmationText);
}

public class AccountDeletionService : IAccountDeletionService
{
    private readonly HttpClient _httpClient;

    public AccountDeletionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<int> GetDataCountAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<DataCountResponse>("api/accountdeletion/data-count");
        return response?.TotalRecords ?? 0;
    }

    public async Task<bool> DeleteAccountAsync(string password, string confirmationText)
    {
        var request = new
        {
            Password = password,
            ConfirmationText = confirmationText
        };

        var response = await _httpClient.PostAsJsonAsync("api/accountdeletion/delete-account", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<DeleteResponse>();
            return result?.Deleted ?? false;
        }

        return false;
    }

    private class DataCountResponse
    {
        public int TotalRecords { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    private class DeleteResponse
    {
        public string Message { get; set; } = string.Empty;
        public bool Deleted { get; set; }
    }
}
