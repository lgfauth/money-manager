using Xunit;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Tests.Calculations;

/// <summary>
/// Testes específicos para cálculos financeiros de investimentos.
/// Cobre casos extremos e edge cases.
/// </summary>
public class InvestmentCalculationsTests
{
    #region Testes de Preço Médio Ponderado

    [Theory]
    [InlineData(100, 10.00, 50, 12.00, 0, 10.67)] // Compra simples
    [InlineData(100, 20.00, 100, 20.00, 0, 20.00)] // Mesmo preço
    [InlineData(50, 30.00, 50, 10.00, 0, 20.00)] // Preços diferentes
    [InlineData(100, 25.00, 25, 20.00, 0, 24.00)] // Compra menor quantidade
    public void CalculateWeightedAverage_DifferentScenarios_ShouldReturnCorrectValue(
        decimal initialQty, 
        decimal initialPrice, 
        decimal buyQty, 
        decimal buyPrice, 
        decimal fees,
        decimal expectedAverage)
    {
        // Arrange
        var initialInvested = initialQty * initialPrice;
        var buyInvested = buyQty * buyPrice + fees;
        var totalQty = initialQty + buyQty;

        // Act
        var averagePrice = (initialInvested + buyInvested) / totalQty;

        // Assert
        Assert.Equal(expectedAverage, Math.Round(averagePrice, 2));
    }

    [Fact]
    public void CalculateWeightedAverage_WithHighFees_ShouldIncreaseAverage()
    {
        // Arrange
        var initialQty = 100m;
        var initialPrice = 10.00m;
        var buyQty = 50m;
        var buyPrice = 10.00m;
        var fees = 100.00m; // Taxa alta (10% do valor)

        var initialInvested = initialQty * initialPrice; // 1000
        var buyInvested = buyQty * buyPrice + fees; // 600
        var totalQty = initialQty + buyQty; // 150

        // Act
        var averagePrice = (initialInvested + buyInvested) / totalQty;

        // Assert
        Assert.Equal(10.67m, Math.Round(averagePrice, 2)); // (1000 + 600) / 150
    }

    [Fact]
    public void CalculateWeightedAverage_MultipleOperations_ShouldAccumulateCorrectly()
    {
        // Arrange
        var asset = new InvestmentAsset
        {
            Quantity = 0,
            AveragePurchasePrice = 0,
            TotalInvested = 0
        };

        // Act - Compra 1: 100 @ R$ 10,00
        asset.UpdateAveragePriceOnBuy(100, 10.00m, 0);
        Assert.Equal(100, asset.Quantity);
        Assert.Equal(10.00m, asset.AveragePurchasePrice);
        Assert.Equal(1000.00m, asset.TotalInvested);

        // Compra 2: 50 @ R$ 12,00 + R$ 5 taxa
        asset.UpdateAveragePriceOnBuy(50, 12.00m, 5.00m);
        Assert.Equal(150, asset.Quantity);
        Assert.Equal(10.70m, Math.Round(asset.AveragePurchasePrice, 2));
        Assert.Equal(1605.00m, asset.TotalInvested);

        // Compra 3: 25 @ R$ 8,00
        asset.UpdateAveragePriceOnBuy(25, 8.00m, 0);
        Assert.Equal(175, asset.Quantity);
        Assert.Equal(10.31m, Math.Round(asset.AveragePurchasePrice, 2)); // (1605 + 200) / 175 = 10.314...
        Assert.Equal(1805.00m, asset.TotalInvested);
    }

    [Fact]
    public void UpdateAveragePriceOnSell_ShouldNotChangeAveragePrice()
    {
        // Arrange
        var asset = new InvestmentAsset
        {
            Quantity = 100,
            AveragePurchasePrice = 20.00m,
            TotalInvested = 2000.00m
        };

        // Act - Venda de 50 unidades
        asset.UpdateAveragePriceOnSell(50);

        // Assert
        Assert.Equal(50, asset.Quantity);
        Assert.Equal(20.00m, asset.AveragePurchasePrice); // Preço médio não muda
        Assert.Equal(1000.00m, asset.TotalInvested); // Investimento reduz proporcionalmente
    }

    #endregion

    #region Testes de Lucro/Prejuízo

