using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;
using NSubstitute;
using Xunit;

namespace MoneyManager.Tests.Application.Services;

public class CreditCardServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICreditCardInvoiceService _invoiceService;
    private readonly IProcessLogger _logger;
    private readonly ICreditCardRepository _cardRepo;
    private readonly ICreditCardInvoiceRepository _invoiceRepo;
    private readonly CreditCardService _service;

    private const string UserId = "user-abc";

    public CreditCardServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _invoiceService = Substitute.For<ICreditCardInvoiceService>();
        _logger = Substitute.For<IProcessLogger>();
        _cardRepo = Substitute.For<ICreditCardRepository>();
        _invoiceRepo = Substitute.For<ICreditCardInvoiceRepository>();

        _unitOfWork.CreditCards.Returns(_cardRepo);
        _unitOfWork.CreditCardInvoices.Returns(_invoiceRepo);

        _service = new CreditCardService(_unitOfWork, _invoiceService, _logger);
    }

    private static CreditCard BuildCard(string userId = UserId) => new()
    {
        Id = "card-1",
        UserId = userId,
        Name = "Nubank",
        Limit = 5000m,
        ClosingDay = 5,
        BillingDueDay = 12,
        BestPurchaseDay = 6,
        Color = "#820AD1",
        Currency = "BRL"
    };

    private static CreditCardInvoice BuildOpenInvoice(string cardId) => new()
    {
        Id = "inv-1",
        CreditCardId = cardId,
        UserId = UserId,
        Status = InvoiceStatus.Open,
        ReferenceMonth = "2026-06",
        TotalAmount = 1000m
    };

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateCardAndEnsureInvoice()
    {
        // Arrange
        var request = new CreateCreditCardRequestDto
        {
            Name = "Nubank",
            Limit = 5000m,
            ClosingDay = 5,
            BillingDueDay = 12,
            Color = "#820AD1",
            Currency = "BRL"
        };

        var fakeCard = BuildCard();
        var fakeInvoice = BuildOpenInvoice(fakeCard.Id);

        _cardRepo.AddAsync(Arg.Any<CreditCard>()).Returns(x => x.Arg<CreditCard>());
        _invoiceService.EnsureCurrentOpenInvoiceAsync(UserId, Arg.Any<CreditCard>()).Returns(fakeInvoice);
        _invoiceRepo.GetByCardAsync(UserId, Arg.Any<string>()).Returns([fakeInvoice]);

        // Act
        var result = await _service.CreateAsync(UserId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Nubank", result.Name);
        Assert.Equal(5000m, result.Limit);
        await _cardRepo.Received(1).AddAsync(Arg.Any<CreditCard>());
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnCard()
    {
        // Arrange
        var card = BuildCard();
        var invoice = BuildOpenInvoice(card.Id);

        _cardRepo.GetByIdAsync(card.Id).Returns(card);
        _invoiceService.EnsureCurrentOpenInvoiceAsync(UserId, card).Returns(invoice);
        _invoiceRepo.GetByCardAsync(UserId, card.Id).Returns([invoice]);

        // Act
        var result = await _service.GetByIdAsync(UserId, card.Id);

        // Assert
        Assert.Equal(card.Id, result.Id);
        Assert.Equal("Nubank", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCardNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _cardRepo.GetByIdAsync("missing-id").Returns((CreditCard?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetByIdAsync(UserId, "missing-id"));
    }

    [Fact]
    public async Task GetByIdAsync_WhenCardBelongsToOtherUser_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var card = BuildCard(userId: "other-user");
        _cardRepo.GetByIdAsync(card.Id).Returns(card);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetByIdAsync(UserId, card.Id));
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldUpdateCard()
    {
        // Arrange
        var card = BuildCard();
        var invoice = BuildOpenInvoice(card.Id);

        _cardRepo.GetByIdAsync(card.Id).Returns(card);
        _invoiceRepo.GetByCardAsync(UserId, card.Id).Returns([invoice]);

        var request = new CreateCreditCardRequestDto
        {
            Name = "Nubank Black",
            Limit = 10000m,
            ClosingDay = 10,
            BillingDueDay = 17,
            Color = "#000000",
            Currency = "BRL"
        };

        // Act
        var result = await _service.UpdateAsync(UserId, card.Id, request);

        // Assert
        Assert.Equal("Nubank Black", result.Name);
        Assert.Equal(10000m, result.Limit);
        await _cardRepo.Received(1).UpdateAsync(card);
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateAsync_WhenCardNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _cardRepo.GetByIdAsync("missing-id").Returns((CreditCard?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateAsync(UserId, "missing-id", new CreateCreditCardRequestDto()));
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldSoftDeleteCard()
    {
        // Arrange
        var card = BuildCard();
        _cardRepo.GetByIdAsync(card.Id).Returns(card);

        // Act
        await _service.DeleteAsync(UserId, card.Id);

        // Assert
        Assert.True(card.IsDeleted);
        await _cardRepo.Received(1).UpdateAsync(card);
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteAsync_WhenCardNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _cardRepo.GetByIdAsync("missing-id").Returns((CreditCard?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.DeleteAsync(UserId, "missing-id"));
    }
}
