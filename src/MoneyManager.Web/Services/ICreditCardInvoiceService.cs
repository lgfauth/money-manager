using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;

namespace MoneyManager.Web.Services;

public interface ICreditCardInvoiceService
{
    // Gestão de Faturas
    Task<CreditCardInvoice> GetOrCreateOpenInvoiceAsync(string accountId);
    Task<CreditCardInvoiceResponseDto> GetInvoiceByIdAsync(string invoiceId);
    Task<IEnumerable<CreditCardInvoiceResponseDto>> GetInvoicesByAccountAsync(string accountId);
    Task<IEnumerable<CreditCardInvoiceResponseDto>> GetPendingInvoicesAsync();
    Task<IEnumerable<CreditCardInvoiceResponseDto>> GetOverdueInvoicesAsync();
    
    // Fechamento
    Task<CreditCardInvoiceResponseDto> CloseInvoiceAsync(string invoiceId);
    
    // Pagamento
    Task PayInvoiceAsync(PayInvoiceRequestDto request);
    Task PayPartialInvoiceAsync(PayInvoiceRequestDto request);
    
    // Relatórios
    Task<InvoiceSummaryDto> GetInvoiceSummaryAsync(string invoiceId);
    Task<IEnumerable<TransactionResponseDto>> GetInvoiceTransactionsAsync(string invoiceId);
    
    // Utilitários
    Task<CreditCardInvoice> DetermineInvoiceForTransactionAsync(string accountId, DateTime transactionDate);
    Task RecalculateInvoiceTotalAsync(string invoiceId);
    Task<CreditCardInvoice> CreateHistoryInvoiceAsync(string accountId);
}
