namespace MoneyManager.Domain.Interfaces;

public interface IBankMcpClient
{
    Task<IReadOnlyList<BankMcpConnection>> ListConnectionsAsync(string apiKey, CancellationToken ct);
    Task<BankMcpConnectionStatus> GetConnectionStatusAsync(string apiKey, string externalConnectionId, CancellationToken ct);
    Task SyncConnectionAsync(string apiKey, IEnumerable<string> externalConnectionIds, CancellationToken ct);
    Task DisconnectAsync(string apiKey, string externalConnectionId, CancellationToken ct);
    Task<IReadOnlyList<BankMcpAccount>> ListAccountsAsync(string apiKey, string externalConnectionId, CancellationToken ct);
    Task<IReadOnlyList<BankMcpTransaction>> ListTransactionsAsync(
        string apiKey,
        string externalConnectionId,
        string externalAccountId,
        DateTime since,
        CancellationToken ct);
}

public record BankMcpConnection(
    string ExternalConnectionId,
    string InstitutionName,
    string? InstitutionLogo,
    string Status);

public record BankMcpConnectionStatus(
    string Id,
    string Status,
    DateTime? LastUpdatedAt);

public record BankMcpAccount(
    string ExternalAccountId,
    string Name,
    string Type,
    string InstitutionName,
    string? InstitutionLogo,
    decimal Balance);

public record BankMcpTransaction(
    string ExternalId,
    string ExternalAccountId,
    DateTime Date,
    string Description,
    decimal Amount,
    string Type,
    string? Category,
    string Status);
