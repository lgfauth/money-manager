namespace MoneyManager.Domain.Interfaces;

public interface IBankMcpClient
{
    Task<BankMcpListConnectionsResult> ListConnectionsAsync(string apiKey, CancellationToken ct);
    Task<BankMcpConnectionStatus> GetConnectionStatusAsync(string apiKey, string item, CancellationToken ct);
    Task SyncConnectionsAsync(string apiKey, IEnumerable<string> items, CancellationToken ct);
    Task DisconnectAsync(string apiKey, string item, CancellationToken ct);
    Task<IReadOnlyList<BankMcpAccount>> ListAccountsAsync(string apiKey, string item, CancellationToken ct);
    Task<BankMcpOpenBillResult?> GetOpenBillAsync(string apiKey, string accountId, CancellationToken ct);
    Task<BankMcpTransactionPage> ListTransactionsAsync(
        string apiKey, string accountId, DateTime from, DateTime to,
        int page, int pageSize, CancellationToken ct);
}

public record BankMcpListConnectionsResult(
    IReadOnlyList<BankMcpConnection> Connections,
    int Count,
    string AddConnectionUrl); // URL pronta para o usuário adicionar mais bancos

public record BankMcpConnection(
    string ItemId,             // item_id — chave primária da conexão
    string ConnectorId,        // "612"
    string ConnectorName,      // "Nubank"
    string Status);            // "UPDATED" | "LOGIN_ERROR" | "UPDATING"

public record BankMcpConnectionStatus(
    string ItemId,
    string Status,
    string ExecutionStatus,
    DateTime LastUpdatedAt);

public record BankMcpAccount(
    string AccountId,          // account_id = id — usar para listar transações
    string Type,                // "BANK" | "CREDIT"
    string Subtype,             // "CHECKING_ACCOUNT" | "CREDIT_CARD"
    string DisplayName,          // campo "bank" do response (NÃO o campo "name")
    string Number,              // número da conta ou final do cartão
    decimal Balance,            // parseado de string
    string ItemId,              // item_id da conexão pai
    string ConnectorId,         // connector_id da conexão pai
    decimal? CreditLimit,       // creditData.creditLimit
    decimal? AvailableCreditLimit,
    decimal? MinimumPayment,
    string? Brand,
    string? CardLevel,
    DateTime? BalanceDueDate,
    DateTime? BalanceCloseDate);

public record BankMcpOpenBillResult(
    bool Available,
    decimal TotalAmount,
    DateTime? CloseDate,
    DateTime? DueDate,
    int TransactionCount,
    decimal TotalPendingDebt);

public record BankMcpTransactionPage(
    int Total,
    int Page,
    int TotalPages,
    IReadOnlyList<BankMcpTransaction> Results);

public record BankMcpTransaction(
    string Id,                  // id — ExternalId para deduplicação
    string AccountId,           // repassado do parâmetro (não vem no body do response)
    DateTime Date,               // "date" ISO datetime
    string Description,          // "description"
    decimal Amount,              // parseado de string (negativo = débito)
    string Type,                 // "DEBIT" | "CREDIT"
    string Status,               // "POSTED" | "PENDING"
    string? Category,            // "Transfers", "Investments" etc
    string? CategoryId,          // "05000000"
    string? OperationType);      // "PIX", "RESGATE_APLIC_FINANCEIRA" etc
