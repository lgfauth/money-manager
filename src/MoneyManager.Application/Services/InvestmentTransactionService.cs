using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Application.Services;

/// <summary>
/// Implementação do serviço de transações de investimento.
/// </summary>
public class InvestmentTransactionService : IInvestmentTransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccountService _accountService;
    private readonly ILogger<InvestmentTransactionService> _logger;

    public InvestmentTransactionService(
        IUnitOfWork unitOfWork,
        IAccountService accountService,
        ILogger<InvestmentTransactionService> logger)
    {
        _unitOfWork = unitOfWork;
        _accountService = accountService;
        _logger = logger;
    }

    public async Task<IEnumerable<InvestmentTransactionResponseDto>> GetByAssetIdAsync(string assetId)
    {
        var transactions = await _unitOfWork.InvestmentTransactions.GetByAssetIdAsync(assetId);
        var asset = await _unitOfWork.InvestmentAssets.GetByIdAsync(assetId);

        return transactions.Select(t => MapToDto(t, asset?.Name, asset?.Ticker));
    }

    public async Task<IEnumerable<InvestmentTransactionResponseDto>> GetByUserIdAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        IEnumerable<InvestmentTransaction> transactions;

        if (startDate.HasValue && endDate.HasValue)
        {
            transactions = await _unitOfWork.InvestmentTransactions.GetByUserIdAsync(userId, startDate.Value, endDate.Value);
        }
        else
        {
            transactions = await _unitOfWork.InvestmentTransactions.GetByUserIdAsync(userId);
        }

        // Buscar nomes dos ativos
        var assets = await _unitOfWork.InvestmentAssets.GetByUserIdAsync(userId);
        var assetDict = assets.ToDictionary(a => a.Id);

        return transactions.Select(t =>
        {
            assetDict.TryGetValue(t.AssetId, out var asset);
            return MapToDto(t, asset?.Name, asset?.Ticker);
        });
    }

    public async Task<InvestmentTransactionResponseDto> RecordYieldAsync(string userId, RecordYieldRequestDto request)
    {
        _logger.LogInformation("Recording yield for asset {AssetId}, amount: {Amount}, type: {YieldType}", 
            request.AssetId, request.Amount, request.YieldType);

        // Validar ativo
        var asset = await _unitOfWork.InvestmentAssets.GetByIdAsync(request.AssetId);
        
        if (asset == null || asset.UserId != userId || asset.IsDeleted)
        {
            throw new KeyNotFoundException("Asset not found");
        }

        // Validar tipo de rendimento
        if (request.YieldType != InvestmentTransactionType.Dividend &&
            request.YieldType != InvestmentTransactionType.Interest &&
            request.YieldType != InvestmentTransactionType.YieldPayment)
        {
            throw new InvalidOperationException("Tipo de rendimento inválido. Use Dividend, Interest ou YieldPayment.");
        }

        // Criar transação de investimento
        var investmentTransaction = new InvestmentTransaction
        {
            UserId = userId,
            AssetId = request.AssetId,
            AccountId = asset.AccountId,
            TransactionType = request.YieldType,
            Quantity = 0, // Rendimentos não envolvem quantidade
            Price = request.Amount, // Para rendimentos, o "preço" é o valor recebido
            Fees = 0,
            Date = request.Date,
            Description = request.Description ?? $"Rendimento de {asset.Name}"
        };

        investmentTransaction.CalculateTotalAmount();
        await _unitOfWork.InvestmentTransactions.AddAsync(investmentTransaction);

        // Criar transação regular (crédito na conta)
        var regularTransaction = new Transaction
        {
            UserId = userId,
            AccountId = asset.AccountId,
            Type = TransactionType.InvestmentYield,
            Amount = request.Amount,
            Date = request.Date,
            Description = investmentTransaction.Description,
            Status = TransactionStatus.Completed
        };

        await _unitOfWork.Transactions.AddAsync(regularTransaction);
        investmentTransaction.LinkedTransactionId = regularTransaction.Id;

        // Atualizar saldo da conta
        await _accountService.UpdateBalanceAsync(userId, asset.AccountId, request.Amount);

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Yield recorded successfully for asset {AssetId}. Transaction ID: {TransactionId}", 
            request.AssetId, investmentTransaction.Id);

        return MapToDto(investmentTransaction, asset.Name, asset.Ticker);
    }

    private static InvestmentTransactionResponseDto MapToDto(InvestmentTransaction transaction, string? assetName = null, string? assetTicker = null)
    {
        return new InvestmentTransactionResponseDto
        {
            Id = transaction.Id,
            UserId = transaction.UserId,
            AssetId = transaction.AssetId,
            AssetName = assetName ?? string.Empty,
            AssetTicker = assetTicker,
            AccountId = transaction.AccountId,
            TransactionType = transaction.TransactionType,
            Quantity = transaction.Quantity,
            Price = transaction.Price,
            TotalAmount = transaction.TotalAmount,
            Fees = transaction.Fees,
            Date = transaction.Date,
            Description = transaction.Description,
            LinkedTransactionId = transaction.LinkedTransactionId,
            CreatedAt = transaction.CreatedAt
        };
    }
}
