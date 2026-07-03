using NSubstitute;
using Xunit;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Tests.Application.Services;

public class CreditCardTransactionServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly ICreditCardRepository _cardRepo;
    private readonly ICreditCardInvoiceRepository _invoiceRepo;
    private readonly ICreditCardTransactionRepository _ccTransactionRepo;
    private readonly IRepository<Category> _categoryRepo;
    private readonly ICreditCardInvoiceService _invoiceServiceMock;
    private readonly CreditCardTransactionService _service;

    private const string UserId = "user1";

    public CreditCardTransactionServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _cardRepo = Substitute.For<ICreditCardRepository>();
        _invoiceRepo = Substitute.For<ICreditCardInvoiceRepository>();
        _ccTransactionRepo = Substitute.For<ICreditCardTransactionRepository>();
        _categoryRepo = Substitute.For<IRepository<Category>>();
        _invoiceServiceMock = Substitute.For<ICreditCardInvoiceService>();

        _unitOfWorkMock.CreditCards.Returns(_cardRepo);
        _unitOfWorkMock.CreditCardInvoices.Returns(_invoiceRepo);
        _unitOfWorkMock.CreditCardTransactions.Returns(_ccTransactionRepo);
        _unitOfWorkMock.Categories.Returns(_categoryRepo);
        _categoryRepo.GetAllAsync().Returns(new List<Category>());
        _ccTransactionRepo.AddAsync(Arg.Any<CreditCardTransaction>()).Returns(x => x.Arg<CreditCardTransaction>());

        _service = new CreditCardTransactionService(
            _unitOfWorkMock,
            _invoiceServiceMock,
            Substitute.For<IProcessLogger>());
    }

    private static CreditCard MakeCard(string id = "card1") => new()
    {
        Id = id,
        UserId = UserId,
        Name = "Cartão Teste",
        ClosingDay = 15,
        BillingDueDay = 22
    };

    private CreditCard SetupCardWithOpenInvoice(out CreditCardInvoice invoice)
    {
        var card = MakeCard();
        _cardRepo.GetByIdAsync(card.Id).Returns(card);

        var openInvoice = new CreditCardInvoice
        {
            Id = "inv-open",
            UserId = UserId,
            CreditCardId = card.Id,
            Status = InvoiceStatus.Open,
            ClosingDate = DateTime.UtcNow.AddDays(60) // longe da data de compra: não dispara redirecionamento
        };
        invoice = openInvoice;

        _invoiceServiceMock.EnsureCurrentOpenInvoiceAsync(UserId, card).Returns(openInvoice);
        _invoiceServiceMock
            .GetOrCreateInvoiceAsync(UserId, card, Arg.Any<string>(), Arg.Any<InvoiceStatus>())
            .Returns(openInvoice);

        return card;
    }

    [Fact]
    public async Task CreateAsync_WithUnknownCard_ShouldThrowKeyNotFound()
    {
        _cardRepo.GetByIdAsync("card1").Returns((CreditCard?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAsync(UserId,
            new CreateCreditCardTransactionRequestDto { CreditCardId = "card1", TotalAmount = 100m }));
    }

    [Fact]
    public async Task CreateAsync_SingleInstallment_ShouldCreateOneTransaction()
    {
        SetupCardWithOpenInvoice(out var invoice);

        var result = (await _service.CreateAsync(UserId, new CreateCreditCardTransactionRequestDto
        {
            CreditCardId = "card1",
            Description = "Compra única",
            TotalAmount = 150m,
            TotalInstallments = 1,
            PurchaseDate = DateTime.UtcNow
        })).ToList();

        Assert.Single(result);
        Assert.Equal(150m, result[0].InstallmentAmount);
        Assert.Equal(1, result[0].InstallmentNumber);
        Assert.Null(result[0].ParentTransactionId);
        await _ccTransactionRepo.Received(1).AddAsync(Arg.Any<CreditCardTransaction>());
        await _invoiceServiceMock.Received(1).RecalculateTotalAsync(UserId, invoice.Id);
    }

    [Fact]
    public async Task CreateAsync_WithThreeInstallments_ShouldDistributeRoundingOnLastInstallment()
    {
        SetupCardWithOpenInvoice(out _);

        var result = (await _service.CreateAsync(UserId, new CreateCreditCardTransactionRequestDto
        {
            CreditCardId = "card1",
            Description = "Parcelado",
            TotalAmount = 100m,
            TotalInstallments = 3,
            PurchaseDate = DateTime.UtcNow
        })).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(33.33m, result[0].InstallmentAmount);
        Assert.Equal(33.33m, result[1].InstallmentAmount);
        Assert.Equal(33.34m, result[2].InstallmentAmount); // resto do arredondamento
        Assert.Equal(100m, result.Sum(r => r.InstallmentAmount));

        // Primeira parcela é a "pai" das demais
        Assert.Null(result[0].ParentTransactionId);
        Assert.All(result.Skip(1), r => Assert.Equal(result[0].Id, r.ParentTransactionId));
    }

    [Fact]
    public async Task CreateAsync_Refund_ShouldCreateSingleNegativeTransaction()
    {
        SetupCardWithOpenInvoice(out _);

        var result = (await _service.CreateAsync(UserId, new CreateCreditCardTransactionRequestDto
        {
            CreditCardId = "card1",
            Description = "Estorno",
            TotalAmount = 80m,
            TotalInstallments = 4, // ignorado para estorno
            IsRefund = true,
            PurchaseDate = DateTime.UtcNow
        })).ToList();

        Assert.Single(result);
        Assert.Equal(-80m, result[0].InstallmentAmount);
        Assert.Equal("Refund", result[0].Type);
    }

    [Fact]
    public async Task CreateAsync_ManualSourceOnClosedInvoice_ShouldThrowInvalidOperation()
    {
        var card = MakeCard();
        _cardRepo.GetByIdAsync(card.Id).Returns(card);

        var closedInvoice = new CreditCardInvoice
        {
            Id = "inv-closed",
            UserId = UserId,
            CreditCardId = card.Id,
            Status = InvoiceStatus.Closed,
            ClosingDate = DateTime.UtcNow.AddDays(-30) // diferente da data de compra: não redireciona
        };
        _invoiceServiceMock.EnsureCurrentOpenInvoiceAsync(UserId, card).Returns(closedInvoice);
        _invoiceServiceMock
            .GetOrCreateInvoiceAsync(UserId, card, Arg.Any<string>(), Arg.Any<InvoiceStatus>())
            .Returns(closedInvoice);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(UserId,
            new CreateCreditCardTransactionRequestDto
            {
                CreditCardId = "card1",
                TotalAmount = 100m,
                Source = "manual",
                PurchaseDate = DateTime.UtcNow
            }));
    }

    [Fact]
    public async Task GetAllAsync_ShouldMapCardCurrencyAndCategory()
    {
        var card = MakeCard();
        _cardRepo.GetByUserAsync(UserId).Returns(new List<CreditCard> { card });
        _categoryRepo.GetAllAsync().Returns(new List<Category>
        {
            new() { Id = "cat1", UserId = UserId, Name = "Lazer", Color = "#00ff00" }
        });
        _ccTransactionRepo.GetByUserAsync(UserId).Returns(new List<CreditCardTransaction>
        {
            new() { UserId = UserId, CreditCardId = card.Id, CategoryId = "cat1", Description = "Cinema" }
        });

        var result = (await _service.GetAllAsync(UserId)).ToList();

        Assert.Single(result);
        Assert.Equal("Lazer", result[0].CategoryName);
        Assert.Equal("BRL", result[0].Currency);
    }

    [Fact]
    public async Task GetAllAsync_WithoutTransactions_ShouldReturnEmpty()
    {
        _ccTransactionRepo.GetByUserAsync(UserId).Returns(new List<CreditCardTransaction>());

        var result = await _service.GetAllAsync(UserId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByCardAsync_WithUnknownCard_ShouldThrowKeyNotFound()
    {
        _cardRepo.GetByIdAsync("card1").Returns((CreditCard?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetByCardAsync(UserId, "card1"));
    }

    [Fact]
    public async Task UpdateAsync_WithOpenInvoice_ShouldUpdateFieldsAndRecalculate()
    {
        var card = MakeCard();
        _cardRepo.GetByUserAsync(UserId).Returns(new List<CreditCard> { card });

        var transaction = new CreditCardTransaction
        {
            Id = "tx1",
            UserId = UserId,
            CreditCardId = card.Id,
            InvoiceId = "inv1",
            Description = "Original",
            TotalAmount = 100m,
            InstallmentAmount = 100m,
            InstallmentNumber = 1,
            TotalInstallments = 1
        };
        _ccTransactionRepo.GetByIdAsync("tx1").Returns(transaction);
        _ccTransactionRepo.GetByParentAsync(UserId, "tx1").Returns(new List<CreditCardTransaction>());
        _invoiceRepo.GetByIdAsync("inv1").Returns(new CreditCardInvoice
        {
            Id = "inv1",
            UserId = UserId,
            Status = InvoiceStatus.Open
        });

        var result = (await _service.UpdateAsync(UserId, "tx1", new UpdateCreditCardTransactionRequestDto
        {
            Description = "Atualizada",
            TotalAmount = 200m,
            PurchaseDate = DateTime.UtcNow
        })).ToList();

        Assert.Single(result);
        Assert.Equal("Atualizada", transaction.Description);
        Assert.Equal(200m, transaction.TotalAmount);
        Assert.Equal(200m, transaction.InstallmentAmount);
        await _ccTransactionRepo.Received(1).UpdateAsync(transaction);
        await _invoiceServiceMock.Received(1).RecalculateTotalAsync(UserId, "inv1");
    }

    [Fact]
    public async Task UpdateAsync_WithClosedInvoice_ShouldThrowInvalidOperation()
    {
        var transaction = new CreditCardTransaction
        {
            Id = "tx1",
            UserId = UserId,
            InvoiceId = "inv1"
        };
        _ccTransactionRepo.GetByIdAsync("tx1").Returns(transaction);
        _ccTransactionRepo.GetByParentAsync(UserId, "tx1").Returns(new List<CreditCardTransaction>());
        _invoiceRepo.GetByIdAsync("inv1").Returns(new CreditCardInvoice
        {
            Id = "inv1",
            UserId = UserId,
            Status = InvoiceStatus.Closed
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(UserId, "tx1",
            new UpdateCreditCardTransactionRequestDto { Description = "x", TotalAmount = 10m }));
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownTransaction_ShouldThrowKeyNotFound()
    {
        _ccTransactionRepo.GetByIdAsync("tx1").Returns((CreditCardTransaction?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(UserId, "tx1",
            new UpdateCreditCardTransactionRequestDto()));
    }

    [Fact]
    public async Task DeleteAsync_WithParentTransaction_ShouldSoftDeleteAllInstallments()
    {
        var openInvoice = new CreditCardInvoice { Id = "inv1", UserId = UserId, Status = InvoiceStatus.Open };
        var parent = new CreditCardTransaction
        {
            Id = "tx-parent",
            UserId = UserId,
            InvoiceId = "inv1",
            InstallmentNumber = 1
        };
        var child = new CreditCardTransaction
        {
            Id = "tx-child",
            UserId = UserId,
            InvoiceId = "inv2",
            ParentTransactionId = "tx-parent",
            InstallmentNumber = 2
        };
        _ccTransactionRepo.GetByIdAsync("tx-parent").Returns(parent);
        _ccTransactionRepo.GetByParentAsync(UserId, "tx-parent").Returns(new List<CreditCardTransaction> { child });
        _invoiceRepo.GetByIdAsync("inv1").Returns(openInvoice);
        _invoiceRepo.GetByIdAsync("inv2").Returns(new CreditCardInvoice { Id = "inv2", UserId = UserId, Status = InvoiceStatus.Pending });

        await _service.DeleteAsync(UserId, "tx-parent");

        Assert.True(parent.IsDeleted);
        Assert.True(child.IsDeleted);
        await _invoiceServiceMock.Received(1).RecalculateTotalAsync(UserId, "inv1");
        await _invoiceServiceMock.Received(1).RecalculateTotalAsync(UserId, "inv2");
    }

    [Fact]
    public async Task DeleteAsync_WithClosedInvoice_ShouldThrowInvalidOperation()
    {
        var transaction = new CreditCardTransaction { Id = "tx1", UserId = UserId, InvoiceId = "inv1" };
        _ccTransactionRepo.GetByIdAsync("tx1").Returns(transaction);
        _ccTransactionRepo.GetByParentAsync(UserId, "tx1").Returns(new List<CreditCardTransaction>());
        _invoiceRepo.GetByIdAsync("inv1").Returns(new CreditCardInvoice
        {
            Id = "inv1",
            UserId = UserId,
            Status = InvoiceStatus.Paid
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(UserId, "tx1"));
        Assert.False(transaction.IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_WithUnknownTransaction_ShouldThrowKeyNotFound()
    {
        _ccTransactionRepo.GetByIdAsync("tx1").Returns((CreditCardTransaction?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAsync(UserId, "tx1"));
    }

    [Fact]
    public async Task DeleteAsync_OfTransactionFromAnotherUser_ShouldThrowKeyNotFound()
    {
        _ccTransactionRepo.GetByIdAsync("tx1")
            .Returns(new CreditCardTransaction { Id = "tx1", UserId = "other-user", InvoiceId = "inv1" });

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAsync(UserId, "tx1"));
    }
}
