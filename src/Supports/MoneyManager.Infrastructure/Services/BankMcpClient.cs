using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MoneyManager.Domain.Exceptions;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Infrastructure.Services;

// Todos os endpoints são POST. Response sempre em { "ok": true, "tool": "...", "result": { ... } }.
// Valores monetários (balance, amount) são strings — parsear com decimal.Parse + InvariantCulture.
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

    public async Task<BankMcpListConnectionsResult> ListConnectionsAsync(string apiKey, CancellationToken ct)
    {
        using var request = BuildRequest(HttpMethod.Post, "connections/list", apiKey);
        var response = await SendAsync<ListConnectionsRaw>(request, ct);

        return new BankMcpListConnectionsResult(
            response.Connections.Select(c => new BankMcpConnection(
                c.ItemId, c.ConnectorId, c.ConnectorName, c.Status)).ToList(),
            response.Count,
            response.AddConnectionUrl ?? string.Empty);
    }

    public async Task<BankMcpConnectionStatus> GetConnectionStatusAsync(string apiKey, string item, CancellationToken ct)
    {
        using var request = BuildRequest(HttpMethod.Post, "connections/status", apiKey);
        request.Content = JsonContent.Create(new { item });
        var response = await SendAsync<GetItemStatusRaw>(request, ct);

        return new BankMcpConnectionStatus(
            response.Id,
            response.Status,
            response.ExecutionStatus ?? string.Empty,
            response.LastUpdatedAt ?? DateTime.UtcNow);
    }

    public async Task SyncConnectionsAsync(string apiKey, IEnumerable<string> items, CancellationToken ct)
    {
        using var request = BuildRequest(HttpMethod.Post, "connections/sync", apiKey);
        request.Content = JsonContent.Create(new { items });
        await SendAsync<object>(request, ct);
    }

    public async Task DisconnectAsync(string apiKey, string item, CancellationToken ct)
    {
        using var request = BuildRequest(HttpMethod.Post, "connections/disconnect", apiKey);
        request.Content = JsonContent.Create(new { item });
        await SendAsync<object>(request, ct);
    }

    public async Task<IReadOnlyList<BankMcpAccount>> ListAccountsAsync(string apiKey, string item, CancellationToken ct)
    {
        using var request = BuildRequest(HttpMethod.Post, "accounts/list", apiKey);
        request.Content = JsonContent.Create(new { item });
        var response = await SendAsync<ListAccountsRaw>(request, ct);

        return response.Results.Select(a => new BankMcpAccount(
            a.AccountId ?? a.Id,                           // account_id (= id)
            a.Type,
            a.Subtype ?? string.Empty,
            a.Bank ?? a.Name ?? string.Empty,               // campo "bank" para exibição
            a.Number ?? string.Empty,
            decimal.Parse(a.Balance ?? "0", CultureInfo.InvariantCulture),
            a.ItemId ?? string.Empty,
            a.ConnectorId ?? string.Empty,
            ParseNullableDecimal(a.CreditData?.CreditLimit),
            ParseNullableDecimal(a.CreditData?.AvailableCreditLimit),
            ParseNullableDecimal(a.CreditData?.MinimumPayment),
            a.CreditData?.Brand,
            a.CreditData?.Level,
            ParseNullableDate(a.CreditData?.BalanceDueDate),
            ParseNullableDate(a.CreditData?.BalanceCloseDate))).ToList();
    }

    public async Task<BankMcpOpenBillResult?> GetOpenBillAsync(string apiKey, string accountId, CancellationToken ct)
    {
        using var request = BuildRequest(HttpMethod.Post, "credit-card-bills/list", apiKey);
        request.Content = JsonContent.Create(new
        {
            account_id = accountId,
            include_open_bill = true,
            page_size = 1
        });

        var response = await SendAsync<CreditCardBillsResponse>(request, ct);

        if (response.OpenBill is null || !response.OpenBill.Available)
            return null;

        return new BankMcpOpenBillResult(
            response.OpenBill.Available,
            ParseNullableDecimal(response.OpenBill.TotalAmount) ?? 0m,
            ParseNullableDate(response.OpenBill.CloseDate),
            ParseNullableDate(response.OpenBill.DueDate),
            response.OpenBill.TransactionCount,
            ParseNullableDecimal(response.TotalPendingDebt) ?? 0m);
    }

    public async Task<BankMcpTransactionPage> ListTransactionsAsync(
        string apiKey, string accountId, DateTime from, DateTime to,
        int page, int pageSize, CancellationToken ct)
    {
        ListTransactionsRaw response;
        var maxAttempts = 3;

        for (var attempt = 1; ; attempt++)
        {
            using var request = BuildRequest(HttpMethod.Post, "transactions/list", apiKey);
            request.Content = JsonContent.Create(new
            {
                account_id = accountId,
                from = from.ToString("yyyy-MM-dd"),
                to = to.ToString("yyyy-MM-dd"),
                page,
                page_size = pageSize
            });

            try
            {
                response = await SendAsync<ListTransactionsRaw>(request, ct);
                break;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests && attempt < maxAttempts)
            {
                var delayMs = (int)Math.Pow(2, attempt - 1) * 1000;
                await Task.Delay(delayMs, ct);
            }
        }

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

    private async Task<TResult> SendAsync<TResult>(HttpRequestMessage request, CancellationToken ct)
    {
        var httpResponse = await _httpClient.SendAsync(request, ct);
        if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new BankMcpKeyExpiredException();

        httpResponse.EnsureSuccessStatusCode();

        // Desembrulha o envelope { "ok": true, "tool": "...", "result": { ... } }
        var envelope = await httpResponse.Content
            .ReadFromJsonAsync<BankMcpEnvelope<TResult>>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Resposta vazia do Banco MCP");

        if (!envelope.Ok)
            throw new InvalidOperationException("Erro na API do Banco MCP");

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
        [property: JsonPropertyName("connector_id")] string? ConnectorId,
        [property: JsonPropertyName("creditData")] CreditDataRaw? CreditData);

    private record CreditDataRaw(
        [property: JsonPropertyName("creditLimit")] JsonElement? CreditLimit,
        [property: JsonPropertyName("availableCreditLimit")] JsonElement? AvailableCreditLimit,
        [property: JsonPropertyName("minimumPayment")] JsonElement? MinimumPayment,
        [property: JsonPropertyName("brand")] string? Brand,
        [property: JsonPropertyName("level")] string? Level,
        [property: JsonPropertyName("balanceDueDate")] string? BalanceDueDate,
        [property: JsonPropertyName("balanceCloseDate")] string? BalanceCloseDate);

    private record CreditCardBillsResponse(
        [property: JsonPropertyName("open_bill")] OpenBillRaw? OpenBill,
        [property: JsonPropertyName("total_pending_debt")] string? TotalPendingDebt);

    private record OpenBillRaw(
        [property: JsonPropertyName("available")] bool Available,
        [property: JsonPropertyName("total_amount")] string? TotalAmount,
        [property: JsonPropertyName("close_date")] string? CloseDate,
        [property: JsonPropertyName("due_date")] string? DueDate,
        [property: JsonPropertyName("transaction_count")] int TransactionCount);

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

    private static decimal? ParseNullableDecimal(JsonElement? value)
    {
        if (!value.HasValue)
            return null;

        if (value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetDecimal(out var number))
            return number;

        if (value.Value.ValueKind == JsonValueKind.String)
        {
            var raw = value.Value.GetString();
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
        }

        return null;
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static DateTime? ParseNullableDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
    }
}
