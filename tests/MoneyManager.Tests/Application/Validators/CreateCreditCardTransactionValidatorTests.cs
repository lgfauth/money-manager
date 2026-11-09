using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Validators;
using Xunit;

namespace MoneyManager.Tests.Application.Validators;

public class CreateCreditCardTransactionValidatorTests
{
    private readonly CreateCreditCardTransactionValidator _validator = new();

    private static CreateCreditCardTransactionRequestDto Valid() => new()
    {
        CreditCardId = "card-1",
        Description = "Mercado",
        TotalAmount = 50m,
        PurchaseDate = DateTime.UtcNow.Date
    };

    [Fact]
    public async Task Validate_ManualPurchase_ShouldPass()
    {
        // Act & Assert
        Assert.True((await _validator.ValidateAsync(Valid())).IsValid);
    }

    [Fact]
    public async Task Validate_WithInvoiceImportSource_ShouldFail()
    {
        // Arrange
        var request = Valid();
        request.Source = "invoice_import";

        // Act & Assert
        Assert.False((await _validator.ValidateAsync(request)).IsValid);
    }

    [Fact]
    public async Task Validate_WithInvoiceImportFields_ShouldFail()
    {
        // Arrange: cliente tentando lançar em fatura fechada pela API de cadastro manual
        var request = Valid();
        request.TargetReferenceMonth = "2026-01";

        // Act & Assert
        Assert.False((await _validator.ValidateAsync(request)).IsValid);
    }
}
