using System.Net.Http.Json;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Infrastructure.Services;

public class BankMcpClient : IBankMcpClient
{
    private readonly HttpClient _httpClient;

    public BankMcpClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("bancoMcp");
        _httpClient.BaseAddress = new Uri("https://api.mcp.ai/api/openfinance/");
    }

    private static HttpRequestMessage BuildRequest(HttpMethod method, string path, string apiKey)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        return request;
    }

    public async Task<IReadOnlyList<BankMcpConnection>> ListConnectionsAsync(string apiKey, CancellationToken ct)
    {
        using var request = BuildRequest(HttpMethod.Get, "connections/list", apiKey);
        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<BankMcpConnectionsResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Resposta vazia de connections/list");

        return payload.Connections.Select(c => new BankMcpConnection(
            c.Id,
            c.Connector?.Name ?? "Banco",
            c.Connector?.ImageUrl,
            c.Status)).ToList();
    }

    public async Task<BankMcpConnectionStatus> GetConnectionStatusAsync(string apiKey, string externalConnectionId, CancellationToken ct)
    {
        using var request = BuildRequest(HttpMethod.Get, $"connections/status?connectionId={externalConnectionId}", apiKey);
        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<BankMcpConnectionStatusResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Resposta vazia de connections/status");

        return new BankMcpConnectionStatus(payload.Id, payload.Status, payload.LastUpdatedAt);
    }

    public async Task SyncConnectionAsync(string apiKey, IEnumerable<string> externalConnectionIds, CancellationToken ct)
    {
        using var request = BuildRequest(HttpMethod.Post, "connections/sync", apiKey);
        request.Content = JsonContent.Create(new { connectionIds = externalConnectionIds });
        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DisconnectAsync(string apiKey, string externalConnectionId, CancellationToken ct)
    {
        using var request = BuildRequest(HttpMethod.Post, "connections/disconnect", apiKey);
        request.Content = JsonContent.Create(new { connectionId = externalConnectionId });
        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<BankMcpAccount>> ListAccountsAsync(string apiKey, string externalConnectionId, CancellationToken ct)
    {
        using var request = BuildRequest(HttpMethod.Get, $"accounts/list?connectionId={externalConnectionId}", apiKey);
        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<BankMcpAccountsResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Resposta vazia de accounts/list");

        return payload.Accounts.Select(a => new BankMcpAccount(
            a.Id,
            a.Name,
            a.Type,
            a.Institution?.Name ?? "Banco",
            a.Institution?.ImageUrl,
            a.Balance?.Available ?? 0)).ToList();
    }

    public async Task<IReadOnlyList<BankMcpTransaction>> ListTransactionsAsync(
        string apiKey, string externalConnectionId, string externalAccountId, DateTime since, CancellationToken ct)
    {
        var sinceStr = since.ToString("yyyy-MM-dd");
        var path = $"transactions/list?connectionId={externalConnectionId}&accountId={externalAccountId}&from={sinceStr}";

        using var request = BuildRequest(HttpMethod.Get, path, apiKey);
        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<BankMcpTransactionsResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Resposta vazia de transactions/list");

        return payload.Transactions.Select(t => new BankMcpTransaction(
            t.Id,
            t.AccountId,
            t.Date,
            t.Description,
            t.Amount,
            t.Amount < 0 ? "DEBIT" : "CREDIT",
            t.Category?.Name,
            t.Status ?? "POSTED")).ToList();
    }

    // ── POCOs de desserialização ──────────────────────────────────────────────

    private record BankMcpConnectionsResponse(List<BankMcpConnectionRaw> Connections);
    private record BankMcpConnectionRaw(string Id, string Status, BankMcpConnectorRaw? Connector, DateTime? LastUpdatedAt);
    private record BankMcpConnectorRaw(string Name, string? ImageUrl);
    private record BankMcpConnectionStatusResponse(string Id, string Status, DateTime? LastUpdatedAt);
    private record BankMcpAccountsResponse(List<BankMcpAccountRaw> Accounts);
    private record BankMcpAccountRaw(string Id, string Name, string Type, BankMcpInstitutionRaw? Institution, BankMcpBalanceRaw? Balance);
    private record BankMcpInstitutionRaw(string Name, string? ImageUrl);
    private record BankMcpBalanceRaw(decimal Available, decimal Current);
    private record BankMcpTransactionsResponse(List<BankMcpTransactionRaw> Transactions);
    private record BankMcpTransactionRaw(string Id, string AccountId, DateTime Date, string Description, decimal Amount, BankMcpCategoryRaw? Category, string? Status);
    private record BankMcpCategoryRaw(string Name);
}
