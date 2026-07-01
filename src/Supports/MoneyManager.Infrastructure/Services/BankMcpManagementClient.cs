using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Infrastructure.Services;

// Management API — base URL diferente (https://app.mcp.ai), endpoints REST convencionais.
public class BankMcpManagementClient : IBankMcpManagementClient
{
    private readonly HttpClient _httpClient;

    public BankMcpManagementClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("bancoMcpManagement");
        _httpClient.BaseAddress = new Uri("https://app.mcp.ai/");

        var apiKey = configuration["BancoMcp:ApiKey"]
            ?? throw new InvalidOperationException("BancoMcp:ApiKey não configurada.");
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<BankMcpInviteResult> CreateUserInviteAsync(string toolkitId, string label, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/toolkits/{toolkitId}/invites",
            new { label },
            ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<InviteRaw>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Resposta vazia ao criar convite");

        return new BankMcpInviteResult(
            result.Id ?? string.Empty,
            result.Label ?? string.Empty,
            result.ConnectUrl ?? string.Empty);
    }

    // POCO de desserialização — confirmar nomes dos campos contra response real da Management API.
    private record InviteRaw(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("label")] string? Label,
        [property: JsonPropertyName("connect_url")] string? ConnectUrl);
}
