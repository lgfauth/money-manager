namespace MoneyManager.Domain.Exceptions;

public class BankMcpKeyExpiredException : Exception
{
    public BankMcpKeyExpiredException()
        : base("API key do Banco MCP expirada ou revogada. Atualize a chave nas configurações.")
    {
    }
}