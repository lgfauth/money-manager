using Microsoft.Extensions.Logging;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Exceptions;
using MoneyManager.Domain.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace MoneyManager.Tests.Application.Services;

/// <summary>
/// Confirmação da importação com o <see cref="InvoiceImportReviewService"/> real (revalidação de itens,
/// cadastro do cartão e duplicidade) e o cadastro de transações substituído.
/// </summary>
public class CreditCardInvoiceImportConfirmTests
{
    private const string UserId = "user-1";
    private const string CardId = "64b7f0c2a1b2c3d4e5f60718";
    private const string ImportId = "64b7f0c2a1b2c3d4e5f60799";
    private const string Maria = "MARIA SILVA";
    private const string Joao = "JOAO SILVA";

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICreditCardRepository _cards = Substitute.For<ICreditCardRepository>();
    private readonly ICreditCardInvoiceImportRepository _imports = Substitute.For<ICreditCardInvoiceImportRepository>();
    private readonly ICreditCardTransactionRepository _existingTransactions = Substitute.For<ICreditCardTransactionRepository>();
    private readonly ICreditCardInvoiceRepository _invoices = Substitute.For<ICreditCardInvoiceRepository>();
    private readonly IRepository<Category> _categories = Substitute.For<IRepository<Category>>();
    private readonly ICreditCardImportEligibilityService _eligibility = Substitute.For<ICreditCardImportEligibilityService>();
    private readonly ICreditCardTransactionService _transactions = Substitute.For<ICreditCardTransactionService>();
    private readonly List<CreateCreditCardTransactionRequestDto> _createdRequests = [];
    private readonly List<InvoiceImportStatus> _savedStatuses = [];
    private readonly List<CreditCardTransaction> _existing = [];
    private readonly CreditCard _card;
    private readonly CreditCardInvoiceImportService _service;

    public CreditCardInvoiceImportConfirmTests()
    {
        // Hoje: 09/11/2026; fatura 2026-11 fechada em 03/11 com vencimento em 10/11
        _card = new CreditCard { Id = CardId, UserId = UserId, ClosingDay = 3, BillingDueDay = 10 };

        _unitOfWork.CreditCards.Returns(_cards);
        _unitOfWork.CreditCardInvoiceImports.Returns(_imports);
        _unitOfWork.CreditCardTransactions.Returns(_existingTransactions);
        _unitOfWork.CreditCardInvoices.Returns(_invoices);
        _unitOfWork.Categories.Returns(_categories);
        _cards.GetByIdAsync(CardId).Returns(_card);
        _existingTransactions.GetByCardAsync(UserId, CardId).Returns(_ => _existing.ToList());
        _categories.GetByUserIdAsync(UserId).Returns([new Category { Id = "cat-1", UserId = UserId, Name = "Mercado" }]);
        _imports.UpdateAsync(Arg.Do<CreditCardInvoiceImport>(j => _savedStatuses.Add(j.Status)))
            .Returns(ci => ci.Arg<CreditCardInvoiceImport>());

        var counter = 0;
        _transactions.CreateAsync(UserId, Arg.Do<CreateCreditCardTransactionRequestDto>(r => _createdRequests.Add(r)))
            .Returns(ci => (IEnumerable<CreditCardTransactionResponseDto>)
            [
                new CreditCardTransactionResponseDto
                {
                    Id = $"tx-{++counter}",
                    InvoiceId = $"inv-{ci.ArgAt<CreateCreditCardTransactionRequestDto>(1).TargetReferenceMonth}"
                }
            ]);

        _service = new CreditCardInvoiceImportService(
            _unitOfWork,
            _eligibility,
            new InvoiceImportReviewService(_unitOfWork),
            _transactions,
            Substitute.For<IImageNormalizationService>(),
            Substitute.For<ISpreadsheetTextExtractor>(),
            new FixedTimeProvider(new DateTimeOffset(2026, 11, 9, 15, 0, 0, TimeSpan.Zero)),
            Substitute.For<ILogger<CreditCardInvoiceImportService>>());
    }

