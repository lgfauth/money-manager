using Microsoft.Extensions.Logging;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Exceptions;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Tests.Application.Validators;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace MoneyManager.Tests.Application.Services;

public class CreditCardInvoiceImportServiceTests
{
    private const string UserId = "user-1";
    private const string CardId = "64b7f0c2a1b2c3d4e5f60718";
    private const string ImportId = "64b7f0c2a1b2c3d4e5f60799";

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICreditCardRepository _cards = Substitute.For<ICreditCardRepository>();
    private readonly ICreditCardInvoiceImportRepository _imports = Substitute.For<ICreditCardInvoiceImportRepository>();
    private readonly ICreditCardInvoiceImportFileRepository _files = Substitute.For<ICreditCardInvoiceImportFileRepository>();
    private readonly ICreditCardImportEligibilityService _eligibility = Substitute.For<ICreditCardImportEligibilityService>();
    private readonly IInvoiceImportReviewService _review = Substitute.For<IInvoiceImportReviewService>();
    private readonly IImageNormalizationService _images = Substitute.For<IImageNormalizationService>();
    private readonly ISpreadsheetTextExtractor _spreadsheets = Substitute.For<ISpreadsheetTextExtractor>();
    private readonly ICreditCardTransactionService _transactions = Substitute.For<ICreditCardTransactionService>();
    private readonly FixedTimeProvider _time = new(new DateTimeOffset(2026, 11, 9, 15, 0, 0, TimeSpan.Zero));
    private readonly CreditCardInvoiceImportService _service;

    public CreditCardInvoiceImportServiceTests()
    {
        _unitOfWork.CreditCards.Returns(_cards);
        _unitOfWork.CreditCardInvoiceImports.Returns(_imports);
        _unitOfWork.CreditCardInvoiceImportFiles.Returns(_files);
        _cards.GetByIdAsync(CardId).Returns(new CreditCard { Id = CardId, UserId = UserId, Name = "Nubank" });
        _imports.AddAsync(Arg.Any<CreditCardInvoiceImport>()).Returns(ci => ci.Arg<CreditCardInvoiceImport>());
        // Normalização falsa: devolve um JPEG com metade do tamanho de entrada
        _images.Normalize(Arg.Any<byte[]>())
            .Returns(ci => new NormalizedImage(new byte[ci.Arg<byte[]>().Length / 2], "image/jpeg", 100, 100));
        _service = new CreditCardInvoiceImportService(
            _unitOfWork, _eligibility, _review, _transactions, _images, _spreadsheets, _time,
            Substitute.For<ILogger<CreditCardInvoiceImportService>>());
    }

    private static CreateInvoiceImportRequestDto Request(params InvoiceImportUploadFileRequestDto[] files) =>
        new() { CreditCardId = CardId, Files = [.. files] };

