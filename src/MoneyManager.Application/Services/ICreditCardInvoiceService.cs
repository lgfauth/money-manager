using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;

namespace MoneyManager.Application.Services;

/// <summary>
/// Serviço para gerenciar faturas de cartão de crédito
/// </summary>
public interface ICreditCardInvoiceService
{
    // ==================== GESTÃO DE FATURAS ====================
    
    /// <summary>
    /// Busca ou cria a fatura aberta (Open) atual de um cartão
    /// </summary>
    Task<CreditCardInvoice> GetOrCreateOpenInvoiceAsync(string userId, string accountId);

    /// <summary>
    /// Busca fatura por ID
    /// </summary>
    Task<CreditCardInvoiceResponseDto> GetInvoiceByIdAsync(string userId, string invoiceId);

    /// <summary>
    /// Busca todas as faturas de um cartão
    /// </summary>
    Task<IEnumerable<CreditCardInvoiceResponseDto>> GetInvoicesByAccountAsync(string userId, string accountId);

    /// <summary>
    /// Busca faturas fechadas e não pagas (pendentes de pagamento)
    /// </summary>
    Task<IEnumerable<CreditCardInvoiceResponseDto>> GetPendingInvoicesAsync(string userId);

    /// <summary>
    /// Busca faturas vencidas
    /// </summary>
    Task<IEnumerable<CreditCardInvoiceResponseDto>> GetOverdueInvoicesAsync(string userId);

    // ==================== FECHAMENTO DE FATURAS ====================
    
    /// <summary>
    /// Fecha uma fatura manualmente
    /// </summary>
    Task<CreditCardInvoiceResponseDto> CloseInvoiceAsync(string userId, string invoiceId);

    /// <summary>
    /// Processa fechamento automático de faturas (chamado pelo Worker)
    /// </summary>
    Task ProcessMonthlyInvoiceClosuresAsync();

    // ==================== PAGAMENTO DE FATURAS ====================
    
    /// <summary>
    /// Paga uma fatura totalmente (atualiza apenas o status da fatura, não cria transação)
    /// A transação de pagamento deve ser criada separadamente via TransactionService
    /// </summary>
    Task PayInvoiceAsync(string userId, PayInvoiceRequestDto request);

    /// <summary>
    /// Paga uma fatura parcialmente (atualiza apenas o status da fatura, não cria transação)
    /// A transação de pagamento deve ser criada separadamente via TransactionService
    /// </summary>
    Task PayPartialInvoiceAsync(string userId, PayInvoiceRequestDto request);

    // ==================== RELATÓRIOS ====================
    
    /// <summary>
    /// Retorna resumo detalhado de uma fatura (com transações)
    /// </summary>
    Task<InvoiceSummaryDto> GetInvoiceSummaryAsync(string userId, string invoiceId);

    /// <summary>
    /// Retorna todas as transações de uma fatura
    /// </summary>
    Task<IEnumerable<TransactionResponseDto>> GetInvoiceTransactionsAsync(string userId, string invoiceId);

    // ==================== UTILITÁRIOS ====================
    
    /// <summary>
    /// Determina a qual fatura uma transação deve pertencer baseado na data
    /// </summary>
    Task<CreditCardInvoice> DetermineInvoiceForTransactionAsync(string userId, string accountId, DateTime transactionDate);

    /// <summary>
    /// Atualiza o valor total de uma fatura recalculando todas as transações vinculadas
    /// </summary>
    Task RecalculateInvoiceTotalAsync(string userId, string invoiceId);

    /// <summary>
    /// Cria fatura "Histórico" para migração de dados antigos
    /// </summary>
    Task<CreditCardInvoice> CreateHistoryInvoiceAsync(string userId, string accountId);
}
