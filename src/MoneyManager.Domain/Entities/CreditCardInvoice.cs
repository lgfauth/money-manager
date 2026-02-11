using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Domain.Entities;

/// <summary>
/// Representa uma fatura de cartão de crédito
/// </summary>
public class CreditCardInvoice
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// ID da conta (cartão de crédito) que esta fatura pertence
    /// </summary>
    [BsonElement("accountId")]
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// ID do usuário dono do cartão
    /// </summary>
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Data de início do período da fatura (dia seguinte ao fechamento anterior)
    /// </summary>
    [BsonElement("periodStart")]
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Data de fechamento da fatura (último dia que aceita transações)
    /// </summary>
    [BsonElement("periodEnd")]
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Data de vencimento da fatura (geralmente 7 dias após o fechamento)
    /// </summary>
    [BsonElement("dueDate")]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Valor total da fatura (soma de todas as transações)
    /// </summary>
    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Valor já pago da fatura
    /// </summary>
    [BsonElement("paidAmount")]
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// Valor restante a pagar (TotalAmount - PaidAmount)
    /// </summary>
    [BsonElement("remainingAmount")]
    public decimal RemainingAmount { get; set; }

    /// <summary>
    /// Status atual da fatura
    /// </summary>
    [BsonElement("status")]
    public InvoiceStatus Status { get; set; }

    /// <summary>
    /// Data/hora em que a fatura foi fechada
    /// </summary>
    [BsonElement("closedAt")]
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// Data/hora em que a fatura foi totalmente paga
    /// </summary>
    [BsonElement("paidAt")]
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// Referência para o mês/ano da fatura (ex: "2026-02" para fevereiro de 2026)
    /// Facilita buscas e organização
    /// </summary>
    [BsonElement("referenceMonth")]
    public string ReferenceMonth { get; set; } = string.Empty;

    /// <summary>
    /// Indica se a fatura foi deletada (soft delete)
    /// </summary>
    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Data de criação da fatura
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data da última atualização
    /// </summary>
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
