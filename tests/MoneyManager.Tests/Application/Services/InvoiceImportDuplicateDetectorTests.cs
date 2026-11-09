using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using Xunit;

namespace MoneyManager.Tests.Application.Services;

public class InvoiceImportDuplicateDetectorTests
{
    private const string InvoiceId = "invoice-2026-11";

    private static InvoiceImportItem Item(
        int day,
        decimal amount,
        string description = "MERCADO",
        int? fileIndex = null,
        int? installment = null,
        int? total = null,
        string? holder = null,
        CreditCardTransactionType type = CreditCardTransactionType.Purchase,
        int month = 10) => new()
    {
        Date = new DateTime(2026, month, day),
        Amount = amount,
        Description = description,
        Type = type,
        InstallmentNumber = installment,
        TotalInstallments = total,
        HolderName = holder,
        Source = fileIndex is null ? null : new InvoiceImportSourceReference { FileIndex = fileIndex }
    };

    private static CreditCardTransaction Tx(
        int day,
        decimal amount,
        string description = "MERCADO",
        int installment = 1,
        int total = 1,
        string invoiceId = "outra",
        CreditCardTransactionType type = CreditCardTransactionType.Purchase,
        string? fingerprint = null,
        string? parentId = null,
        int month = 10) => new()
    {
        PurchaseDate = new DateTime(2026, month, day),
        TotalAmount = amount * total,
        InstallmentAmount = type == CreditCardTransactionType.Refund ? -amount : amount,
        Description = description,
        InstallmentNumber = installment,
        TotalInstallments = total,
        InvoiceId = invoiceId,
        Type = type,
        ImportFingerprint = fingerprint,
        ParentTransactionId = parentId
    };

    private static void Apply(List<InvoiceImportItem> items, List<CreditCardTransaction> existing, Func<InvoiceImportItem, bool>? include = null)
    {
        InvoiceImportDuplicateDetector.AssignFingerprints("card-1", "2026-11", items);
        InvoiceImportDuplicateDetector.Apply(items, include ?? (_ => true), existing, InvoiceId);
    }

    // --- Prints sobrepostos ---

    [Fact]
    public void MergeOverlappingScreenshots_WithSameItemInTwoImages_ShouldKeepOne()
    {
        // Arrange
        var items = new List<InvoiceImportItem>
        {
            Item(20, 50m, "Padaria Pão", fileIndex: 0),
            Item(21, 30m, "Farmácia", fileIndex: 0),
            Item(20, 50m, "PADARIA PAO", fileIndex: 1),
            Item(22, 70m, "Posto", fileIndex: 1)
        };

        // Act
        var result = InvoiceImportDuplicateDetector.MergeOverlappingScreenshots(items);

        // Assert
        Assert.Equal(["Padaria Pão", "Farmácia", "Posto"], result.Select(i => i.Description));
    }

