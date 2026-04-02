namespace MoneyManager.Domain.Exceptions;

public class ConcurrencyException : Exception
{
    public ConcurrencyException()
        : base("The record was modified by another process. Please reload and try again.") { }

    public ConcurrencyException(string message)
        : base(message) { }

    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException) { }
}
