namespace MoneyManager.Domain.Exceptions;

public class PremiumRequiredException : Exception
{
    public PremiumRequiredException()
        : base("Recurso disponível apenas no plano Premium") { }
}
