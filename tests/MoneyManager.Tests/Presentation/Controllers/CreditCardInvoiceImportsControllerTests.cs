using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Application.Services;
using MoneyManager.Application.Validators;
using MoneyManager.Domain.Exceptions;
using MoneyManager.Presentation.Controllers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace MoneyManager.Tests.Presentation.Controllers;

public class CreditCardInvoiceImportsControllerTests
{
    private const string UserId = "user-abc";
    private const string CardId = "64b7f0c2a1b2c3d4e5f60718";
    private const string ImportId = "64b7f0c2a1b2c3d4e5f60799";

    private readonly ICreditCardInvoiceImportService _service = Substitute.For<ICreditCardInvoiceImportService>();
    private readonly CreditCardInvoiceImportsController _controller;

    public CreditCardInvoiceImportsControllerTests()
    {
        // Validator real: o controller é quem invoca a validação
        _controller = new CreditCardInvoiceImportsController(
            _service, new CreateInvoiceImportValidator(), new ConfirmInvoiceImportValidator())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, UserId)], "TestAuth"))
                }
            }
        };
    }

    private static IFormFile FormFile(string contentType, byte[] content, string name = "arquivo") =>
        new FormFile(new MemoryStream(content), 0, content.Length, "files", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

    private static IFormFile PdfFile() => FormFile("application/pdf", "%PDF-1.7 conteudo"u8.ToArray(), "fatura.pdf");

    private static IFormFile JpegFile() => FormFile("image/jpeg", [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10], "print.jpg");

    // --- Create ---

    [Fact]
    public async Task Create_WithValidPdf_ShouldReturnAcceptedWithIdAndStatus()
    {
        // Arrange
        var created = new InvoiceImportCreatedResponseDto { Id = ImportId, Status = "Queued" };
        _service.CreateAsync(UserId, Arg.Any<CreateInvoiceImportRequestDto>()).Returns(created);

        // Act
        var result = await _controller.Create(CardId, [PdfFile()]);

        // Assert
        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.Equal(StatusCodes.Status202Accepted, accepted.StatusCode);
        Assert.Equal(created, accepted.Value);
        Assert.Equal($"/api/credit-card-invoice-imports/{ImportId}", accepted.Location);
        await _service.Received(1).CreateAsync(UserId, Arg.Is<CreateInvoiceImportRequestDto>(r =>
            r.CreditCardId == CardId
            && r.Files.Count == 1
            && r.Files[0].ContentType == "application/pdf"
            && r.Files[0].FileName == "fatura.pdf"
            && r.Files[0].SizeBytes > 0));
    }

    [Fact]
    public async Task Create_MixingPdfAndImage_ShouldReturnBadRequestWithoutCallingService()
    {
        // Act
        var result = await _controller.Create(CardId, [PdfFile(), JpegFile()]);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        await _service.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<CreateInvoiceImportRequestDto>());
    }

    [Fact]
    public async Task Create_WithoutFilesOrCard_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.Create(null, null);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        await _service.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<CreateInvoiceImportRequestDto>());
    }

    [Fact]
    public async Task Create_WhenCardNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _service.CreateAsync(UserId, Arg.Any<CreateInvoiceImportRequestDto>())
            .ThrowsAsync(new KeyNotFoundException(CreditCardInvoiceImportService.CardNotFoundMessage));

        // Act
        var result = await _controller.Create(CardId, [PdfFile()]);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Theory]
    [InlineData(CreditCardImportEligibilityService.SyncedCardMessage)]
    [InlineData(CreditCardInvoiceImportService.ActiveImportMessage)]
    public async Task Create_WhenServiceReportsConflict_ShouldReturnConflict(string message)
    {
        // Arrange
        _service.CreateAsync(UserId, Arg.Any<CreateInvoiceImportRequestDto>())
            .ThrowsAsync(new InvoiceImportConflictException(message));

        // Act
        var result = await _controller.Create(CardId, [JpegFile()]);

        // Assert
        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, obj.StatusCode);
    }

    // --- Queries ---

    [Fact]
    public async Task GetEligibleCards_ShouldReturnOkWithCards()
    {
        // Arrange
        IReadOnlyList<InvoiceImportEligibleCardResponseDto> cards = [new() { Id = CardId, Name = "Nubank" }];
        _service.GetEligibleCardsAsync(UserId).Returns(cards);

        // Act
        var result = await _controller.GetEligibleCards();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(cards, ok.Value);
    }

    [Fact]
    public async Task GetRecent_ShouldReturnOkWithUserImports()
    {
        // Arrange
        IReadOnlyList<CreditCardInvoiceImportResponseDto> imports = [new() { Id = ImportId }];
        _service.GetRecentAsync(UserId).Returns(imports);

        // Act
        var result = await _controller.GetRecent();

        // Assert
        Assert.Same(imports, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnOk()
    {
        // Arrange
        var dto = new CreditCardInvoiceImportResponseDto { Id = ImportId, Status = "AwaitingReview" };
        _service.GetByIdAsync(UserId, ImportId).Returns(dto);

        // Act
        var result = await _controller.GetById(ImportId);

        // Assert
        Assert.Same(dto, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetById_WhenFromAnotherUser_ShouldReturnNotFound()
    {
        // Arrange
        _service.GetByIdAsync(UserId, ImportId).ThrowsAsync(new KeyNotFoundException("Importação não encontrada."));

        // Act
        var result = await _controller.GetById(ImportId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // --- Confirm ---

    private static ConfirmInvoiceImportRequestDto ConfirmRequest() => new()
    {
        SelectedHolders = ["MARIA"],
        Items = [new ConfirmInvoiceImportItemRequestDto { Id = "a", Description = "Mercado", Amount = 10m, Date = new DateTime(2026, 10, 20) }]
    };

    [Fact]
    public async Task Confirm_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var dto = new CreditCardInvoiceImportResponseDto { Id = ImportId, Status = "Confirmed" };
        _service.ConfirmAsync(UserId, ImportId, Arg.Any<ConfirmInvoiceImportRequestDto>()).Returns(dto);

        // Act
        var result = await _controller.Confirm(ImportId, ConfirmRequest());

        // Assert
        Assert.Same(dto, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task Confirm_WithoutItems_ShouldReturnBadRequestWithoutCallingService()
    {
        // Act
        var result = await _controller.Confirm(ImportId, new ConfirmInvoiceImportRequestDto());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        await _service.DidNotReceive().ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ConfirmInvoiceImportRequestDto>());
    }

    [Fact]
    public async Task Confirm_WhenItemsAreInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        _service.ConfirmAsync(UserId, ImportId, Arg.Any<ConfirmInvoiceImportRequestDto>())
            .ThrowsAsync(new InvoiceImportValidationException("inválido", ["Mercado: informe a data."]));

        // Act
        var result = await _controller.Confirm(ImportId, ConfirmRequest());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Confirm_WhenAlreadyConfirmedOrSynced_ShouldReturnConflict()
    {
        // Arrange
        _service.ConfirmAsync(UserId, ImportId, Arg.Any<ConfirmInvoiceImportRequestDto>())
            .ThrowsAsync(new InvoiceImportConflictException(CreditCardInvoiceImportService.NotAwaitingReviewMessage));

        // Act
        var result = await _controller.Confirm(ImportId, ConfirmRequest());

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task Confirm_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _service.ConfirmAsync(UserId, ImportId, Arg.Any<ConfirmInvoiceImportRequestDto>())
            .ThrowsAsync(new KeyNotFoundException("Importação não encontrada."));

        // Act
        var result = await _controller.Confirm(ImportId, ConfirmRequest());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // --- Discard ---

    [Fact]
    public async Task Discard_WhenAllowed_ShouldReturnNoContent()
    {
        // Act
        var result = await _controller.Discard(ImportId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _service.Received(1).DiscardAsync(UserId, ImportId);
    }

    [Fact]
    public async Task Discard_WhenStateDoesNotAllow_ShouldReturnConflict()
    {
        // Arrange
        _service.DiscardAsync(UserId, ImportId)
            .ThrowsAsync(new InvoiceImportConflictException(CreditCardInvoiceImportService.CannotDiscardMessage));

        // Act
        var result = await _controller.Discard(ImportId);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task Discard_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _service.DiscardAsync(UserId, ImportId).ThrowsAsync(new KeyNotFoundException("Importação não encontrada."));

        // Act
        var result = await _controller.Discard(ImportId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
