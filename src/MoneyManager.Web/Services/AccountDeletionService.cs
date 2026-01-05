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
        try
        {
            Console.WriteLine("[Frontend] Chamando api/accountdeletion/data-count");
            Console.WriteLine($"[Frontend] BaseAddress: {_httpClient.BaseAddress}");
            
            var response = await _httpClient.GetAsync("api/accountdeletion/data-count");
            Console.WriteLine($"[Frontend] Status: {response.StatusCode}");
            
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Frontend] Response: {content}");
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Erro ao obter contagem: {response.StatusCode} - {content}");
            }
            
            var data = await response.Content.ReadFromJsonAsync<DataCountResponse>();
            return data?.TotalRecords ?? 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Frontend] ERRO: {ex.Message}");
            Console.WriteLine($"[Frontend] StackTrace: {ex.StackTrace}");
            throw;
        }
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
