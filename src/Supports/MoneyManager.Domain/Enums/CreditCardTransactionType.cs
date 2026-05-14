namespace MoneyManager.Domain.Enums;

/// <summary>
/// Tipos de transações de cartão de crédito.
/// </summary>
public enum CreditCardTransactionType
{
    /// <summary>
    /// Compra comum — reduz o limite disponível.
    /// </summary>
    Purchase = 0,

    /// <summary>
    /// Estorno — libera limite ao reverter uma compra anterior.
    /// </summary>
    Refund = 1
}
