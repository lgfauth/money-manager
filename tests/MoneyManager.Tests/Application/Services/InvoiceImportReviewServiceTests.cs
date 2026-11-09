using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace MoneyManager.Tests.Application.Services;

public class InvoiceImportReviewServiceTests
{
    private const string UserId = "user-1";
    private static readonly DateOnly Today = new(2026, 11, 9);

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRepository<Category> _categories = Substitute.For<IRepository<Category>>();
    private readonly ICreditCardInvoiceImportRepository _imports = Substitute.For<ICreditCardInvoiceImportRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly InvoiceImportReviewService _service;

    private readonly CreditCard _card = new() { Id = "card-1", UserId = UserId, ClosingDay = 2, BillingDueDay = 10 };

    public InvoiceImportReviewServiceTests()
    {
        _unitOfWork.Categories.Returns(_categories);
        _unitOfWork.CreditCardInvoiceImports.Returns(_imports);
        _unitOfWork.Users.Returns(_users);
        _categories.GetByUserIdAsync(UserId).Returns([]);
        _users.GetByIdAsync(UserId).Returns(new User { Id = UserId, Name = "Usuário Sem Fatura" });
        _service = new InvoiceImportReviewService(_unitOfWork);
    }

    // --- Helpers ---

    private static InvoiceDocumentDateDto D(int year, int month, int day) => new(day, month, year);
    private static InvoiceDocumentDateDto NoYear(int month, int day) => new(day, month, null);

    private static InvoiceDocumentItemDto Item(
        InvoiceDocumentDateDto? date,
        string description = "MERCADO",
        decimal? amount = 100m,
        int? installment = null,
        int? total = null,
        string? holder = null,
        InvoiceImportHolderType holderType = InvoiceImportHolderType.Unknown,
        string? card = null,
        CreditCardTransactionType type = CreditCardTransactionType.Purchase,
        string? category = null) =>
        new(date, description, amount, type, installment, total, holder, holderType, card,
            InvoiceImportCardType.Unknown, null, category, 0.9m, null);

    private static InvoiceDocumentAnalysisResultDto Analysis(
        InvoiceDocumentDateDto? closing,
        InvoiceDocumentDateDto? due,
        params InvoiceDocumentItemDto[] items) =>
        new(new InvoiceDocumentInfoDto("Banco X", "6720", closing, due, 500m, false), items, []);

    // Fatura padrão: fecha 02/11/2026 e vence 10/11/2026, igual ao cadastro do cartão
    private static InvoiceDocumentAnalysisResultDto StandardAnalysis(params InvoiceDocumentItemDto[] items) =>
        Analysis(D(2026, 11, 2), D(2026, 11, 10), items);

    private Task<InvoiceImportReviewResult> Build(InvoiceDocumentAnalysisResultDto analysis, DateOnly? today = null, CreditCard? card = null) =>
        _service.BuildReviewAsync(UserId, card ?? _card, analysis, today ?? Today);

    // --- Caminho feliz ---

