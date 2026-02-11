using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using System.Net.Http.Json;

namespace MoneyManager.Web.Services;

public class CreditCardInvoiceService : ICreditCardInvoiceService
{
    private readonly HttpClient _httpClient;

    public CreditCardInvoiceService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CreditCardInvoice> GetOrCreateOpenInvoiceAsync(string accountId)
    {
        var response = await _httpClient.GetAsync($"api/credit-card-invoices/accounts/{accountId}/open");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreditCardInvoice>() 
            ?? throw new InvalidOperationException("Failed to get or create open invoice");
    }

    public async Task<CreditCardInvoiceResponseDto> GetInvoiceByIdAsync(string invoiceId)
    {
        return await _httpClient.GetFromJsonAsync<CreditCardInvoiceResponseDto>($"api/credit-card-invoices/{invoiceId}")
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found");
    }

    public async Task<IEnumerable<CreditCardInvoiceResponseDto>> GetInvoicesByAccountAsync(string accountId)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<CreditCardInvoiceResponseDto>>($"api/credit-card-invoices/accounts/{accountId}")
            ?? Array.Empty<CreditCardInvoiceResponseDto>();
    }

    public async Task<IEnumerable<CreditCardInvoiceResponseDto>> GetPendingInvoicesAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<CreditCardInvoiceResponseDto>>("api/credit-card-invoices/pending")
            ?? Array.Empty<CreditCardInvoiceResponseDto>();
    }

    public async Task<IEnumerable<CreditCardInvoiceResponseDto>> GetOverdueInvoicesAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<CreditCardInvoiceResponseDto>>("api/credit-card-invoices/overdue")
            ?? Array.Empty<CreditCardInvoiceResponseDto>();
    }

    public async Task<CreditCardInvoiceResponseDto> CloseInvoiceAsync(string invoiceId)
    {
        var response = await _httpClient.PostAsync($"api/credit-card-invoices/{invoiceId}/close", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreditCardInvoiceResponseDto>()
            ?? throw new InvalidOperationException("Failed to close invoice");
    }

    public async Task PayInvoiceAsync(PayInvoiceRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/credit-card-invoices/pay", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task PayPartialInvoiceAsync(PayInvoiceRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/credit-card-invoices/pay-partial", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<InvoiceSummaryDto> GetInvoiceSummaryAsync(string invoiceId)
    {
        return await _httpClient.GetFromJsonAsync<InvoiceSummaryDto>($"api/credit-card-invoices/{invoiceId}/summary")
            ?? throw new KeyNotFoundException($"Invoice summary for {invoiceId} not found");
    }

    public async Task<IEnumerable<TransactionResponseDto>> GetInvoiceTransactionsAsync(string invoiceId)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<TransactionResponseDto>>($"api/credit-card-invoices/{invoiceId}/transactions")
            ?? Array.Empty<TransactionResponseDto>();
    }

    public async Task<CreditCardInvoice> DetermineInvoiceForTransactionAsync(string accountId, DateTime transactionDate)
    {
        var response = await _httpClient.GetAsync($"api/credit-card-invoices/accounts/{accountId}/determine?transactionDate={transactionDate:O}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreditCardInvoice>()
            ?? throw new InvalidOperationException("Failed to determine invoice");
    }

    public async Task RecalculateInvoiceTotalAsync(string invoiceId)
    {
        var response = await _httpClient.PostAsync($"api/credit-card-invoices/{invoiceId}/recalculate", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<CreditCardInvoice> CreateHistoryInvoiceAsync(string accountId)
    {
        var response = await _httpClient.PostAsync($"api/credit-card-invoices/accounts/{accountId}/history", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreditCardInvoice>()
            ?? throw new InvalidOperationException("Failed to create history invoice");
    }
}