    private static InvoiceImportItem Item(
        string id, string holder, string description, decimal amount, DateTime date,
        int? installment = null, int? total = null,
        CreditCardTransactionType type = CreditCardTransactionType.Purchase) => new()
    {
        Id = id,
        HolderName = holder,
        Description = description,
        Amount = amount,
        Date = date,
        InstallmentNumber = installment,
        TotalInstallments = total,
        Type = type
    };

    private CreditCardInvoiceImport Job(params InvoiceImportItem[] items)
    {
        var job = new CreditCardInvoiceImport
        {
            Id = ImportId,
            UserId = UserId,
            CreditCardId = CardId,
            Status = InvoiceImportStatus.AwaitingReview,
            ReferenceMonth = "2026-11",
            Invoice = new InvoiceImportDocumentInfo
            {
                ClosingDate = new DateTime(2026, 11, 3),
                DueDate = new DateTime(2026, 11, 10),
                ReferenceMonth = "2026-11"
            },
            Holders = items.Select(i => i.HolderName!).Distinct()
                .Select(name => new InvoiceImportHolder { Name = name }).ToList(),
            Items = [.. items]
        };
        _imports.GetByIdForUserAsync(UserId, ImportId).Returns(job);
        return job;
    }

    private static ConfirmInvoiceImportItemRequestDto Keep(InvoiceImportItem item) => new()
    {
        Id = item.Id,
        Date = item.Date,
        Description = item.Description,
        Amount = item.Amount,
        CategoryId = item.CategoryId,
        InstallmentNumber = item.InstallmentNumber,
        TotalInstallments = item.TotalInstallments
    };

    private static ConfirmInvoiceImportRequestDto Request(IEnumerable<string> holders, params InvoiceImportItem[] items) =>
        new() { SelectedHolders = [.. holders], Items = items.Select(Keep).ToList() };