    [Fact]
    public async Task BuildReviewAsync_WithClosedInvoice_ShouldBuildReview()
    {
        // Arrange
        var expense = new Category { Id = "cat-food", UserId = UserId, Name = "Alimentação", Type = CategoryType.Expense };
        var income = new Category { Id = "cat-income", UserId = UserId, Name = "Compras", Type = CategoryType.Income };
        _categories.GetByUserIdAsync(UserId).Returns([expense, income]);
        var analysis = StandardAnalysis(
            Item(D(2026, 10, 20), "MERCADO BOM", 150.5m, category: "alimentacao"),
            Item(D(2026, 10, 21), "LOJA", 80m, category: "Compras"));

        // Act
        var result = await Build(analysis);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.FailureMessage);
        Assert.Equal("2026-11", result.ReferenceMonth);
        Assert.Equal(new DateTime(2026, 11, 2), result.Invoice!.ClosingDate);
        Assert.False(result.Invoice.ClosingDateCalculated);
        Assert.Equal(new DateTime(2026, 11, 10), result.Invoice.DueDate);
        Assert.Equal("2026-11", result.Invoice.ReferenceMonth);
        Assert.Empty(result.Warnings);
        Assert.Empty(result.Holders);
        Assert.All(result.Items, i => Assert.True(i.IsSelectedByDefault));
        Assert.All(result.Items, i => Assert.Empty(i.Warnings));
        Assert.Equal("cat-food", result.Items[0].CategoryId);
        Assert.Null(result.Items[1].CategoryId); // categoria de receita não é associada
        Assert.Equal(new DateTime(2026, 10, 20), result.Items[0].Date);
    }

    [Fact]
    public async Task BuildReviewAsync_WithIssuesFromAi_ShouldAddInformationalWarnings()
    {
        // Arrange
        var analysis = StandardAnalysis(Item(D(2026, 10, 20))) with { Issues = ["Página 2 ilegível"] };

        // Act
        var result = await Build(analysis);

        // Assert
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(InvoiceImportWarningCode.UnreadableContent, warning.Code);
        Assert.Equal("Página 2 ilegível", warning.Message);
        Assert.False(warning.IsBlocking);
    }

    // --- Fatura fechada ---

    [Fact]
    public async Task BuildReviewAsync_WhenDocumentDeclaresOpenOrPartial_ShouldFail()
    {
        // Arrange
        var analysis = StandardAnalysis(Item(D(2026, 10, 20)));
        analysis = analysis with { Invoice = analysis.Invoice with { IsOpenOrPartial = true } };

        // Act
        var result = await Build(analysis);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InvoiceImportReviewService.NotClosedMessage, result.FailureMessage);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task BuildReviewAsync_WhenClosingIsAfterToday_ShouldFail()
    {
        // Arrange
        var analysis = Analysis(D(2026, 11, 10), D(2026, 11, 18), Item(D(2026, 11, 1)));

        // Act
        var result = await Build(analysis);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InvoiceImportReviewService.NotClosedMessage, result.FailureMessage);
    }

    [Fact]
    public async Task BuildReviewAsync_WhenClosingIsToday_ShouldSucceed()
    {
        // Arrange
        var card = new CreditCard { Id = "card-1", UserId = UserId, ClosingDay = 9, BillingDueDay = 17 };
        var analysis = Analysis(D(2026, 11, 9), D(2026, 11, 17), Item(D(2026, 11, 1)));

        // Act
        var result = await Build(analysis, card: card);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task BuildReviewAsync_WithoutClosingDate_ShouldCalculateFromDueDateAndWarn()
    {
        // Arrange — caso do .xlsx do Itaú: só vencimento
        var analysis = Analysis(null, D(2026, 11, 10), Item(D(2026, 10, 20)));

        // Act
        var result = await Build(analysis);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("2026-11", result.ReferenceMonth);
        Assert.Equal(new DateTime(2026, 11, 2), result.Invoice!.ClosingDate);
        Assert.True(result.Invoice.ClosingDateCalculated);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(InvoiceImportWarningCode.ClosingDateCalculated, warning.Code);
        Assert.False(warning.IsBlocking);
    }

    [Fact]
    public async Task BuildReviewAsync_WithoutClosingAndWithDueInNextMonth_ShouldUseCardSchedule()
    {
        // Arrange — cartão fecha dia 25 e vence dia 5 do mês seguinte
        var card = new CreditCard { Id = "card-1", UserId = UserId, ClosingDay = 25, BillingDueDay = 5 };
        var analysis = Analysis(null, D(2026, 11, 5), Item(D(2026, 10, 20)));

        // Act
        var result = await Build(analysis, card: card);

        // Assert
        Assert.Equal("2026-10", result.ReferenceMonth);
        Assert.Equal(new DateTime(2026, 10, 25), result.Invoice!.ClosingDate);
    }

    [Fact]
    public async Task BuildReviewAsync_WithoutClosingAndDue_ShouldUseLatestItemDate()
    {
        // Arrange
        var analysis = Analysis(null, null, Item(D(2026, 9, 28)), Item(D(2026, 10, 30)));

        // Act
        var result = await Build(analysis);

        // Assert — 30/10 com fechamento dia 2 pertence à fatura de novembro
        Assert.True(result.IsSuccess);
        Assert.Equal("2026-11", result.ReferenceMonth);
        Assert.True(result.Invoice!.ClosingDateCalculated);
    }

    [Fact]
    public async Task BuildReviewAsync_WithoutAnyDate_ShouldFailWithUnknownReferenceMonth()
    {
        // Arrange
        var analysis = Analysis(null, null, Item(null));

        // Act
        var result = await Build(analysis);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InvoiceImportReviewService.UnknownReferenceMonthMessage, result.FailureMessage);
    }

    [Fact]
    public async Task BuildReviewAsync_WithoutItems_ShouldFail()
    {
        // Act
        var result = await Build(StandardAnalysis());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InvoiceImportReviewService.NoItemsMessage, result.FailureMessage);
    }

    // --- Datas sem ano ---

    [Fact]
    public async Task BuildReviewAsync_WithItemDateWithoutYear_ShouldAssumeCurrentYear()
    {
        // Act
        var result = await Build(StandardAnalysis(Item(NoYear(10, 20))));

        // Assert
        Assert.Equal(new DateTime(2026, 10, 20), result.Items[0].Date);
    }

    [Fact]
    public async Task BuildReviewAsync_WhenDateWithoutYearFallsAfterClosing_ShouldUsePreviousYear()
    {
        // Arrange — virada de ano: hoje 05/01/2027, fatura fechou em 28/12/2026
        var card = new CreditCard { Id = "card-1", UserId = UserId, ClosingDay = 28, BillingDueDay = 5 };
        var analysis = Analysis(D(2026, 12, 28), D(2027, 1, 5), Item(NoYear(12, 20)), Item(NoYear(1, 2)));

        // Act
        var result = await Build(analysis, today: new DateOnly(2027, 1, 5), card: card);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("2026-12", result.ReferenceMonth);
        Assert.Equal(new DateTime(2026, 12, 20), result.Items[0].Date);
        // 02/01/2027 cairia depois do fechamento → 02/01/2026, fora dos 60 dias
        Assert.Equal(new DateTime(2026, 1, 2), result.Items[1].Date);
        Assert.Contains(InvoiceImportWarningCode.OutOfAllowedPeriod, result.Items[1].Warnings);
    }

    [Fact]
    public async Task BuildReviewAsync_WithClosingWithoutYear_ShouldResolveToLastOccurrence()
    {
        // Arrange — hoje 05/01/2027, fechamento "28/12" sem ano
        var card = new CreditCard { Id = "card-1", UserId = UserId, ClosingDay = 28, BillingDueDay = 5 };
        var analysis = Analysis(NoYear(12, 28), NoYear(1, 5), Item(NoYear(12, 20)));

        // Act
        var result = await Build(analysis, today: new DateOnly(2027, 1, 5), card: card);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTime(2026, 12, 28), result.Invoice!.ClosingDate);
        Assert.Equal(new DateTime(2027, 1, 5), result.Invoice.DueDate);
        Assert.Empty(result.Warnings);
    }

    // --- Regras de item ---

    [Fact]
    public async Task BuildReviewAsync_WithItemAfterClosingOrToday_ShouldBlockAsFutureDate()
    {
        // Act
        var result = await Build(StandardAnalysis(Item(D(2026, 11, 5)), Item(D(2026, 11, 20))));

        // Assert
        Assert.All(result.Items, i =>
        {
            Assert.Contains(InvoiceImportWarningCode.FutureDate, i.Warnings);
            Assert.False(i.IsSelectedByDefault);
        });
    }

    [Fact]
    public async Task BuildReviewAsync_With60DayRule_ShouldAllowBoundaryAndBlockOlderItems()
    {
        // Arrange — hoje 09/11/2026 − 60 dias = 10/09/2026
        var analysis = StandardAnalysis(
            Item(D(2026, 9, 10), "LIMITE"),
            Item(D(2026, 9, 9), "ANTIGA"),
            Item(D(2026, 9, 9), "PRIMEIRA PARCELA", installment: 1, total: 10),
            Item(D(2026, 3, 1), "PARCELA ANTIGA", installment: 9, total: 10));

        // Act
        var result = await Build(analysis);

        // Assert
        Assert.Empty(result.Items[0].Warnings);
        Assert.True(result.Items[0].IsSelectedByDefault);
        Assert.Contains(InvoiceImportWarningCode.OutOfAllowedPeriod, result.Items[1].Warnings);
        Assert.False(result.Items[1].IsSelectedByDefault);
        Assert.Contains(InvoiceImportWarningCode.OutOfAllowedPeriod, result.Items[2].Warnings);
        // Parcela X > 1 é isenta dos 60 dias (data da compra original)
        Assert.Empty(result.Items[3].Warnings);
        Assert.Equal(9, result.Items[3].InstallmentNumber);
        Assert.Equal(10, result.Items[3].TotalInstallments);
    }

    [Fact]
    public async Task BuildReviewAsync_WithInvalidFields_ShouldBlockAndDeselect()
    {
        // Arrange
        var analysis = StandardAnalysis(
            Item(null, "SEM DATA"),
            Item(D(2026, 10, 20), "  "),
            Item(D(2026, 10, 20), "VALOR ZERO", amount: 0m),
            Item(D(2026, 10, 20), "SEM VALOR", amount: null));

        // Act
        var result = await Build(analysis);

        // Assert
        Assert.Equal([InvoiceImportWarningCode.MissingDate], result.Items[0].Warnings);
        Assert.Equal([InvoiceImportWarningCode.MissingDescription], result.Items[1].Warnings);
        Assert.Equal([InvoiceImportWarningCode.InvalidAmount], result.Items[2].Warnings);
        Assert.Equal([InvoiceImportWarningCode.InvalidAmount], result.Items[3].Warnings);
        Assert.All(result.Items, i => Assert.False(i.IsSelectedByDefault));
    }

    [Fact]
    public async Task BuildReviewAsync_WithSingleInstallmentInfo_ShouldTreatAsCashPurchase()
    {
        // Act
        var result = await Build(StandardAnalysis(Item(D(2026, 10, 20), installment: 1, total: 1)));

        // Assert
        Assert.Null(result.Items[0].InstallmentNumber);
        Assert.Null(result.Items[0].TotalInstallments);
    }

    [Fact]
    public void ValidateItem_WithCorrectedItem_ShouldReturnNoWarnings()
    {
        // Arrange — item editado pelo usuário na revisão (uso na confirmação)
        var item = new InvoiceImportItem { Date = new DateTime(2026, 10, 1), Description = "CORRIGIDO", Amount = 10m };

        // Act
        var codes = _service.ValidateItem(item, new DateTime(2026, 11, 2), Today);

        // Assert
        Assert.Empty(codes);
    }

    [Theory]
    [InlineData(InvoiceImportWarningCode.MissingDate, true)]
    [InlineData(InvoiceImportWarningCode.FutureDate, true)]
    [InlineData(InvoiceImportWarningCode.OutOfAllowedPeriod, true)]
    [InlineData(InvoiceImportWarningCode.AlreadyImported, true)]
    [InlineData(InvoiceImportWarningCode.NotFoundInSpreadsheet, true)]
    [InlineData(InvoiceImportWarningCode.PossibleDuplicate, false)]
    [InlineData(InvoiceImportWarningCode.PreviousInstallmentsNotRegistered, false)]
    public void IsBlocking_ShouldClassifyItemCodes(InvoiceImportWarningCode code, bool expected)
    {
        Assert.Equal(expected, InvoiceImportReviewService.IsBlocking(code));
    }

    // --- Conferência com o cadastro do cartão ---

    [Theory]
    [InlineData(2, 10, 0)]   // igual ao cadastro
    [InlineData(5, 10, 1)]   // fechamento 3 dias depois → informativo
    [InlineData(6, 10, 1)]   // fechamento 4 dias depois → bloqueante
    public async Task BuildReviewAsync_ShouldCompareClosingDayWithCard(int docClosingDay, int docDueDay, int expectedWarnings)
    {
        // Arrange — cadastro: fechamento dia 2, vencimento dia 10
        var analysis = Analysis(D(2026, 11, docClosingDay), D(2026, 11, docDueDay), Item(D(2026, 10, 20)));

        // Act
        var result = await Build(analysis);

        // Assert
        var mismatches = result.Warnings.Where(w => w.Code == InvoiceImportWarningCode.ClosingDayMismatch).ToList();
        Assert.Equal(expectedWarnings, mismatches.Count);
        if (docClosingDay == 6)
        {
            Assert.True(mismatches[0].IsBlocking);
            Assert.Contains("dia 6", mismatches[0].Message);
            Assert.Contains("dia 2", mismatches[0].Message);
            Assert.Contains("ajuste o cadastro", mismatches[0].Message);
        }
        else if (docClosingDay == 5)
        {
            Assert.False(mismatches[0].IsBlocking);
        }
    }

    [Fact]
    public async Task BuildReviewAsync_WithDueDayMismatchAboveTolerance_ShouldAddBlockingWarning()
    {
        // Arrange
        var analysis = Analysis(D(2026, 11, 2), D(2026, 11, 15), Item(D(2026, 10, 20)));

        // Act
        var result = await Build(analysis);

        // Assert
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(InvoiceImportWarningCode.DueDayMismatch, warning.Code);
        Assert.True(warning.IsBlocking);
        Assert.Contains("vencimento", warning.Message);
    }

    [Fact]
    public async Task BuildReviewAsync_WithClosingPostponedToNextMonth_ShouldKeepReferenceMonthAndOnlyInform()
    {
        // Arrange — cartão fecha dia 30; em 2026-10-31 (sáb) → banco fechou 02/11
        var card = new CreditCard { Id = "card-1", UserId = UserId, ClosingDay = 31, BillingDueDay = 8 };
        var analysis = Analysis(D(2026, 11, 2), null, Item(D(2026, 10, 20)));

        // Act
        var result = await Build(analysis, card: card);

        // Assert
        Assert.Equal("2026-10", result.ReferenceMonth);
        var warning = Assert.Single(result.Warnings);
        Assert.False(warning.IsBlocking);
    }

    [Fact]
    public async Task BuildReviewAsync_WithDay31CardInShortMonth_ShouldNotWarn()
    {
        // Arrange — cartão fecha dia 31; em fevereiro o fechamento cai no dia 28
        var card = new CreditCard { Id = "card-1", UserId = UserId, ClosingDay = 31, BillingDueDay = 10 };
        var analysis = Analysis(D(2026, 2, 28), D(2026, 3, 10), Item(D(2026, 2, 20)));

        // Act
        var result = await Build(analysis, today: new DateOnly(2026, 3, 1), card: card);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("2026-02", result.ReferenceMonth);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void CheckCardSchedule_AfterCardAdjustment_ShouldNoLongerBlock()
    {
        // Arrange — revalidação na confirmação contra o cadastro atual
        var invoice = new InvoiceImportDocumentInfo
        {
            ClosingDate = new DateTime(2026, 11, 8),
            ReferenceMonth = "2026-11",
            DueDate = new DateTime(2026, 11, 15)
        };
        var wrongCard = new CreditCard { ClosingDay = 2, BillingDueDay = 10 };
        var adjustedCard = new CreditCard { ClosingDay = 8, BillingDueDay = 15 };

        // Act
        var before = _service.CheckCardSchedule(invoice, wrongCard);
        var after = _service.CheckCardSchedule(invoice, adjustedCard);

        // Assert
        Assert.Equal(2, before.Count(w => w.IsBlocking));
        Assert.Empty(after);
    }

    [Fact]
    public void CheckCardSchedule_WithCalculatedClosing_ShouldSkipClosingComparison()
    {
        // Arrange
        var invoice = new InvoiceImportDocumentInfo
        {
            ClosingDate = new DateTime(2026, 11, 2),
            ClosingDateCalculated = true,
            ReferenceMonth = "2026-11"
        };

        // Act
        var warnings = _service.CheckCardSchedule(invoice, new CreditCard { ClosingDay = 20, BillingDueDay = 28 });

        // Assert
        Assert.Empty(warnings);
    }

    // --- Titulares ---

    [Fact]
    public async Task BuildReviewAsync_WithSeveralHolders_ShouldGroupByNormalizedNameAndPrefix()
    {
        // Arrange — nome cortado em 20 caracteres e com/sem acento
        var analysis = StandardAnalysis(
            Item(D(2026, 10, 20), amount: 100m, holder: "JOÃO DA SILVA SANTOS", holderType: InvoiceImportHolderType.Primary, card: "6720"),
            Item(D(2026, 10, 21), amount: 50m, holder: "Joao da Silva Santos Filho", holderType: InvoiceImportHolderType.Primary, card: "1111"),
            Item(D(2026, 10, 22), amount: 30m, holder: "joao  da silva santos", type: CreditCardTransactionType.Refund, card: "6720"),
            Item(D(2026, 10, 23), amount: 70m, holder: "MARIA DA SILVA", holderType: InvoiceImportHolderType.Additional, card: "2222"));

        // Act
        var result = await Build(analysis);

        // Assert
        Assert.Equal(2, result.Holders.Count);
        var joao = result.Holders.Single(h => h.Name == "Joao da Silva Santos Filho");
        Assert.Equal(InvoiceImportHolderType.Primary, joao.Type);
        Assert.Equal(3, joao.ItemCount);
        Assert.Equal(120m, joao.TotalAmount); // 100 + 50 − 30 (estorno)
        Assert.Equal(["1111", "6720"], joao.CardLastDigits);
        var maria = result.Holders.Single(h => h.Name == "MARIA DA SILVA");
        Assert.Equal(InvoiceImportHolderType.Additional, maria.Type);
        Assert.Equal(1, maria.ItemCount);
        // Itens padronizados com o nome do grupo
        Assert.Equal(3, result.Items.Count(i => i.HolderName == "Joao da Silva Santos Filho"));
    }

    [Fact]
    public async Task BuildReviewAsync_WithoutHolderInformation_ShouldUseImplicitHolder()
    {
        // Act
        var result = await Build(StandardAnalysis(Item(D(2026, 10, 20)), Item(D(2026, 10, 21))));

        // Assert
        Assert.Empty(result.Holders);
        Assert.All(result.Items, i => Assert.Null(i.HolderName));
    }

    [Fact]
    public async Task BuildReviewAsync_WithSomeItemsWithoutHolder_ShouldGroupThemAsUnidentified()
    {
        // Act
        var result = await Build(StandardAnalysis(Item(D(2026, 10, 20), holder: "ANA"), Item(D(2026, 10, 21))));

        // Assert
        Assert.Equal(2, result.Holders.Count);
        var unidentified = result.Holders.Single(h => h.Name == InvoiceImportReviewService.UnidentifiedHolderName);
        Assert.Equal(1, unidentified.ItemCount);
        Assert.False(unidentified.IsSuggested);
    }

    [Fact]
    public async Task BuildReviewAsync_ShouldPreferHoldersFromLastConfirmedImport()
    {
        // Arrange
        _imports.GetLastConfirmedByCardAsync(UserId, "card-1")
            .Returns(new CreditCardInvoiceImport { SelectedHolders = ["MARIA DA SILVA"] });
        _users.GetByIdAsync(UserId).Returns(new User { Id = UserId, FullName = "João da Silva" });
        var analysis = StandardAnalysis(
            Item(D(2026, 10, 20), holder: "JOAO DA SILVA"),
            Item(D(2026, 10, 21), holder: "MARIA DA SILVA"));

        // Act
        var result = await Build(analysis);

        // Assert
        Assert.True(result.Holders.Single(h => h.Name == "MARIA DA SILVA").IsSuggested);
        Assert.False(result.Holders.Single(h => h.Name == "JOAO DA SILVA").IsSuggested);
    }

    [Fact]
    public async Task BuildReviewAsync_WithoutPreviousImport_ShouldSuggestHolderMatchingUserFullName()
    {
        // Arrange — nome cortado pelo banco é prefixo do nome completo do usuário
        _users.GetByIdAsync(UserId).Returns(new User { Id = UserId, Name = "Jo", FullName = "João da Silva Santos Pereira" });
        var analysis = StandardAnalysis(
            Item(D(2026, 10, 20), holder: "JOAO DA SILVA SANTOS"),
            Item(D(2026, 10, 21), holder: "MARIA DA SILVA"));

        // Act
        var result = await Build(analysis);

        // Assert
        Assert.True(result.Holders.Single(h => h.Name == "JOAO DA SILVA SANTOS").IsSuggested);
        Assert.False(result.Holders.Single(h => h.Name == "MARIA DA SILVA").IsSuggested);
    }

    [Fact]
    public void ApplyHolderSuggestion_WithEmptyFullName_ShouldUseUserName()
    {
        // Arrange — o serviço passa Name quando FullName está vazio
        var holders = new List<InvoiceImportHolder> { new() { Name = "ANA" }, new() { Name = "CARLOS" } };

        // Act
        InvoiceImportReviewService.ApplyHolderSuggestion(holders, [], "Carlos");

        // Assert
        Assert.False(holders[0].IsSuggested);
        Assert.True(holders[1].IsSuggested);
    }

    [Fact]
    public async Task BuildReviewAsync_WithSingleHolderNotMatchingUser_ShouldSuggestIt()
    {
        // Act
        var result = await Build(StandardAnalysis(Item(D(2026, 10, 20), holder: "FULANO")));

        // Assert
        Assert.True(Assert.Single(result.Holders).IsSuggested);
    }

    [Fact]
    public void ApplyHolderSuggestion_WithSeveralUnmatchedHolders_ShouldSuggestNone()
    {
        // Arrange
        var holders = new List<InvoiceImportHolder> { new() { Name = "ANA" }, new() { Name = "BRUNO" } };

        // Act
        InvoiceImportReviewService.ApplyHolderSuggestion(holders, ["PEDRO"], "Carla");

        // Assert
        Assert.All(holders, h => Assert.False(h.IsSuggested));
    }
}