    [Fact]
    public void MergeOverlappingScreenshots_WithIdenticalItemsInSameImage_ShouldKeepThem()
    {
        // Arrange — duas compras iguais na imagem 0; a imagem 1 repete uma delas
        var items = new List<InvoiceImportItem>
        {
            Item(20, 5m, "CAFE", fileIndex: 0),
            Item(20, 5m, "CAFE", fileIndex: 0),
            Item(20, 5m, "CAFE", fileIndex: 1)
        };

        // Act
        var result = InvoiceImportDuplicateDetector.MergeOverlappingScreenshots(items);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void MergeOverlappingScreenshots_WithPdfOrSpreadsheetItems_ShouldKeepAll()
    {
        // Arrange — sem índice de arquivo (mesmo documento)
        var items = new List<InvoiceImportItem> { Item(20, 5m, "CAFE"), Item(20, 5m, "CAFE") };

        // Act
        var result = InvoiceImportDuplicateDetector.MergeOverlappingScreenshots(items);

        // Assert
        Assert.Equal(2, result.Count);
    }

    // --- Fingerprint ---

    [Fact]
    public void AssignFingerprints_ShouldBeStableAndIgnoreFormattingDifferences()
    {
        // Arrange
        var first = new List<InvoiceImportItem> { Item(20, 99.9m, "Loja Ótima 03/10", installment: 3, total: 10) };
        var second = new List<InvoiceImportItem> { Item(20, 99.90m, "LOJA OTIMA - PARCELA 3 DE 10", installment: 3, total: 10) };

        // Act
        InvoiceImportDuplicateDetector.AssignFingerprints("card-1", "2026-11", first);
        InvoiceImportDuplicateDetector.AssignFingerprints("card-1", "2026-11", second);

        // Assert
        Assert.NotNull(first[0].Fingerprint);
        Assert.Equal(64, first[0].Fingerprint!.Length);
        Assert.Equal(first[0].Fingerprint, second[0].Fingerprint);
    }

    [Fact]
    public void AssignFingerprints_ShouldDifferByCardMonthAmountInstallmentAndOrdinal()
    {
        // Arrange
        List<InvoiceImportItem> Make() => [Item(20, 10m), Item(20, 10m), Item(20, 11m), Item(20, 10m, installment: 2, total: 3)];
        var a = Make();
        var otherCard = Make();
        var otherMonth = Make();

        // Act
        InvoiceImportDuplicateDetector.AssignFingerprints("card-1", "2026-11", a);
        InvoiceImportDuplicateDetector.AssignFingerprints("card-2", "2026-11", otherCard);
        InvoiceImportDuplicateDetector.AssignFingerprints("card-1", "2026-12", otherMonth);

        // Assert
        Assert.Equal(4, a.Select(i => i.Fingerprint).Distinct().Count()); // ordinal separa os dois itens idênticos
        Assert.NotEqual(a[0].Fingerprint, otherCard[0].Fingerprint);
        Assert.NotEqual(a[0].Fingerprint, otherMonth[0].Fingerprint);
    }

    // --- Correspondência com transações existentes ---

    [Fact]
    public void Apply_WithTwoIdenticalItemsAndOneExisting_ShouldFlagOnlyOne()
    {
        // Arrange
        var items = new List<InvoiceImportItem> { Item(20, 5m, "CAFE"), Item(20, 5m, "CAFE") };

        // Act
        Apply(items, [Tx(20, 5m, "Café")]);

        // Assert
        Assert.Single(items, i => i.Warnings.Contains(InvoiceImportWarningCode.PossibleDuplicate));
        Assert.Single(items, i => i.IsSelectedByDefault && i.Warnings.Count == 0);
    }

    [Theory]
    [InlineData(19, true)]
    [InlineData(21, true)]
    [InlineData(22, false)]
    public void Apply_ShouldMatchWithOneDayTolerance(int existingDay, bool expectedDuplicate)
    {
        // Arrange
        var items = new List<InvoiceImportItem> { Item(20, 42m) };

        // Act
        Apply(items, [Tx(existingDay, 42m)]);

        // Assert
        Assert.Equal(expectedDuplicate, items[0].Warnings.Contains(InvoiceImportWarningCode.PossibleDuplicate));
        Assert.Equal(!expectedDuplicate, items[0].IsSelectedByDefault);
    }

    [Fact]
    public void Apply_WithDifferentAmountOrType_ShouldNotMatch()
    {
        // Arrange
        var items = new List<InvoiceImportItem>
        {
            Item(20, 42.01m),
            Item(20, 42m, type: CreditCardTransactionType.Refund)
        };

        // Act
        Apply(items, [Tx(20, 42m)]);

        // Assert
        Assert.All(items, i => Assert.Empty(i.Warnings));
    }

    [Fact]
    public void Apply_ShouldUseDescriptionAsTieBreaker()
    {
        // Arrange — mesmo valor e data; a transação existente é do iFood
        var items = new List<InvoiceImportItem> { Item(20, 35m, "UBER TRIP"), Item(20, 35m, "IFD*RESTAURANTE SABOR") };

        // Act
        Apply(items, [Tx(20, 35m, "Restaurante Sabor")]);

        // Assert
        Assert.Empty(items[0].Warnings);
        Assert.Equal([InvoiceImportWarningCode.PossibleDuplicate], items[1].Warnings);
    }

    [Fact]
    public void Apply_WithExistingRefundStoredAsNegative_ShouldMatchRefundItem()
    {
        // Arrange
        var items = new List<InvoiceImportItem> { Item(20, 30m, "ESTORNO LOJA", type: CreditCardTransactionType.Refund) };

        // Act
        Apply(items, [Tx(20, 30m, "Estorno loja", type: CreditCardTransactionType.Refund)]);

        // Assert
        Assert.Equal([InvoiceImportWarningCode.PossibleDuplicate], items[0].Warnings);
    }

    [Fact]
    public void Apply_WithBlockedItem_ShouldNotMatchNorConsumeTransaction()
    {
        // Arrange — o primeiro item está bloqueado (data futura) e não deve "consumir" a transação
        var blocked = Item(20, 5m);
        blocked.Warnings.Add(InvoiceImportWarningCode.FutureDate);
        var items = new List<InvoiceImportItem> { blocked, Item(20, 5m) };

        // Act
        Apply(items, [Tx(20, 5m)]);

        // Assert
        Assert.Equal([InvoiceImportWarningCode.FutureDate], items[0].Warnings);
        Assert.False(items[0].IsSelectedByDefault);
        Assert.Equal([InvoiceImportWarningCode.PossibleDuplicate], items[1].Warnings);
    }

    // --- Reimportação ---

    [Fact]
    public void Apply_WithFingerprintAlreadyImported_ShouldBlockItemAndConsumeTransaction()
    {
        // Arrange — mesma fatura importada antes: a transação gravou o fingerprint
        var previous = new List<InvoiceImportItem> { Item(20, 5m, "CAFE") };
        InvoiceImportDuplicateDetector.AssignFingerprints("card-1", "2026-11", previous);
        var existing = new List<CreditCardTransaction> { Tx(20, 5m, "CAFE", fingerprint: previous[0].Fingerprint) };
        var items = new List<InvoiceImportItem> { Item(20, 5m, "Café"), Item(20, 5m, "Café") };

        // Act
        Apply(items, existing);

        // Assert
        Assert.Equal([InvoiceImportWarningCode.AlreadyImported], items[0].Warnings);
        Assert.False(items[0].IsSelectedByDefault);
        Assert.True(InvoiceImportReviewService.IsBlocking(InvoiceImportWarningCode.AlreadyImported));
        // A transação já foi "consumida" pelo item importado; o segundo é novo
        Assert.Empty(items[1].Warnings);
        Assert.True(items[1].IsSelectedByDefault);
    }

    // --- Parcelamentos ---

    [Fact]
    public void Apply_WithFirstInstallmentAlreadyRegistered_ShouldMarkAlreadyRegistered()
    {
        // Arrange — compra de 3x de 100 cadastrada (registros 1..3 do mesmo parcelamento)
        var existing = new List<CreditCardTransaction>
        {
            Tx(20, 100m, "TV", installment: 1, total: 3) ,
            Tx(20, 100m, "TV", installment: 2, total: 3, parentId: "p"),
            Tx(20, 100m, "TV", installment: 3, total: 3, parentId: "p")
        };
        existing[0].Id = "p";
        var items = new List<InvoiceImportItem> { Item(20, 100m, "TV 1/3", installment: 1, total: 3), Item(20, 100m, "TV 1/3", installment: 1, total: 3) };

        // Act
        Apply(items, existing);

        // Assert — só um item corresponde ao parcelamento cadastrado
        Assert.Equal([InvoiceImportWarningCode.AlreadyRegistered], items[0].Warnings);
        Assert.False(items[0].IsSelectedByDefault);
        Assert.Empty(items[1].Warnings);
        Assert.True(items[1].IsSelectedByDefault);
    }

    [Fact]
    public void Apply_WithNewFirstInstallment_ShouldKeepItSelected()
    {
        // Arrange — existe parcelamento com outro número de parcelas
        var items = new List<InvoiceImportItem> { Item(20, 100m, installment: 1, total: 3) };

        // Act
        Apply(items, [Tx(20, 100m, installment: 1, total: 4)]);

        // Assert
        Assert.Empty(items[0].Warnings);
        Assert.True(items[0].IsSelectedByDefault);
    }

    [Fact]
    public void Apply_WithLaterInstallmentInImportedInvoice_ShouldMarkAlreadyRegistered()
    {
        // Arrange — parcela 3/10 já está na fatura de destino (valor com centavo de arredondamento)
        var items = new List<InvoiceImportItem> { Item(5, 99.90m, "LOJA", installment: 3, total: 10, month: 8) };

        // Act
        Apply(items, [Tx(5, 99.91m, "LOJA", installment: 3, total: 10, invoiceId: InvoiceId, month: 8)]);

        // Assert
        Assert.Equal([InvoiceImportWarningCode.AlreadyRegistered], items[0].Warnings);
        Assert.False(items[0].IsSelectedByDefault);
    }

    [Fact]
    public void Apply_WithLaterInstallmentNotRegistered_ShouldWarnAndDeselect()
    {
        // Arrange — a parcela 3/10 existe, mas em outra fatura (não na importada)
        var items = new List<InvoiceImportItem> { Item(5, 99.90m, "LOJA", installment: 3, total: 10, month: 8) };

        // Act
        Apply(items, [Tx(5, 99.90m, "LOJA", installment: 3, total: 10, invoiceId: "outra", month: 8)]);

        // Assert
        Assert.Equal([InvoiceImportWarningCode.PreviousInstallmentsNotRegistered], items[0].Warnings);
        Assert.False(items[0].IsSelectedByDefault);
        Assert.False(InvoiceImportReviewService.IsBlocking(InvoiceImportWarningCode.PreviousInstallmentsNotRegistered));
    }

    [Fact]
    public void Apply_WithoutImportedInvoice_ShouldWarnLaterInstallment()
    {
        // Arrange
        var items = new List<InvoiceImportItem> { Item(5, 50m, installment: 2, total: 5, month: 9) };
        InvoiceImportDuplicateDetector.AssignFingerprints("card-1", "2026-11", items);

        // Act
        InvoiceImportDuplicateDetector.Apply(items, _ => true, [], importedInvoiceId: null);

        // Assert
        Assert.Equal([InvoiceImportWarningCode.PreviousInstallmentsNotRegistered], items[0].Warnings);
    }

    // --- Titulares e reaplicação ---

    [Fact]
    public void Apply_WithExcludedHolder_ShouldNotFlagNorConsume()
    {
        // Arrange — item da adicional (não escolhida) vem primeiro e não pode "roubar" a transação
        var items = new List<InvoiceImportItem> { Item(20, 5m, holder: "MARIA"), Item(20, 5m, holder: "JOAO") };

        // Act
        Apply(items, [Tx(20, 5m)], include: i => i.HolderName == "JOAO");

        // Assert
        Assert.Empty(items[0].Warnings);
        Assert.Equal([InvoiceImportWarningCode.PossibleDuplicate], items[1].Warnings);
    }

    [Fact]
    public void Apply_WhenReapplied_ShouldNotDuplicateWarnings()
    {
        // Arrange
        var items = new List<InvoiceImportItem> { Item(20, 5m) };
        var existing = new List<CreditCardTransaction> { Tx(20, 5m) };

        // Act
        Apply(items, existing);
        Apply(items, existing);

        // Assert
        Assert.Equal([InvoiceImportWarningCode.PossibleDuplicate], items[0].Warnings);
    }

    // --- Normalização de descrição ---

    [Theory]
    [InlineData("IFD*Restaurante Sabor", "RESTAURANTE SABOR")]
    [InlineData("PAG*Loja.Exemplo", "LOJA EXEMPLO")]
    [InlineData("Loja Ótima 03/10", "LOJA OTIMA")]
    [InlineData("LOJA OTIMA - PARCELA 3 DE 10", "LOJA OTIMA")]
    [InlineData("MERCADOPAGO   Feira Livre", "FEIRA LIVRE")]
    [InlineData("Posto Shell, 12", "POSTO SHELL 12")]
    public void NormalizeDescription_ShouldRemoveNoise(string input, string expected)
    {
        Assert.Equal(expected, InvoiceImportText.NormalizeDescription(input));
    }
}