    [Theory]
    [InlineData(100, 20.00, 25.00, 500.00, 25.00)] // Lucro de 25%
    [InlineData(100, 30.00, 20.00, -1000.00, -33.33)] // Prejuízo de 33.33%
    [InlineData(100, 25.00, 25.00, 0.00, 0.00)] // Break-even
    [InlineData(50, 40.00, 50.00, 500.00, 25.00)] // Lucro de 25%
    public void CalculateProfitLoss_DifferentScenarios_ShouldReturnCorrectValues(
        decimal quantity,
        decimal avgPrice,
        decimal currentPrice,
        decimal expectedProfit,
        decimal expectedPercentage)
    {
        // Arrange
        var asset = new InvestmentAsset
        {
            Quantity = quantity,
            AveragePurchasePrice = avgPrice,
            CurrentPrice = currentPrice,
            TotalInvested = quantity * avgPrice
        };

        // Act
        asset.CalculateCurrentValue();
        asset.CalculateProfitLoss();

        // Assert
        Assert.Equal(expectedProfit, asset.ProfitLoss);
        Assert.Equal(expectedPercentage, Math.Round(asset.ProfitLossPercentage, 2));
    }

    [Fact]
    public void CalculateProfitLoss_ZeroQuantity_ShouldReturnZero()
    {
        // Arrange
        var asset = new InvestmentAsset
        {
            Quantity = 0,
            AveragePurchasePrice = 20.00m,
            CurrentPrice = 25.00m,
            TotalInvested = 0
        };

        // Act
        asset.CalculateCurrentValue();
        asset.CalculateProfitLoss();

        // Assert
        Assert.Equal(0, asset.CurrentValue);
        Assert.Equal(0, asset.ProfitLoss);
        Assert.Equal(0, asset.ProfitLossPercentage);
    }

    [Fact]
    public void CalculateProfitLoss_PartialSale_ShouldMaintainProportions()
    {
        // Arrange
        var asset = new InvestmentAsset
        {
            Quantity = 100,
            AveragePurchasePrice = 20.00m,
            CurrentPrice = 30.00m,
            TotalInvested = 2000.00m
        };

        asset.CalculateCurrentValue();
        asset.CalculateProfitLoss();

        var initialProfit = asset.ProfitLoss;
        var initialPercentage = asset.ProfitLossPercentage;

        // Act - Venda de 50%
        asset.UpdateAveragePriceOnSell(50);
        asset.CalculateCurrentValue();
        asset.CalculateProfitLoss();

        // Assert
        Assert.Equal(50, asset.Quantity);
        Assert.Equal(initialProfit / 2, asset.ProfitLoss); // Lucro cai pela metade
        Assert.Equal(initialPercentage, asset.ProfitLossPercentage); // Percentual mantém
    }

    #endregion

    #region Testes de Valor Atual

    [Theory]
    [InlineData(100, 25.50, 2550.00)]
    [InlineData(50, 110.75, 5537.50)]
    [InlineData(1, 10000.00, 10000.00)]
    [InlineData(0, 25.00, 0.00)]
    [InlineData(150.5, 32.10, 4831.05)]
    public void CalculateCurrentValue_DifferentQuantities_ShouldMultiplyCorrectly(
        decimal quantity,
        decimal price,
        decimal expectedValue)
    {
        // Arrange
        var asset = new InvestmentAsset
        {
            Quantity = quantity,
            CurrentPrice = price
        };

        // Act
        asset.CalculateCurrentValue();

        // Assert
        Assert.Equal(expectedValue, asset.CurrentValue);
    }

    [Fact]
    public void CalculateCurrentValue_AfterPriceUpdate_ShouldReflectNewPrice()
    {
        // Arrange
        var asset = new InvestmentAsset
        {
            Quantity = 100,
            CurrentPrice = 20.00m
        };

        asset.CalculateCurrentValue();
        Assert.Equal(2000.00m, asset.CurrentValue);

        // Act - Atualizar preço
        asset.CurrentPrice = 25.00m;
        asset.CalculateCurrentValue();

        // Assert
        Assert.Equal(2500.00m, asset.CurrentValue);
    }

    #endregion

    #region Testes de Casos Extremos

    [Fact]
    public void CalculateWeightedAverage_VeryLargeQuantities_ShouldHandleCorrectly()
    {
        // Arrange
        var asset = new InvestmentAsset
        {
            Quantity = 1000000,
            AveragePurchasePrice = 0.50m,
            TotalInvested = 500000.00m
        };

        // Act
        asset.UpdateAveragePriceOnBuy(500000, 0.75m, 100.00m);

        // Assert
        Assert.Equal(1500000, asset.Quantity);
        Assert.Equal(0.58m, Math.Round(asset.AveragePurchasePrice, 2));
        Assert.Equal(875100.00m, asset.TotalInvested);
    }

    [Fact]
    public void CalculateProfitLoss_VerySmallPriceChange_ShouldBeAccurate()
    {
        // Arrange
        var asset = new InvestmentAsset
        {
            Quantity = 100,
            AveragePurchasePrice = 25.00m,
            CurrentPrice = 25.01m,
            TotalInvested = 2500.00m
        };

        // Act
        asset.CalculateCurrentValue();
        asset.CalculateProfitLoss();

        // Assert
        Assert.Equal(2501.00m, asset.CurrentValue);
        Assert.Equal(1.00m, asset.ProfitLoss);
        Assert.Equal(0.04m, Math.Round(asset.ProfitLossPercentage, 2));
    }

