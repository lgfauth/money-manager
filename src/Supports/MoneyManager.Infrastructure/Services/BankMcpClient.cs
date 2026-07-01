using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Infrastructure.Services;

// Todos os endpoints são POST. Response sempre em { "ok": true, "tool": "...", "result": { ... } }.
// Valores monetários (balance, amount) são strings — parsear com decimal.Parse + InvariantCulture.
public class BankMcpClient : IBankMcpClient
{
    private readonly HttpClient _httpClient;

    public BankMcpClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("bancoMcp");
        _httpClient.BaseAddress = new Uri("https://api.mcp.ai/api/openfinance/");

        var apiKey = configuration["BancoMcp:ApiKey"]
            ?? throw new InvalidOperationException("BancoMcp:ApiKey não configurada.");
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<BankMcpListConnectionsResult> ListConnectionsAsync(CancellationToken ct)
    {
        var response = await PostAsync<object, ListConnectionsRaw>("connections/list", null, ct);

        return new BankMcpListConnectionsResult(
            response.Connections.Select(c => new BankMcpConnection(
                c.ItemId, c.ConnectorId, c.ConnectorName, c.Status)).ToList(),
            response.Count,
            response.AddConnectionUrl ?? string.Empty);
    }

    public async Task<BankMcpConnectionStatus> GetConnectionStatusAsync(string item, CancellationToken ct)
    {
        var response = await PostAsync<object, GetItemStatusRaw>("connections/status", new { item }, ct);

        return new BankMcpConnectionStatus(
            response.Id,
            response.Status,
            response.ExecutionStatus ?? string.Empty,
            response.LastUpdatedAt ?? DateTime.UtcNow);
    }

    public async Task SyncConnectionsAsync(IEnumerable<string> items, CancellationToken ct)
    {
        await PostAsync<object, object>("connections/sync", new { items }, ct);
    }

    public async Task DisconnectAsync(string item, CancellationToken ct)
    {
        await PostAsync<object, object>("connections/disconnect", new { item }, ct);
    }

    public async Task<IReadOnlyList<BankMcpAccount>> ListAccountsAsync(string item, CancellationToken ct)
    {
        var response = await PostAsync<object, ListAccountsRaw>("accounts/list", new { item }, ct);

        return response.Results.Select(a => new BankMcpAccount(
            a.AccountId ?? a.Id,                           // account_id (= id)
            a.Type,
            a.Subtype ?? string.Empty,
            a.Bank ?? a.Name ?? string.Empty,               // campo "bank" para exibição
            a.Number ?? string.Empty,
            decimal.Parse(a.Balance ?? "0", CultureInfo.InvariantCulture),
            a.ItemId ?? string.Empty,
            a.ConnectorId ?? string.Empty)).ToList();
    }

    public async Task<BankMcpTransactionPage> ListTransactionsAsync(
        string accountId, DateTime from, DateTime to,
        int page, int pageSize, CancellationToken ct)
    {
        var body = new
        {
            account_id = accountId,
            from = from.ToString("yyyy-MM-dd"),
            to = to.ToString("yyyy-MM-dd"),
            page,
            page_size = pageSize
        };

        var response = await PostAsync<object, ListTransactionsRaw>("transactions/list", body, ct);

        return new BankMcpTransactionPage(
            response.Total,
            response.Page,
            response.TotalPages,
            response.Results.Select(t => new BankMcpTransaction(
                t.Id,
                accountId,                                  // repassado do request (não vem no body)
                DateTime.Parse(t.Date, CultureInfo.InvariantCulture),
                t.Description ?? string.Empty,
                decimal.Parse(t.Amount ?? "0", CultureInfo.InvariantCulture),
                t.Type ?? (t.Amount?.StartsWith("-") == true ? "DEBIT" : "CREDIT"),
                t.Status ?? "POSTED",
                t.Category,
                t.CategoryId,
                t.OperationType)).ToList());
    }

    // ── Helper genérico ────────────────────────────────────────────────────

    private async Task<TResult> PostAsync<TBody, TResult>(string path, TBody? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);

        if (body is not null)
            request.Content = JsonContent.Create(body);

        var httpResponse = await _httpClient.SendAsync(request, ct);
        httpResponse.EnsureSuccessStatusCode();

        // Desembrulha o envelope { "ok": true, "tool": "...", "result": { ... } }
        var envelope = await httpResponse.Content
            .ReadFromJsonAsync<BankMcpEnvelope<TResult>>(cancellationToken: ct)
            ?? throw new InvalidOperationException($"Resposta vazia de {path}");

        if (!envelope.Ok)
            throw new InvalidOperationException($"Erro na API do Banco MCP: {path}");

        return envelope.Result;
    }

    // ── Envelope e POCOs de desserialização ────────────────────────────────

    private record BankMcpEnvelope<T>(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("result")] T Result);

    private record ListConnectionsRaw(
        [property: JsonPropertyName("connections")] List<ConnectionRaw> Connections,
        [property: JsonPropertyName("count")] int Count,
        [property: JsonPropertyName("add_connection_url")] string? AddConnectionUrl);

    private record ConnectionRaw(
        [property: JsonPropertyName("item_id")] string ItemId,
        [property: JsonPropertyName("connector_id")] string ConnectorId,
        [property: JsonPropertyName("connector_name")] string ConnectorName,
        [property: JsonPropertyName("status")] string Status);

    private record GetItemStatusRaw(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("executionStatus")] string? ExecutionStatus,
        [property: JsonPropertyName("lastUpdatedAt")] DateTime? LastUpdatedAt);

    private record ListAccountsRaw(
        [property: JsonPropertyName("results")] List<AccountRaw> Results);

    private record AccountRaw(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("account_id")] string? AccountId,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("subtype")] string? Subtype,
        [property: JsonPropertyName("bank")] string? Bank,        // nome de exibição
        [property: JsonPropertyName("name")] string? Name,        // razão social (fallback)
        [property: JsonPropertyName("number")] string? Number,
        [property: JsonPropertyName("balance")] string? Balance,   // STRING — parsear
        [property: JsonPropertyName("item_id")] string? ItemId,
        [property: JsonPropertyName("connector_id")] string? ConnectorId);

    private record ListTransactionsRaw(
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("page")] int Page,
        [property: JsonPropertyName("totalPages")] int TotalPages,
        [property: JsonPropertyName("results")] List<TransactionRaw> Results);

    private record TransactionRaw(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("date")] string Date,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("amount")] string? Amount,     // STRING — parsear
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("category")] string? Category,
        [property: JsonPropertyName("categoryId")] string? CategoryId,
        [property: JsonPropertyName("operationType")] string? OperationType);
}