    private async Task AssertNothingCreatedAsync()
    {
        await _transactions.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<CreateCreditCardTransactionRequestDto>());
        Assert.DoesNotContain(InvoiceImportStatus.Confirming, _savedStatuses);
    }

    [Fact]
    public async Task ConfirmAsync_ShouldCreateChosenHolderItemsOnImportedMonthAndMarkConfirmed()
    {
        // Arrange
        var cash = Item("a", Maria, "Mercado", 50m, new DateTime(2026, 10, 20));
        var newInstallments = Item("b", Maria, "Geladeira", 200m, new DateTime(2026, 10, 15), 1, 3);
        var oldInstallments = Item("c", Maria, "Notebook", 100m, new DateTime(2026, 3, 1), 8, 10);
        var refund = Item("d", Maria, "Estorno loja", 30m, new DateTime(2026, 10, 25), type: CreditCardTransactionType.Refund);
        var otherHolder = Item("e", Joao, "Cinema", 40m, new DateTime(2026, 10, 22));
        var job = Job(cash, newInstallments, oldInstallments, refund, otherHolder);
        var request = Request([Maria], cash, newInstallments, oldInstallments, refund, otherHolder);
        request.Items[0].CategoryId = "cat-1";

        // Act
        var dto = await _service.ConfirmAsync(UserId, ImportId, request);

        // Assert
        Assert.Equal(4, _createdRequests.Count);
        Assert.All(_createdRequests, r =>
        {
            Assert.Equal("invoice_import", r.Source);
            Assert.Equal("2026-11", r.TargetReferenceMonth);
            Assert.Equal(ImportId, r.InvoiceImportId);
            Assert.False(string.IsNullOrEmpty(r.ImportFingerprint));
        });
        Assert.DoesNotContain(_createdRequests, r => r.Description == "Cinema");

        var mercado = _createdRequests.Single(r => r.Description == "Mercado");
        Assert.Equal((50m, 1, 1, "cat-1"), (mercado.TotalAmount, mercado.TotalInstallments, mercado.FirstInstallmentNumber, mercado.CategoryId));

        var geladeira = _createdRequests.Single(r => r.Description == "Geladeira");
        Assert.Equal((600m, 3, 1), (geladeira.TotalAmount, geladeira.TotalInstallments, geladeira.FirstInstallmentNumber));

        var notebook = _createdRequests.Single(r => r.Description == "Notebook");
        Assert.Equal((1000m, 10, 8), (notebook.TotalAmount, notebook.TotalInstallments, notebook.FirstInstallmentNumber));

        var estorno = _createdRequests.Single(r => r.Description == "Estorno loja");
        Assert.True(estorno.IsRefund);
        Assert.Equal(30m, estorno.TotalAmount);

        Assert.Equal([InvoiceImportStatus.Confirming, InvoiceImportStatus.Confirmed], _savedStatuses);
        Assert.Equal("Confirmed", dto.Status);
        Assert.Equal([Maria], job.SelectedHolders);
        Assert.Equal(4, job.Result!.CreatedCount);
        Assert.Equal(0, job.Result.SkippedCount);
        Assert.Equal(["inv-2026-11"], job.Result.AffectedInvoiceIds);
        Assert.NotNull(job.ConfirmedAt);
    }

    [Fact]
    public async Task ConfirmAsync_WithSingleHolder_ShouldNotRequireSelection()
    {
        // Arrange
        var item = Item("a", Maria, "Mercado", 50m, new DateTime(2026, 10, 20));
        var job = Job(item);

        // Act
        await _service.ConfirmAsync(UserId, ImportId, Request([], item));

        // Assert
        Assert.Single(_createdRequests);
        Assert.Equal([Maria], job.SelectedHolders);
    }

    [Fact]
    public async Task ConfirmAsync_ShouldApplyUserEdits()
    {
        // Arrange
        var item = Item("a", Maria, "MERC*123", 50m, new DateTime(2026, 10, 20));
        Job(item);
        var request = Request([], item);
        request.Items[0].Description = "Mercado do bairro";
        request.Items[0].Amount = 55.5m;
        request.Items[0].Date = new DateTime(2026, 10, 21);

        // Act
        await _service.ConfirmAsync(UserId, ImportId, request);

        // Assert
        var created = Assert.Single(_createdRequests);
        Assert.Equal(("Mercado do bairro", 55.5m, new DateTime(2026, 10, 21)), (created.Description, created.TotalAmount, created.PurchaseDate));
    }

    [Theory]
    [InlineData(InvoiceImportStatus.Queued)]
    [InlineData(InvoiceImportStatus.Confirming)]
    [InlineData(InvoiceImportStatus.Confirmed)]
    [InlineData(InvoiceImportStatus.Failed)]
    public async Task ConfirmAsync_WhenNotAwaitingReview_ShouldThrowConflict(InvoiceImportStatus status)
    {
        // Arrange
        var item = Item("a", Maria, "Mercado", 50m, new DateTime(2026, 10, 20));
        Job(item).Status = status;

        // Act & Assert
        await Assert.ThrowsAsync<InvoiceImportConflictException>(() => _service.ConfirmAsync(UserId, ImportId, Request([], item)));
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task ConfirmAsync_WhenAnotherRequestClaimedFirst_ShouldThrowConflictWithoutCreating()
    {
        // Arrange
        var item = Item("a", Maria, "Mercado", 50m, new DateTime(2026, 10, 20));
        Job(item);
        _imports.UpdateAsync(Arg.Any<CreditCardInvoiceImport>()).ThrowsAsync(new ConcurrencyException());

        // Act & Assert
        await Assert.ThrowsAsync<InvoiceImportConflictException>(() => _service.ConfirmAsync(UserId, ImportId, Request([], item)));
        await _transactions.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<CreateCreditCardTransactionRequestDto>());
    }

    [Fact]
    public async Task ConfirmAsync_WhenImportIsFromAnotherUser_ShouldThrowNotFound()
    {
        // Arrange
        _imports.GetByIdForUserAsync(UserId, ImportId).Returns((CreditCardInvoiceImport?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.ConfirmAsync(UserId, ImportId, new ConfirmInvoiceImportRequestDto()));
    }

    [Fact]
    public async Task ConfirmAsync_WhenCardBecameSyncedByOpenFinance_ShouldThrowConflictWithoutCreating()
    {
        // Arrange
        var item = Item("a", Maria, "Mercado", 50m, new DateTime(2026, 10, 20));
        Job(item);
        _eligibility.IsSyncedByOpenFinanceAsync(UserId, CardId).Returns(true);

        // Act
        var ex = await Assert.ThrowsAsync<InvoiceImportConflictException>(() => _service.ConfirmAsync(UserId, ImportId, Request([], item)));

        // Assert
        Assert.Equal(CreditCardImportEligibilityService.SyncedCardMessage, ex.Message);
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task ConfirmAsync_WhenCardScheduleDiffersMoreThanTolerance_ShouldThrowConflictUntilCardIsAdjusted()
    {
        // Arrange: fechamento do documento dia 3, cadastro passou a ser dia 10
        var item = Item("a", Maria, "Mercado", 50m, new DateTime(2026, 10, 20));
        Job(item);
        _card.ClosingDay = 10;
        _card.BillingDueDay = 17;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvoiceImportConflictException>(() => _service.ConfirmAsync(UserId, ImportId, Request([], item)));
        Assert.Contains("ajuste o cadastro do cartão", ex.Message);
        await AssertNothingCreatedAsync();

        // Após ajustar o cartão, confirma sem reenviar a fatura
        _card.ClosingDay = 3;
        _card.BillingDueDay = 10;
        var dto = await _service.ConfirmAsync(UserId, ImportId, Request([], item));
        Assert.Equal("Confirmed", dto.Status);
    }

    [Fact]
    public async Task ConfirmAsync_WithSeveralHoldersAndNoneChosen_ShouldThrowValidation()
    {
        // Arrange
        var a = Item("a", Maria, "Mercado", 50m, new DateTime(2026, 10, 20));
        var b = Item("b", Joao, "Cinema", 40m, new DateTime(2026, 10, 22));
        Job(a, b);

        // Act
        var ex = await Assert.ThrowsAsync<InvoiceImportValidationException>(() => _service.ConfirmAsync(UserId, ImportId, Request([], a, b)));

        // Assert
        Assert.Equal(CreditCardInvoiceImportService.HolderRequiredMessage, ex.Message);
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task ConfirmAsync_WithUnknownHolder_ShouldThrowValidation()
    {
        // Arrange
        var a = Item("a", Maria, "Mercado", 50m, new DateTime(2026, 10, 20));
        var b = Item("b", Joao, "Cinema", 40m, new DateTime(2026, 10, 22));
        Job(a, b);

        // Act & Assert
        await Assert.ThrowsAsync<InvoiceImportValidationException>(() =>
            _service.ConfirmAsync(UserId, ImportId, Request(["FULANO"], a, b)));
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task ConfirmAsync_WithItemFromAnotherImport_ShouldThrowValidation()
    {
        // Arrange
        var item = Item("a", Maria, "Mercado", 50m, new DateTime(2026, 10, 20));
        Job(item);
        var request = Request([], item);
        request.Items.Add(new ConfirmInvoiceImportItemRequestDto { Id = "zzz", Description = "x", Amount = 1m, Date = new DateTime(2026, 10, 1) });

        // Act & Assert
        await Assert.ThrowsAsync<InvoiceImportValidationException>(() => _service.ConfirmAsync(UserId, ImportId, request));
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task ConfirmAsync_WithUnknownCategory_ShouldThrowValidation()
    {
        // Arrange
        var item = Item("a", Maria, "Mercado", 50m, new DateTime(2026, 10, 20));
        Job(item);
        var request = Request([], item);
        request.Items[0].CategoryId = "cat-de-outro";

        // Act & Assert
        await Assert.ThrowsAsync<InvoiceImportValidationException>(() => _service.ConfirmAsync(UserId, ImportId, request));
        await AssertNothingCreatedAsync();
    }

    [Theory]
    [InlineData(2026, 11, 5, 50)]   // depois do fechamento
    [InlineData(2026, 8, 1, 50)]    // à vista com mais de 60 dias
    [InlineData(2026, 10, 20, 0)]   // valor zero
    public async Task ConfirmAsync_WithBlockedItemNotCorrected_ShouldThrowValidationAndCreateNothing(int year, int month, int day, int amount)
    {
        // Arrange
        var ok = Item("a", Maria, "Mercado", 50m, new DateTime(2026, 10, 20));
        var blocked = Item("b", Maria, "Farmácia", 30m, new DateTime(2026, 10, 20));
        Job(ok, blocked);
        var request = Request([], ok, blocked);
        request.Items[1].Date = new DateTime(year, month, day);
        request.Items[1].Amount = amount;

        // Act
        var ex = await Assert.ThrowsAsync<InvoiceImportValidationException>(() => _service.ConfirmAsync(UserId, ImportId, request));

        // Assert
        Assert.Contains(ex.Errors, e => e.StartsWith("Farmácia"));
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task ConfirmAsync_WhenBlockedItemIsCorrected_ShouldCreateIt()
    {
        // Arrange: o item veio sem data da IA e o usuário informou a data na revisão
        var item = Item("a", Maria, "Mercado", 50m, new DateTime(2026, 10, 20));
        item.Date = null;
        item.Warnings = [InvoiceImportWarningCode.MissingDate];
        Job(item);
        var request = Request([], item);
        request.Items[0].Date = new DateTime(2026, 10, 20);

        // Act
        await _service.ConfirmAsync(UserId, ImportId, request);

        // Assert
        Assert.Single(_createdRequests);
    }

    [Fact]
    public async Task ConfirmAsync_WhenFailingMidway_ShouldReturnToReviewAndRetryWithoutDuplicating()
    {
        // Arrange
        var first = Item("a", Maria, "Mercado", 50m, new DateTime(2026, 10, 20));
        var second = Item("b", Maria, "Farmácia", 30m, new DateTime(2026, 10, 21));
        var job = Job(first, second);
        _transactions.CreateAsync(UserId, Arg.Is<CreateCreditCardTransactionRequestDto>(r => r.Description == "Farmácia"))
            .ThrowsAsync(new TimeoutException("mongo"));

        // Act: 1ª tentativa cadastra "Mercado" e falha em "Farmácia"
        await Assert.ThrowsAsync<TimeoutException>(() => _service.ConfirmAsync(UserId, ImportId, Request([], first, second)));

        // Assert
        Assert.Equal(InvoiceImportStatus.AwaitingReview, job.Status);
        Assert.Equal([InvoiceImportStatus.Confirming, InvoiceImportStatus.AwaitingReview], _savedStatuses);

        // Arrange: "Mercado" ficou gravado com o fingerprint; a falha foi corrigida
        var mercado = _createdRequests.Single(r => r.Description == "Mercado");
        _existing.Add(new CreditCardTransaction
        {
            Id = "tx-mercado", UserId = UserId, CreditCardId = CardId, Description = "Mercado",
            InstallmentAmount = 50m, PurchaseDate = mercado.PurchaseDate, ImportFingerprint = mercado.ImportFingerprint
        });
        _createdRequests.Clear();
        _transactions.CreateAsync(UserId, Arg.Is<CreateCreditCardTransactionRequestDto>(r => r.Description == "Farmácia"))
            .Returns([new CreditCardTransactionResponseDto { Id = "tx-farmacia", InvoiceId = "inv-2026-11" }]);

        // Act: 2ª tentativa
        await _service.ConfirmAsync(UserId, ImportId, Request([], first, second));

        // Assert: só "Farmácia" é cadastrada; "Mercado" é pulado
        Assert.Equal(["Farmácia"], _createdRequests.Select(r => r.Description));
        Assert.Equal(InvoiceImportStatus.Confirmed, job.Status);
        Assert.Equal(1, job.Result!.CreatedCount);
        Assert.Equal(1, job.Result.SkippedCount);
    }
}