    [Fact]
    public void CalculateProfitLoss_VeryHighGain_ShouldCalculateCorrectly()
    {
        // Arrange - Comprou a R$ 1, vale R$ 100 (10.000% de lucro)
        var asset = new InvestmentAsset
        {
            Quantity = 1000,
            AveragePurchasePrice = 1.00m,
            CurrentPrice = 100.00m,
            TotalInvested = 1000.00m
        };

        // Act
        asset.CalculateCurrentValue();
        asset.CalculateProfitLoss();

        // Assert
        Assert.Equal(100000.00m, asset.CurrentValue);
        Assert.Equal(99000.00m, asset.ProfitLoss);
        Assert.Equal(9900.00m, asset.ProfitLossPercentage); // 9900%
    }

    [Fact]
    public void CalculateProfitLoss_NearTotalLoss_ShouldCalculateCorrectly()
    {
        // Arrange - Comprou a R$ 100, vale R$ 1 (99% de prejuízo)
        var asset = new InvestmentAsset
        {
            Quantity = 100,
            AveragePurchasePrice = 100.00m,
            CurrentPrice = 1.00m,
            TotalInvested = 10000.00m
        };

        // Act
        asset.CalculateCurrentValue();
        asset.CalculateProfitLoss();

        // Assert
        Assert.Equal(100.00m, asset.CurrentValue);
        Assert.Equal(-9900.00m, asset.ProfitLoss);
        Assert.Equal(-99.00m, asset.ProfitLossPercentage);
    }

    [Fact]
    public void CalculateWeightedAverage_FractionalShares_ShouldHandleDecimals()
    {
        // Arrange - Ações fracionárias (BDRs, por exemplo)
        var asset = new InvestmentAsset
        {
            Quantity = 10.5m,
            AveragePurchasePrice = 25.50m,
            TotalInvested = 267.75m
        };

        // Act
        asset.UpdateAveragePriceOnBuy(5.25m, 30.00m, 2.50m);

        // Assert
        Assert.Equal(15.75m, asset.Quantity);
        Assert.Equal(27.16m, Math.Round(asset.AveragePurchasePrice, 2)); // (267.75 + 160.00) / 15.75 = 27.158...
        Assert.Equal(427.75m, Math.Round(asset.TotalInvested, 2)); // 267.75 + 157.50 + 2.50 = 427.75
    }

    #endregion

    #region Testes de Consistência

    [Fact]
    public void BuyThenSellAll_ShouldReturnToZero()
    {
        // Arrange
        var asset = new InvestmentAsset
        {
            Quantity = 0,
            AveragePurchasePrice = 0,
            TotalInvested = 0
        };

        // Act - Comprar
        asset.UpdateAveragePriceOnBuy(100, 25.00m, 10.00m);
        Assert.Equal(100, asset.Quantity);
        Assert.Equal(2510.00m, asset.TotalInvested);

        // Vender tudo
        asset.UpdateAveragePriceOnSell(100);

        // Assert
        Assert.Equal(0, asset.Quantity);
        Assert.Equal(0, asset.TotalInvested);
    }

    [Fact]
    public void MultipleBuysAndSells_ShouldMaintainConsistency()
    {
        // Arrange
        var asset = new InvestmentAsset
        {
            Quantity = 0,
            AveragePurchasePrice = 0,
            TotalInvested = 0
        };

        // Act
        // Compra 1: 100 @ R$ 20
        asset.UpdateAveragePriceOnBuy(100, 20.00m, 0);
        Assert.Equal(100, asset.Quantity);
        Assert.Equal(20.00m, asset.AveragePurchasePrice);

        // Compra 2: 50 @ R$ 25
        asset.UpdateAveragePriceOnBuy(50, 25.00m, 0);
        Assert.Equal(150, asset.Quantity);
        Assert.Equal(21.67m, Math.Round(asset.AveragePurchasePrice, 2));

        // Venda: 75
        asset.UpdateAveragePriceOnSell(75);
        Assert.Equal(75, asset.Quantity);
        Assert.Equal(21.67m, Math.Round(asset.AveragePurchasePrice, 2));

        // Compra 3: 25 @ R$ 30
        asset.UpdateAveragePriceOnBuy(25, 30.00m, 0);
        Assert.Equal(100, asset.Quantity);
        Assert.Equal(23.75m, Math.Round(asset.AveragePurchasePrice, 2));
    }

    #endregion
}