    private async Task AssertNothingCreatedAsync()
    {
        await _files.DidNotReceive().AddManyAsync(Arg.Any<IEnumerable<CreditCardInvoiceImportFile>>());
        await _imports.DidNotReceive().AddAsync(Arg.Any<CreditCardInvoiceImport>());
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_WithImages_ShouldSaveFilesInOrderThenQueueJob()
    {
        // Arrange
        var first = CreateInvoiceImportValidatorTests.Jpeg(100);
        var second = CreateInvoiceImportValidatorTests.Jpeg(200);
        List<CreditCardInvoiceImportFile>? savedFiles = null;
        await _files.AddManyAsync(Arg.Do<IEnumerable<CreditCardInvoiceImportFile>>(f => savedFiles = f.ToList()));

        // Act
        var result = await _service.CreateAsync(UserId, Request(first, second));

        // Assert
        Assert.Equal("Queued", result.Status);
        await _imports.Received(1).AddAsync(Arg.Is<CreditCardInvoiceImport>(j =>
            j.Id == result.Id && j.UserId == UserId && j.CreditCardId == CardId
            && j.Status == InvoiceImportStatus.Queued && j.SourceType == InvoiceImportSourceType.Images
            && j.FileCount == 2 && j.Attempts == 0));
        Assert.NotNull(savedFiles);
        Assert.Equal([0, 1], savedFiles!.Select(f => f.Order));
        Assert.All(savedFiles, f => Assert.Equal(result.Id, f.ImportId));
        Assert.All(savedFiles, f => Assert.Equal(UserId, f.UserId));
        // O arquivo gravado é o normalizado (JPEG), não o original
        Assert.Equal([50L, 100L], savedFiles.Select(f => f.SizeBytes));
        Assert.Equal([50, 100], savedFiles.Select(f => f.Content.Length));
        Assert.All(savedFiles, f => Assert.Equal("image/jpeg", f.ContentType));
        _images.Received(2).Normalize(Arg.Any<byte[]>());
        Received.InOrder(() =>
        {
            _files.AddManyAsync(Arg.Any<IEnumerable<CreditCardInvoiceImportFile>>());
            _imports.AddAsync(Arg.Any<CreditCardInvoiceImport>());
        });
    }

    [Fact]
    public async Task CreateAsync_WithPdf_ShouldQueuePdfJobWithNormalizedContentType()
    {
        // Arrange
        var pdf = CreateInvoiceImportValidatorTests.Pdf();
        pdf.ContentType = "Application/PDF; charset=binary";

        // Act
        await _service.CreateAsync(UserId, Request(pdf));

        // Assert
        await _imports.Received(1).AddAsync(Arg.Is<CreditCardInvoiceImport>(j => j.SourceType == InvoiceImportSourceType.Pdf));
        await _files.Received(1).AddManyAsync(Arg.Is<IEnumerable<CreditCardInvoiceImportFile>>(f =>
            f.Single().ContentType == "application/pdf"));
    }

    [Fact]
    public async Task CreateAsync_WithPdf_ShouldNotNormalizeAndStoreOriginalBytes()
    {
        // Arrange
        var pdf = CreateInvoiceImportValidatorTests.Pdf(300);

        // Act
        await _service.CreateAsync(UserId, Request(pdf));

        // Assert
        _images.DidNotReceive().Normalize(Arg.Any<byte[]>());
        await _files.Received(1).AddManyAsync(Arg.Is<IEnumerable<CreditCardInvoiceImportFile>>(f =>
            f.Single().Content == pdf.Content && f.Single().SizeBytes == 300));
    }

    [Fact]
    public async Task CreateAsync_WithSpreadsheet_ShouldStoreOnlyConvertedText()
    {
        // Arrange
        var xlsx = new InvoiceImportUploadFileRequestDto
        {
            FileName = "fatura.xlsx",
            ContentType = "application/octet-stream",
            Content = [0x50, 0x4B, 0x03, 0x04, 1, 2, 3]
        };
        _spreadsheets.Extract(xlsx.Content).Returns(new SpreadsheetContent(
            [new SpreadsheetSheet("Lançamentos", [new SpreadsheetRow(5, ["2026-10-03", "MERCADO", "123.45"])])], false));
        List<CreditCardInvoiceImportFile>? saved = null;
        await _files.AddManyAsync(Arg.Do<IEnumerable<CreditCardInvoiceImportFile>>(f => saved = f.ToList()));

        // Act
        await _service.CreateAsync(UserId, Request(xlsx));

        // Assert
        await _imports.Received(1).AddAsync(Arg.Is<CreditCardInvoiceImport>(j => j.SourceType == InvoiceImportSourceType.Spreadsheet));
        var file = Assert.Single(saved!);
        Assert.Equal("text/plain", file.ContentType);
        var text = System.Text.Encoding.UTF8.GetString(file.Content);
        Assert.Contains("[Aba: Lançamentos]", text);
        Assert.Contains("5\t2026-10-03\tMERCADO\t123.45", text);
        Assert.False(file.Content.AsSpan().StartsWith(xlsx.Content.AsSpan(0, 4))); // o .xlsx original não é gravado
    }

    [Fact]
    public async Task CreateAsync_WhenSpreadsheetIsRejected_ShouldCreateNothing()
    {
        // Arrange
        var xlsx = new InvoiceImportUploadFileRequestDto
        {
            FileName = "fatura.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Content = [0x50, 0x4B, 0x03, 0x04]
        };
        _spreadsheets.Extract(Arg.Any<byte[]>()).Throws(new InvalidSpreadsheetException("bomba"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidSpreadsheetException>(() => _service.CreateAsync(UserId, Request(xlsx)));
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task CreateAsync_WhenAnImageCannotBeNormalized_ShouldCreateNothing()
    {
        // Arrange
        _images.Normalize(Arg.Any<byte[]>()).Throws(new ImageNormalizationException("corrompida"));

        // Act & Assert
        await Assert.ThrowsAsync<ImageNormalizationException>(() =>
            _service.CreateAsync(UserId, Request(CreateInvoiceImportValidatorTests.Jpeg(), CreateInvoiceImportValidatorTests.Jpeg())));
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task CreateAsync_WhenCardBelongsToAnotherUser_ShouldThrowNotFoundAndCreateNothing()
    {
        // Arrange
        _cards.GetByIdAsync(CardId).Returns(new CreditCard { Id = CardId, UserId = "outro" });

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.CreateAsync(UserId, Request(CreateInvoiceImportValidatorTests.Pdf())));
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task CreateAsync_WhenCardIsDeletedOrMissing_ShouldThrowNotFound()
    {
        // Arrange
        _cards.GetByIdAsync(CardId).Returns(new CreditCard { Id = CardId, UserId = UserId, IsDeleted = true });

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.CreateAsync(UserId, Request(CreateInvoiceImportValidatorTests.Pdf())));

        _cards.GetByIdAsync(CardId).Returns((CreditCard?)null);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.CreateAsync(UserId, Request(CreateInvoiceImportValidatorTests.Pdf())));
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task CreateAsync_WithMalformedCardId_ShouldThrowNotFoundWithoutQuerying()
    {
        // Arrange
        var request = Request(CreateInvoiceImportValidatorTests.Pdf());
        request.CreditCardId = "abc";

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAsync(UserId, request));
        await _cards.DidNotReceive().GetByIdAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task CreateAsync_WhenCardIsSyncedByOpenFinance_ShouldThrowConflictAndCreateNothing()
    {
        // Arrange
        _eligibility.IsSyncedByOpenFinanceAsync(UserId, CardId).Returns(true);

        // Act
        var ex = await Assert.ThrowsAsync<InvoiceImportConflictException>(() =>
            _service.CreateAsync(UserId, Request(CreateInvoiceImportValidatorTests.Pdf())));

        // Assert
        Assert.Equal(CreditCardImportEligibilityService.SyncedCardMessage, ex.Message);
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task CreateAsync_WhenCardHasActiveJob_ShouldThrowConflictAndCreateNothing()
    {
        // Arrange
        _imports.GetActiveByCardAsync(UserId, CardId)
            .Returns(new CreditCardInvoiceImport { UserId = UserId, CreditCardId = CardId, Status = InvoiceImportStatus.Processing });

        // Act
        var ex = await Assert.ThrowsAsync<InvoiceImportConflictException>(() =>
            _service.CreateAsync(UserId, Request(CreateInvoiceImportValidatorTests.Pdf())));

        // Assert
        Assert.Equal(CreditCardInvoiceImportService.ActiveImportMessage, ex.Message);
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task CreateAsync_WhenJobInsertFails_ShouldRemoveSavedFilesAndRethrow()
    {
        // Arrange
        _imports.AddAsync(Arg.Any<CreditCardInvoiceImport>()).ThrowsAsync(new TimeoutException("mongo"));

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() =>
            _service.CreateAsync(UserId, Request(CreateInvoiceImportValidatorTests.Pdf())));
        await _files.Received(1).DeleteByImportAsync(UserId, Arg.Any<string>());
    }

    // --- GetByIdAsync / GetRecentAsync ---

    [Fact]
    public async Task GetByIdAsync_ShouldMapReviewDataWithBlockingFlags()
    {
        // Arrange
        var job = new CreditCardInvoiceImport
        {
            Id = ImportId,
            UserId = UserId,
            CreditCardId = CardId,
            Status = InvoiceImportStatus.AwaitingReview,
            SourceType = InvoiceImportSourceType.Pdf,
            ReferenceMonth = "2026-10",
            Invoice = new InvoiceImportDocumentInfo { Issuer = "Banco X", TotalAmount = 150m },
            Holders = [new InvoiceImportHolder { Name = "MARIA", Type = InvoiceImportHolderType.Primary, IsSuggested = true }],
            Warnings = [new InvoiceImportWarning { Code = InvoiceImportWarningCode.ClosingDayMismatch, Message = "m", IsBlocking = true }],
            Items =
            [
                new InvoiceImportItem
                {
                    Description = "Mercado",
                    Amount = 150m,
                    Warnings = [InvoiceImportWarningCode.AlreadyImported, InvoiceImportWarningCode.LowConfidence],
                    Source = new InvoiceImportSourceReference { Page = 2 }
                }
            ]
        };
        _imports.GetByIdForUserAsync(UserId, ImportId).Returns(job);

        // Act
        var dto = await _service.GetByIdAsync(UserId, ImportId);

        // Assert
        Assert.Equal("AwaitingReview", dto.Status);
        Assert.Equal("Pdf", dto.SourceType);
        Assert.Equal("Banco X", dto.Invoice!.Issuer);
        Assert.Equal("Primary", Assert.Single(dto.Holders).Type);
        Assert.True(Assert.Single(dto.Warnings).IsBlocking);
        var item = Assert.Single(dto.Items);
        Assert.Equal(2, item.Source!.Page);
        Assert.Equal("Purchase", item.Type);
        Assert.Collection(item.Warnings,
            w => { Assert.Equal("AlreadyImported", w.Code); Assert.True(w.IsBlocking); },
            w => { Assert.Equal("LowConfidence", w.Code); Assert.False(w.IsBlocking); });
    }

    [Fact]
    public async Task GetByIdAsync_WhenImportIsFromAnotherUser_ShouldThrowNotFound()
    {
        // Arrange
        _imports.GetByIdForUserAsync(UserId, ImportId).Returns((CreditCardInvoiceImport?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetByIdAsync(UserId, ImportId));
    }

    [Fact]
    public async Task GetRecentAsync_ShouldReturnOnlyUserImportsWithLimit()
    {
        // Arrange
        _imports.GetRecentByUserAsync(UserId, CreditCardInvoiceImportService.RecentLimit).Returns(
        [
            new CreditCardInvoiceImport { Id = "a", UserId = UserId },
            new CreditCardInvoiceImport { Id = "b", UserId = "outro" },
            new CreditCardInvoiceImport { Id = "c", UserId = UserId, IsDeleted = true }
        ]);

        // Act
        var result = await _service.GetRecentAsync(UserId);

        // Assert
        Assert.Equal("a", Assert.Single(result).Id);
    }

    // --- GetEligibleCardsAsync ---

    [Fact]
    public async Task GetEligibleCardsAsync_ShouldMapCardsFromEligibilityRule()
    {
        // Arrange
        _eligibility.GetEligibleCardsAsync(UserId).Returns(
        [
            new CreditCard { Id = CardId, UserId = UserId, Name = "Nubank", Brand = "Mastercard", Color = "#820AD1", ClosingDay = 3, BillingDueDay = 10 }
        ]);

        // Act
        var result = await _service.GetEligibleCardsAsync(UserId);

        // Assert
        var card = Assert.Single(result);
        Assert.Equal(CardId, card.Id);
        Assert.Equal("Nubank", card.Name);
        Assert.Equal("Mastercard", card.Brand);
        Assert.Equal("#820AD1", card.Color);
        Assert.Equal(3, card.ClosingDay);
        Assert.Equal(10, card.BillingDueDay);
    }

    // --- DiscardAsync ---

    [Theory]
    [InlineData(InvoiceImportStatus.AwaitingReview)]
    [InlineData(InvoiceImportStatus.Failed)]
    public async Task DiscardAsync_WhenDiscardable_ShouldSoftDeleteAsDiscarded(InvoiceImportStatus status)
    {
        // Arrange
        _imports.GetByIdForUserAsync(UserId, ImportId)
            .Returns(new CreditCardInvoiceImport { Id = ImportId, UserId = UserId, Status = status });

        // Act
        await _service.DiscardAsync(UserId, ImportId);

        // Assert
        await _imports.Received(1).UpdateAsync(Arg.Is<CreditCardInvoiceImport>(j =>
            j.Status == InvoiceImportStatus.Discarded && j.IsDeleted));
        await _files.Received(1).DeleteByImportAsync(UserId, ImportId);
    }

    [Theory]
    [InlineData(InvoiceImportStatus.Queued)]
    [InlineData(InvoiceImportStatus.Processing)]
    [InlineData(InvoiceImportStatus.Confirming)]
    [InlineData(InvoiceImportStatus.Confirmed)]
    public async Task DiscardAsync_WhenNotDiscardable_ShouldThrowConflict(InvoiceImportStatus status)
    {
        // Arrange
        _imports.GetByIdForUserAsync(UserId, ImportId)
            .Returns(new CreditCardInvoiceImport { Id = ImportId, UserId = UserId, Status = status });

        // Act & Assert
        await Assert.ThrowsAsync<InvoiceImportConflictException>(() => _service.DiscardAsync(UserId, ImportId));
        await _imports.DidNotReceive().UpdateAsync(Arg.Any<CreditCardInvoiceImport>());
    }

    [Fact]
    public async Task DiscardAsync_WhenChangedConcurrently_ShouldThrowConflict()
    {
        // Arrange
        _imports.GetByIdForUserAsync(UserId, ImportId)
            .Returns(new CreditCardInvoiceImport { Id = ImportId, UserId = UserId, Status = InvoiceImportStatus.AwaitingReview });
        _imports.UpdateAsync(Arg.Any<CreditCardInvoiceImport>()).ThrowsAsync(new ConcurrencyException());

        // Act & Assert
        await Assert.ThrowsAsync<InvoiceImportConflictException>(() => _service.DiscardAsync(UserId, ImportId));
    }

    [Fact]
    public async Task DiscardAsync_WhenImportIsFromAnotherUser_ShouldThrowNotFound()
    {
        // Arrange
        _imports.GetByIdForUserAsync(UserId, ImportId).Returns((CreditCardInvoiceImport?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DiscardAsync(UserId, ImportId));
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
