using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Validators;
using Xunit;

namespace MoneyManager.Tests.Application.Validators;

public class ConfirmInvoiceImportValidatorTests
{
    private readonly ConfirmInvoiceImportValidator _validator = new();

    private static ConfirmInvoiceImportItemRequestDto Item(string id = "a", int? installment = null, int? total = null) => new()
    {
        Id = id,
        Description = "Mercado",
        Amount = 10m,
        Date = new DateTime(2026, 10, 20),
        InstallmentNumber = installment,
        TotalInstallments = total
    };

    [Fact]
    public async Task Validate_WithItemsAndInstallments_ShouldPass()
    {
        // Arrange
        var request = new ConfirmInvoiceImportRequestDto { SelectedHolders = ["MARIA"], Items = [Item("a"), Item("b", 3, 10)] };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithoutItems_ShouldFail()
    {
        // Act
        var result = await _validator.ValidateAsync(new ConfirmInvoiceImportRequestDto());

        // Assert
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Selecione ao menos um lançamento para importar.");
    }

    [Fact]
    public async Task Validate_WithRepeatedItem_ShouldFail()
    {
        // Act
        var result = await _validator.ValidateAsync(new ConfirmInvoiceImportRequestDto { Items = [Item("a"), Item("a")] });

        // Assert
        Assert.Contains(result.Errors, e => e.ErrorMessage == "O mesmo lançamento foi enviado mais de uma vez.");
    }

    [Theory]
    [InlineData(3, null)]
    [InlineData(null, 3)]
    [InlineData(11, 10)]
    [InlineData(0, 10)]
    [InlineData(1, 49)]
    public async Task Validate_WithInvalidInstallments_ShouldFail(int? installment, int? total)
    {
        // Act
        var result = await _validator.ValidateAsync(new ConfirmInvoiceImportRequestDto { Items = [Item("a", installment, total)] });

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithTooLongDescription_ShouldFail()
    {
        // Arrange
        var item = Item();
        item.Description = new string('x', ConfirmInvoiceImportValidator.MaxDescriptionLength + 1);

        // Act
        var result = await _validator.ValidateAsync(new ConfirmInvoiceImportRequestDto { Items = [item] });

        // Assert
        Assert.False(result.IsValid);
    }
}
