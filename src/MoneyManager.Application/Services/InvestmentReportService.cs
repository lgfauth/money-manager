using Microsoft.Extensions.Logging;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

/// <summary>
/// Implementação do serviço de relatórios de investimentos.
/// </summary>
public class InvestmentReportService : IInvestmentReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InvestmentReportService> _logger;

    public InvestmentReportService(
        IUnitOfWork unitOfWork,
        ILogger<InvestmentReportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<InvestmentSalesReportDto> GenerateSalesReportAsync(string userId, int year)
    {
        _logger.LogInformation("Gerando relatório de vendas para usuário {UserId}, ano {Year}", userId, year);

        var startDate = new DateTime(year, 1, 1);
        var endDate = new DateTime(year, 12, 31, 23, 59, 59);

        // Buscar todas as transações de venda do ano
        var allTransactions = await _unitOfWork.InvestmentTransactions
            .GetByUserIdAsync(userId, startDate, endDate);

        var salesTransactions = allTransactions
            .Where(t => t.TransactionType == InvestmentTransactionType.Sell)
            .OrderBy(t => t.Date)
            .ToList();

        // Buscar informações dos ativos
        var assetIds = salesTransactions.Select(t => t.AssetId).Distinct().ToList();
        var assets = new Dictionary<string, Domain.Entities.InvestmentAsset>();
        
        foreach (var assetId in assetIds)
        {
            var asset = await _unitOfWork.InvestmentAssets.GetByIdAsync(assetId);
            if (asset != null)
            {
                assets[assetId] = asset;
            }
        }

        // Montar itens de venda
        var saleItems = new List<SaleItemDto>();
        decimal totalProfit = 0;
        decimal totalLoss = 0;

        foreach (var transaction in salesTransactions)
        {
            if (!assets.TryGetValue(transaction.AssetId, out var asset))
                continue;

            var totalSaleValue = transaction.Quantity * transaction.Price;
            var totalCost = transaction.Quantity * asset.AveragePurchasePrice;
            var profitLoss = totalSaleValue - totalCost - transaction.Fees;
            var profitLossPercentage = totalCost > 0 
                ? (profitLoss / totalCost) * 100 
                : 0;

            if (profitLoss > 0)
                totalProfit += profitLoss;
            else
                totalLoss += Math.Abs(profitLoss);

            saleItems.Add(new SaleItemDto
            {
                Date = transaction.Date,
                AssetName = asset.Name,
                Ticker = asset.Ticker,
                AssetType = asset.AssetType,
                Quantity = transaction.Quantity,
                AveragePurchasePrice = asset.AveragePurchasePrice,
                SalePrice = transaction.Price,
                TotalSaleValue = totalSaleValue,
                TotalCost = totalCost,
                ProfitLoss = profitLoss,
                ProfitLossPercentage = profitLossPercentage,
                Fees = transaction.Fees
            });
        }

        var totalSold = saleItems.Sum(s => s.TotalSaleValue);
        var netResult = totalProfit - totalLoss;
        
        // IR: 15% sobre lucro (ações), simplificado
        var estimatedTax = totalProfit * 0.15m;

        // Buscar nome do usuário
        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        var report = new InvestmentSalesReportDto
        {
            Year = year,
            UserId = userId,
            UserName = user?.Name ?? "Usuário",
            Sales = saleItems,
            TotalSold = totalSold,
            TotalProfit = totalProfit,
            TotalLoss = totalLoss,
            NetResult = netResult,
            EstimatedTaxDue = estimatedTax,
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Relatório de vendas gerado: {Count} vendas, Total: {Total:C}, Lucro: {Profit:C}",
            saleItems.Count, totalSold, totalProfit);

        return report;
    }

    public async Task<InvestmentYieldsReportDto> GenerateYieldsReportAsync(string userId, int year)
    {
        _logger.LogInformation("Gerando relatório de rendimentos para usuário {UserId}, ano {Year}", userId, year);

        var startDate = new DateTime(year, 1, 1);
        var endDate = new DateTime(year, 12, 31, 23, 59, 59);

        // Buscar todas as transações de rendimento do ano
        var allTransactions = await _unitOfWork.InvestmentTransactions
            .GetByUserIdAsync(userId, startDate, endDate);

        var yieldTransactions = allTransactions
            .Where(t => t.TransactionType == InvestmentTransactionType.Dividend ||
                       t.TransactionType == InvestmentTransactionType.Interest ||
                       t.TransactionType == InvestmentTransactionType.YieldPayment)
            .OrderBy(t => t.Date)
            .ToList();

        // Buscar informações dos ativos
        var assetIds = yieldTransactions.Select(t => t.AssetId).Distinct().ToList();
        var assets = new Dictionary<string, Domain.Entities.InvestmentAsset>();
        
        foreach (var assetId in assetIds)
        {
            var asset = await _unitOfWork.InvestmentAssets.GetByIdAsync(assetId);
            if (asset != null)
            {
                assets[assetId] = asset;
            }
        }

        // Montar itens de rendimento
        var yieldItems = new List<YieldItemDto>();
        decimal totalDividends = 0;
        decimal totalInterest = 0;
        decimal totalRealEstateYields = 0;

        foreach (var transaction in yieldTransactions)
        {
            if (!assets.TryGetValue(transaction.AssetId, out var asset))
                continue;

            yieldItems.Add(new YieldItemDto
            {
                Date = transaction.Date,
                AssetName = asset.Name,
                Ticker = asset.Ticker,
                AssetType = asset.AssetType,
                YieldType = transaction.TransactionType,
                Amount = transaction.TotalAmount,
                Description = transaction.Description
            });

            // Somar por tipo
            switch (transaction.TransactionType)
            {
                case InvestmentTransactionType.Dividend:
                    totalDividends += transaction.TotalAmount;
                    break;
                case InvestmentTransactionType.Interest:
                    totalInterest += transaction.TotalAmount;
                    break;
                case InvestmentTransactionType.YieldPayment:
                    totalRealEstateYields += transaction.TotalAmount;
                    break;
            }
        }

        var totalYields = yieldItems.Sum(y => y.Amount);

        // Buscar nome do usuário
        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        var report = new InvestmentYieldsReportDto
        {
            Year = year,
            UserId = userId,
            UserName = user?.Name ?? "Usuário",
            Yields = yieldItems,
            TotalYields = totalYields,
            TotalDividends = totalDividends,
            TotalInterest = totalInterest,
            TotalRealEstateYields = totalRealEstateYields,
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Relatório de rendimentos gerado: {Count} rendimentos, Total: {Total:C}",
            yieldItems.Count, totalYields);

        return report;
    }

    public async Task<InvestmentConsolidatedStatementDto> GenerateConsolidatedStatementAsync(
        string userId, 
        DateTime startDate, 
        DateTime endDate)
    {
        _logger.LogInformation(
            "Gerando extrato consolidado para usuário {UserId}, período {Start} a {End}",
            userId, startDate, endDate);

        // Buscar todas as transações do período
        var allTransactions = await _unitOfWork.InvestmentTransactions
            .GetByUserIdAsync(userId, startDate, endDate);

        var transactions = allTransactions.OrderBy(t => t.Date).ToList();

        // Buscar informações dos ativos
        var assetIds = transactions.Select(t => t.AssetId).Distinct().ToList();
        var assets = new Dictionary<string, Domain.Entities.InvestmentAsset>();
        
        foreach (var assetId in assetIds)
        {
            var asset = await _unitOfWork.InvestmentAssets.GetByIdAsync(assetId);
            if (asset != null)
            {
                assets[assetId] = asset;
            }
        }

        // Montar transações consolidadas
        var consolidatedTransactions = new List<ConsolidatedTransactionDto>();
        decimal totalPurchases = 0;
        decimal totalSales = 0;
        decimal totalYields = 0;
        decimal totalFees = 0;

        foreach (var transaction in transactions)
        {
            if (!assets.TryGetValue(transaction.AssetId, out var asset))
                continue;

            consolidatedTransactions.Add(new ConsolidatedTransactionDto
            {
                Date = transaction.Date,
                AssetName = asset.Name,
                TransactionType = transaction.TransactionType,
                Quantity = transaction.Quantity > 0 ? transaction.Quantity : null,
                Price = transaction.Price > 0 ? transaction.Price : null,
                TotalAmount = transaction.TotalAmount,
                Fees = transaction.Fees,
                Description = transaction.Description
            });

            // Somar por tipo
            switch (transaction.TransactionType)
            {
                case InvestmentTransactionType.Buy:
                    totalPurchases += transaction.TotalAmount;
                    totalFees += transaction.Fees;
                    break;
                case InvestmentTransactionType.Sell:
                    totalSales += transaction.TotalAmount;
                    totalFees += transaction.Fees;
                    break;
                case InvestmentTransactionType.Dividend:
                case InvestmentTransactionType.Interest:
                case InvestmentTransactionType.YieldPayment:
                    totalYields += transaction.TotalAmount;
                    break;
                case InvestmentTransactionType.Fee:
                    totalFees += transaction.TotalAmount;
                    break;
            }
        }

        var netResult = (totalSales + totalYields) - (totalPurchases + totalFees);

        // Buscar nome do usuário
        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        var statement = new InvestmentConsolidatedStatementDto
        {
            StartDate = startDate,
            EndDate = endDate,
            UserId = userId,
            UserName = user?.Name ?? "Usuário",
            Transactions = consolidatedTransactions,
            TotalPurchases = totalPurchases,
            TotalSales = totalSales,
            TotalYields = totalYields,
            TotalFees = totalFees,
            NetResult = netResult,
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Extrato consolidado gerado: {Count} transações, Resultado: {Result:C}",
            consolidatedTransactions.Count, netResult);

        return statement;
    }

    public Task<byte[]> ExportSalesReportToPdfAsync(InvestmentSalesReportDto report)
    {
        // TODO: Implementar após adicionar pacote QuestPDF
        _logger.LogWarning("Exportação para PDF ainda não implementada");
        throw new NotImplementedException("Exportação para PDF será implementada com QuestPDF");
    }

    public Task<byte[]> ExportSalesReportToExcelAsync(InvestmentSalesReportDto report)
    {
        // TODO: Implementar após adicionar pacote ClosedXML
        _logger.LogWarning("Exportação para Excel ainda não implementada");
        throw new NotImplementedException("Exportação para Excel será implementada com ClosedXML");
    }

    public Task<byte[]> ExportYieldsReportToExcelAsync(InvestmentYieldsReportDto report)
    {
        // TODO: Implementar após adicionar pacote ClosedXML
        _logger.LogWarning("Exportação para Excel ainda não implementada");
        throw new NotImplementedException("Exportação para Excel será implementada com ClosedXML");
    }
}
