using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Application.Services;

/// <summary>
/// Implementação do serviço de gerenciamento de ativos de investimento.
/// </summary>
public class InvestmentAssetService : IInvestmentAssetService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccountService _accountService;
    private readonly ILogger<InvestmentAssetService> _logger;

    public InvestmentAssetService(
        IUnitOfWork unitOfWork,
        IAccountService accountService,
        ILogger<InvestmentAssetService> logger)
    {
        _unitOfWork = unitOfWork;
        _accountService = accountService;
        _logger = logger;
    }

    public async Task<InvestmentAssetResponseDto> CreateAsync(string userId, CreateInvestmentAssetRequestDto request)
    {
        _logger.LogInformation("Creating investment asset for user {UserId}: {AssetName}", userId, request.Name);

        // Validar conta
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId);
        if (account == null || account.UserId != userId || account.IsDeleted)
        {
            throw new KeyNotFoundException("Account not found");
        }

        if (account.Type != AccountType.Investment)
        {
            _logger.LogWarning("Attempted to create investment asset in non-investment account {AccountId}", request.AccountId);
            throw new InvalidOperationException("A conta selecionada não é uma conta de investimento.");
        }

        // Criar ativo
        var asset = new InvestmentAsset
        {
            UserId = userId,
            AccountId = request.AccountId,
            AssetType = request.AssetType,
            Name = request.Name,
            Ticker = request.Ticker,
            Quantity = request.InitialQuantity,
            CurrentPrice = request.InitialPrice,
            Notes = request.Notes
        };

        // Calcular valores iniciais
        var totalCost = (request.InitialQuantity * request.InitialPrice) + request.InitialFees;
        asset.TotalInvested = totalCost;
        asset.AveragePurchasePrice = request.InitialQuantity > 0 ? totalCost / request.InitialQuantity : 0;
        
        asset.CalculateProfitLoss();
        asset.LastPriceUpdate = DateTime.UtcNow;

        await _unitOfWork.InvestmentAssets.AddAsync(asset);

        // Se houve investimento inicial, criar transação de compra e deduzir do saldo da conta
        if (request.InitialQuantity > 0 && request.InitialPrice > 0)
        {
            var investmentTransaction = new InvestmentTransaction
            {
                UserId = userId,
                AssetId = asset.Id,
                AccountId = request.AccountId,
                TransactionType = InvestmentTransactionType.Buy,
                Quantity = request.InitialQuantity,
                Price = request.InitialPrice,
                Fees = request.InitialFees,
                Date = DateTime.UtcNow,
                Description = $"Compra inicial de {request.Name}"
            };

            investmentTransaction.CalculateTotalAmount();
            await _unitOfWork.InvestmentTransactions.AddAsync(investmentTransaction);

            // Criar transação regular (débito na conta)
            var regularTransaction = new Transaction
            {
                UserId = userId,
                AccountId = request.AccountId,
                Type = TransactionType.InvestmentBuy,
                Amount = totalCost,
                Date = DateTime.UtcNow,
                Description = $"Compra de {request.Name}",
                Status = TransactionStatus.Completed
            };

            await _unitOfWork.Transactions.AddAsync(regularTransaction);
            investmentTransaction.LinkedTransactionId = regularTransaction.Id;

            // Atualizar saldo da conta
            await _accountService.UpdateBalanceAsync(userId, request.AccountId, -totalCost);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Investment asset {AssetId} created successfully for user {UserId}", asset.Id, userId);

        return MapToDto(asset);
    }

    public async Task<IEnumerable<InvestmentAssetResponseDto>> GetAllAsync(string userId)
    {
        var assets = await _unitOfWork.InvestmentAssets.GetActiveByUserIdAsync(userId);
        return assets.Select(MapToDto);
    }

    public async Task<InvestmentAssetResponseDto> GetByIdAsync(string userId, string assetId)
    {
        var asset = await _unitOfWork.InvestmentAssets.GetByIdAsync(assetId);
        
        if (asset == null || asset.UserId != userId || asset.IsDeleted)
        {
            throw new KeyNotFoundException("Asset not found");
        }

        return MapToDto(asset);
    }

    public async Task<InvestmentAssetResponseDto> UpdateAsync(string userId, string assetId, UpdateInvestmentAssetRequestDto request)
    {
        var asset = await _unitOfWork.InvestmentAssets.GetByIdAsync(assetId);
        
        if (asset == null || asset.UserId != userId || asset.IsDeleted)
        {
            throw new KeyNotFoundException("Asset not found");
        }

        asset.Name = request.Name;
        asset.Ticker = request.Ticker;
        asset.Notes = request.Notes;
        asset.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.InvestmentAssets.UpdateAsync(asset);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Investment asset {AssetId} updated for user {UserId}", assetId, userId);

        return MapToDto(asset);
    }

    public async Task DeleteAsync(string userId, string assetId)
    {
        var asset = await _unitOfWork.InvestmentAssets.GetByIdAsync(assetId);
        
        if (asset == null || asset.UserId != userId || asset.IsDeleted)
        {
            throw new KeyNotFoundException("Asset not found");
        }

        asset.IsDeleted = true;
        asset.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.InvestmentAssets.UpdateAsync(asset);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Investment asset {AssetId} deleted for user {UserId}", assetId, userId);
    }

    public async Task<InvestmentAssetResponseDto> BuyAsync(string userId, string assetId, BuyAssetRequestDto request)
    {
        _logger.LogInformation("Processing buy operation for asset {AssetId}, quantity: {Quantity}, price: {Price}", 
            assetId, request.Quantity, request.Price);

        var asset = await _unitOfWork.InvestmentAssets.GetByIdAsync(assetId);
        
        if (asset == null || asset.UserId != userId || asset.IsDeleted)
        {
            throw new KeyNotFoundException("Asset not found");
        }

        // Atualizar preço médio ponderado e quantidade
        asset.UpdateAveragePriceOnBuy(request.Quantity, request.Price, request.Fees);
        asset.CurrentPrice = request.Price; // Atualizar preço atual com preço da compra
        asset.CalculateProfitLoss();
        asset.LastPriceUpdate = DateTime.UtcNow;

        // Criar transação de investimento
        var investmentTransaction = new InvestmentTransaction
        {
            UserId = userId,
            AssetId = assetId,
            AccountId = asset.AccountId,
            TransactionType = InvestmentTransactionType.Buy,
            Quantity = request.Quantity,
            Price = request.Price,
            Fees = request.Fees,
            Date = request.Date,
            Description = request.Description ?? $"Compra de {asset.Name}"
        };

        investmentTransaction.CalculateTotalAmount();
        await _unitOfWork.InvestmentTransactions.AddAsync(investmentTransaction);

        // Criar transação regular (débito na conta)
        var totalAmount = investmentTransaction.TotalAmount;
        var regularTransaction = new Transaction
        {
            UserId = userId,
            AccountId = asset.AccountId,
            Type = TransactionType.InvestmentBuy,
            Amount = totalAmount,
            Date = request.Date,
            Description = investmentTransaction.Description,
            Status = TransactionStatus.Completed
        };

        await _unitOfWork.Transactions.AddAsync(regularTransaction);
        investmentTransaction.LinkedTransactionId = regularTransaction.Id;

        // Atualizar saldo da conta
        await _accountService.UpdateBalanceAsync(userId, asset.AccountId, -totalAmount);

        await _unitOfWork.InvestmentAssets.UpdateAsync(asset);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Buy operation completed for asset {AssetId}. New quantity: {Quantity}, New avg price: {AvgPrice}", 
            assetId, asset.Quantity, asset.AveragePurchasePrice);

        return MapToDto(asset);
    }

    public async Task<InvestmentAssetResponseDto> SellAsync(string userId, string assetId, SellAssetRequestDto request)
    {
        _logger.LogInformation("Processing sell operation for asset {AssetId}, quantity: {Quantity}, price: {Price}", 
            assetId, request.Quantity, request.Price);

        var asset = await _unitOfWork.InvestmentAssets.GetByIdAsync(assetId);
        
        if (asset == null || asset.UserId != userId || asset.IsDeleted)
        {
            throw new KeyNotFoundException("Asset not found");
        }

        // Validar quantidade disponível
        if (request.Quantity > asset.Quantity)
        {
            throw new InvalidOperationException($"Quantidade insuficiente. Disponível: {asset.Quantity}");
        }

        // Calcular lucro/prejuízo da venda
        var profitLoss = (request.Quantity * request.Price) - (request.Quantity * asset.AveragePurchasePrice) - request.Fees;

        // Atualizar quantidade (preço médio permanece o mesmo)
        asset.UpdateAveragePriceOnSell(request.Quantity);
        asset.CurrentPrice = request.Price; // Atualizar preço atual com preço da venda
        asset.CalculateProfitLoss();
        asset.LastPriceUpdate = DateTime.UtcNow;

        // Criar transação de investimento
        var investmentTransaction = new InvestmentTransaction
        {
            UserId = userId,
            AssetId = assetId,
            AccountId = asset.AccountId,
            TransactionType = InvestmentTransactionType.Sell,
            Quantity = request.Quantity,
            Price = request.Price,
            Fees = request.Fees,
            Date = request.Date,
            Description = request.Description ?? $"Venda de {asset.Name} - {(profitLoss >= 0 ? "Lucro" : "Prejuízo")}: R$ {Math.Abs(profitLoss):F2}"
        };

        investmentTransaction.CalculateTotalAmount();
        await _unitOfWork.InvestmentTransactions.AddAsync(investmentTransaction);

        // Criar transação regular (crédito na conta)
        var totalAmount = investmentTransaction.TotalAmount;
        var regularTransaction = new Transaction
        {
            UserId = userId,
            AccountId = asset.AccountId,
            Type = TransactionType.InvestmentSell,
            Amount = totalAmount,
            Date = request.Date,
            Description = investmentTransaction.Description,
            Status = TransactionStatus.Completed
        };

        await _unitOfWork.Transactions.AddAsync(regularTransaction);
        investmentTransaction.LinkedTransactionId = regularTransaction.Id;

        // Atualizar saldo da conta
        await _accountService.UpdateBalanceAsync(userId, asset.AccountId, totalAmount);

        await _unitOfWork.InvestmentAssets.UpdateAsync(asset);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Sell operation completed for asset {AssetId}. Remaining quantity: {Quantity}, Profit/Loss: {ProfitLoss}", 
            assetId, asset.Quantity, profitLoss);

        return MapToDto(asset);
    }

    public async Task<InvestmentAssetResponseDto> AdjustPriceAsync(string userId, string assetId, AdjustPriceRequestDto request)
    {
        var asset = await _unitOfWork.InvestmentAssets.GetByIdAsync(assetId);
        
        if (asset == null || asset.UserId != userId || asset.IsDeleted)
        {
            throw new KeyNotFoundException("Asset not found");
        }

        var oldPrice = asset.CurrentPrice;
        asset.UpdateMarketPrice(request.NewPrice);

        // Criar transação de ajuste de mercado
        var investmentTransaction = new InvestmentTransaction
        {
            UserId = userId,
            AssetId = assetId,
            AccountId = asset.AccountId,
            TransactionType = InvestmentTransactionType.MarketAdjustment,
            Quantity = asset.Quantity,
            Price = request.NewPrice,
            Fees = 0,
            Date = request.Date,
            Description = $"Ajuste de preço de {asset.Name}: R$ {oldPrice:F2} ? R$ {request.NewPrice:F2}"
        };

        investmentTransaction.CalculateTotalAmount();
        await _unitOfWork.InvestmentTransactions.AddAsync(investmentTransaction);

        await _unitOfWork.InvestmentAssets.UpdateAsync(asset);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Price adjusted for asset {AssetId}: {OldPrice} ? {NewPrice}", 
            assetId, oldPrice, request.NewPrice);

        return MapToDto(asset);
    }

    public async Task<InvestmentSummaryResponseDto> GetSummaryAsync(string userId)
    {
        var assets = await _unitOfWork.InvestmentAssets.GetActiveByUserIdAsync(userId);
        var assetsList = assets.ToList();

        if (!assetsList.Any())
        {
            return new InvestmentSummaryResponseDto();
        }

        var summary = new InvestmentSummaryResponseDto
        {
            TotalInvested = assetsList.Sum(a => a.TotalInvested),
            CurrentValue = assetsList.Sum(a => a.CurrentValue),
            TotalProfitLoss = assetsList.Sum(a => a.ProfitLoss),
            TotalAssets = assetsList.Count
        };

        if (summary.TotalInvested > 0)
        {
            summary.TotalProfitLossPercentage = (summary.TotalProfitLoss / summary.TotalInvested) * 100;
        }

        // Agrupar por tipo
        summary.AssetsByType = assetsList
            .GroupBy(a => a.AssetType)
            .Select(g => new AssetsByTypeDto
            {
                AssetType = g.Key,
                Count = g.Count(),
                TotalInvested = g.Sum(a => a.TotalInvested),
                CurrentValue = g.Sum(a => a.CurrentValue),
                ProfitLoss = g.Sum(a => a.ProfitLoss),
                ProfitLossPercentage = g.Sum(a => a.TotalInvested) > 0 
                    ? (g.Sum(a => a.ProfitLoss) / g.Sum(a => a.TotalInvested)) * 100 
                    : 0,
                PortfolioPercentage = summary.CurrentValue > 0 
                    ? (g.Sum(a => a.CurrentValue) / summary.CurrentValue) * 100 
                    : 0
            })
            .OrderByDescending(a => a.CurrentValue)
            .ToList();

        // Top 5 melhores performers
        summary.TopPerformers = assetsList
            .Where(a => a.TotalInvested > 0)
            .OrderByDescending(a => a.ProfitLossPercentage)
            .Take(5)
            .Select(a => new AssetPerformanceDto
            {
                AssetId = a.Id,
                AssetName = a.Name,
                AssetTicker = a.Ticker,
                AssetType = a.AssetType,
                TotalInvested = a.TotalInvested,
                CurrentValue = a.CurrentValue,
                ProfitLoss = a.ProfitLoss,
                ProfitLossPercentage = a.ProfitLossPercentage
            })
            .ToList();

        // Top 5 piores performers
        summary.WorstPerformers = assetsList
            .Where(a => a.TotalInvested > 0)
            .OrderBy(a => a.ProfitLossPercentage)
            .Take(5)
            .Select(a => new AssetPerformanceDto
            {
                AssetId = a.Id,
                AssetName = a.Name,
                AssetTicker = a.Ticker,
                AssetType = a.AssetType,
                TotalInvested = a.TotalInvested,
                CurrentValue = a.CurrentValue,
                ProfitLoss = a.ProfitLoss,
                ProfitLossPercentage = a.ProfitLossPercentage
            })
            .ToList();

        // Calcular rendimentos totais (necessita das transações)
        var allTransactions = await _unitOfWork.InvestmentTransactions.GetByUserIdAsync(userId);
        var yieldTransactions = allTransactions.Where(t => 
            t.TransactionType == InvestmentTransactionType.Dividend ||
            t.TransactionType == InvestmentTransactionType.Interest ||
            t.TransactionType == InvestmentTransactionType.YieldPayment);
        
        summary.TotalYields = yieldTransactions.Sum(t => t.TotalAmount);

        return summary;
    }

    private static InvestmentAssetResponseDto MapToDto(InvestmentAsset asset)
    {
        return new InvestmentAssetResponseDto
        {
            Id = asset.Id,
            UserId = asset.UserId,
            AccountId = asset.AccountId,
            AssetType = asset.AssetType,
            Name = asset.Name,
            Ticker = asset.Ticker,
            Quantity = asset.Quantity,
            AveragePurchasePrice = asset.AveragePurchasePrice,
            CurrentPrice = asset.CurrentPrice,
            TotalInvested = asset.TotalInvested,
            CurrentValue = asset.CurrentValue,
            ProfitLoss = asset.ProfitLoss,
            ProfitLossPercentage = asset.ProfitLossPercentage,
            LastPriceUpdate = asset.LastPriceUpdate,
            Notes = asset.Notes,
            CreatedAt = asset.CreatedAt,
            UpdatedAt = asset.UpdatedAt
        };
    }
}
