namespace MoneyManager.Domain.Interfaces;

public interface IBankMcpManagementClient
{
    // Cria convite de guest no toolkit — gera URL única por usuário MoneyManager.
    Task<BankMcpInviteResult> CreateUserInviteAsync(string toolkitId, string label, CancellationToken ct);
}

public record BankMcpInviteResult(
    string InviteId,
    string Label,
    string ConnectUrl); // URL com u=usr_... único — usuário abre pra conectar o banco
