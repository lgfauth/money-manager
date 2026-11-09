using System.IO.Compression;
using System.Text;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Exceptions;
using MoneyManager.Infrastructure.Services.Spreadsheets;
using Xunit;

namespace MoneyManager.Tests.Infrastructure.Services;

/// <summary>
/// Planilhas sintéticas montadas no teste (nunca o arquivo real de <c>docs/</c>).
/// </summary>
public class XlsxTextExtractorTests
{
    private const string MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string SheetMain = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml";

    private readonly XlsxTextExtractor _extractor = new();

    /// <summary>Montador mínimo de .xlsx: abas com XML de sheetData, sharedStrings e styles opcionais.</summary>
    private sealed class XlsxBuilder
    {
        private readonly List<(string Name, string SheetData, string? State)> _sheets = [];
        private readonly Dictionary<string, byte[]> _extraParts = [];
        public List<string> SharedStrings { get; } = [];
        public string? StylesXml { get; set; }
        public bool Date1904 { get; set; }
        public string MainContentType { get; set; } = SheetMain;

        public XlsxBuilder Sheet(string name, string sheetData, string? state = null)
        {
            _sheets.Add((name, sheetData, state));
            return this;
        }

        public XlsxBuilder Part(string path, string content)
        {
            _extraParts[path] = Encoding.UTF8.GetBytes(content);
            return this;
        }

        public byte[] Build()
        {
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                void Add(string path, string content) => Write(zip, path, Encoding.UTF8.GetBytes(content));

                var overrides = new StringBuilder($"<Override PartName=\"/xl/workbook.xml\" ContentType=\"{MainContentType}\"/>");
                Add("[Content_Types].xml",
                    $"<?xml version=\"1.0\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">{overrides}</Types>");

                var sheetsXml = new StringBuilder();
                var rels = new StringBuilder();
                for (var i = 0; i < _sheets.Count; i++)
                {
                    var (name, data, state) = _sheets[i];
                    var stateAttr = state is null ? "" : $" state=\"{state}\"";
                    sheetsXml.Append($"<sheet name=\"{name}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"{stateAttr}/>");
                    rels.Append($"<Relationship Id=\"rId{i + 1}\" Type=\"{RelNs}/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>");
                    Add($"xl/worksheets/sheet{i + 1}.xml",
                        $"<?xml version=\"1.0\"?><worksheet xmlns=\"{MainNs}\"><sheetData>{data}</sheetData></worksheet>");
                }

                rels.Append($"<Relationship Id=\"rIdS\" Type=\"{RelNs}/sharedStrings\" Target=\"sharedStrings.xml\"/>");
                rels.Append($"<Relationship Id=\"rIdT\" Type=\"{RelNs}/styles\" Target=\"styles.xml\"/>");

                var date1904 = Date1904 ? "<workbookPr date1904=\"1\"/>" : "";
                Add("xl/workbook.xml",
                    $"<?xml version=\"1.0\"?><workbook xmlns=\"{MainNs}\" xmlns:r=\"{RelNs}\">{date1904}<sheets>{sheetsXml}</sheets></workbook>");
                Add("xl/_rels/workbook.xml.rels",
                    $"<?xml version=\"1.0\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">{rels}</Relationships>");

                var si = string.Concat(SharedStrings.Select(s => $"<si><t>{System.Security.SecurityElement.Escape(s)}</t></si>"));
                Add("xl/sharedStrings.xml", $"<?xml version=\"1.0\"?><sst xmlns=\"{MainNs}\">{si}</sst>");
                Add("xl/styles.xml", StylesXml ?? $"<?xml version=\"1.0\"?><styleSheet xmlns=\"{MainNs}\"><cellXfs><xf numFmtId=\"0\"/></cellXfs></styleSheet>");

                foreach (var (path, content) in _extraParts)
                    Write(zip, path, content);
            }
            return ms.ToArray();
        }

        private static void Write(ZipArchive zip, string path, byte[] content)
        {
            using var stream = zip.CreateEntry(path, CompressionLevel.Optimal).Open();
            stream.Write(content);
        }
    }

    // Estilos: 0 = geral, 1 = data embutida (14), 2 = data personalizada "dd/mm/yyyy", 3 = moeda personalizada
    private static readonly string DateStyles =
        $"<?xml version=\"1.0\"?><styleSheet xmlns=\"{MainNs}\">" +
        "<numFmts><numFmt numFmtId=\"164\" formatCode=\"dd/mm/yyyy\"/><numFmt numFmtId=\"165\" formatCode=\"&quot;R$&quot;\\ #,##0.00\"/></numFmts>" +
        "<cellXfs><xf numFmtId=\"0\"/><xf numFmtId=\"14\"/><xf numFmtId=\"164\"/><xf numFmtId=\"165\"/></cellXfs></styleSheet>";

    private static string Cell(string reference, string value, string? type = null, int? style = null, string? formula = null)
    {
        var t = type is null ? "" : $" t=\"{type}\"";
        var s = style is null ? "" : $" s=\"{style}\"";
        var f = formula is null ? "" : $"<f>{formula}</f>";
        return $"<c r=\"{reference}\"{t}{s}>{f}<v>{value}</v></c>";
    }

    private static string Row(int number, params string[] cells) => $"<row r=\"{number}\">{string.Concat(cells)}</row>";

    [Fact]
    public void Extract_ShouldConvertSharedStringsNumbersDatesAndKeepRowNumbers()
    {
        // Arrange
        var builder = new XlsxBuilder { StylesXml = DateStyles };
        builder.SharedStrings.AddRange(["Data", "Lançamento", "Valor", "MERCADO X"]);
        builder.Sheet("Lançamentos",
            Row(1, Cell("A1", "0", "s"), Cell("B1", "1", "s"), Cell("C1", "2", "s")) +
            Row(7, Cell("A7", "46298", style: 1), Cell("B7", "3", "s"), Cell("C7", "1234.5", style: 3)) +
            Row(8, Cell("A8", "46299", style: 2), "<c r=\"B8\" t=\"inlineStr\"><is><t>PADARIA</t></is></c>", Cell("C8", "-0.1")));

        // Act
        var result = _extractor.Extract(builder.Build());

        // Assert
        var sheet = Assert.Single(result.Sheets);
        Assert.Equal("Lançamentos", sheet.Name);
        Assert.Equal([1, 7, 8], sheet.Rows.Select(r => r.Number));
        Assert.Equal(["2026-10-03", "MERCADO X", "1234.5"], sheet.Rows[1].Cells);
        Assert.Equal(["2026-10-04", "PADARIA", "-0.1"], sheet.Rows[2].Cells);
        Assert.False(result.HasTruncatedCells);
    }

    [Fact]
    public void Extract_ShouldUseOnlyFormulaCachedValue()
    {
        // Arrange
        var builder = new XlsxBuilder();
        builder.Sheet("A", Row(2, Cell("A2", "300", formula: "SUM(B1:B9)"), Cell("B2", "texto calculado", type: "str", formula: "CONCAT(\"a\")")));

        // Act
        var cells = _extractor.Extract(builder.Build()).Sheets[0].Rows[0].Cells;

        // Assert
        Assert.Equal(["300", "texto calculado"], cells);
        Assert.DoesNotContain(cells, c => c.Contains("SUM"));
    }

    [Fact]
    public void Extract_ShouldKeepColumnPositionsWithEmptyCellsInTheMiddle()
    {
        // Arrange
        var builder = new XlsxBuilder();
        builder.Sheet("A", Row(3, Cell("A3", "1"), Cell("D3", "4")));

        // Act
        var cells = _extractor.Extract(builder.Build()).Sheets[0].Rows[0].Cells;

        // Assert
        Assert.Equal(["1", "", "", "4"], cells);
    }

    [Fact]
    public void Extract_ShouldSkipHiddenSheetsAndEmptyRows()
    {
        // Arrange
        var builder = new XlsxBuilder();
        builder.Sheet("Visível", Row(1, Cell("A1", "1")) + "<row r=\"2\"><c r=\"A2\"/></row>")
            .Sheet("Oculta", Row(1, Cell("A1", "999")), state: "hidden")
            .Sheet("MuitoOculta", Row(1, Cell("A1", "998")), state: "veryHidden");

        // Act
        var result = _extractor.Extract(builder.Build());

        // Assert
        var sheet = Assert.Single(result.Sheets);
        Assert.Equal("Visível", sheet.Name);
        Assert.Single(sheet.Rows);
    }

    [Fact]
    public void Extract_ShouldTruncateLongCellsAndFlagIt()
    {
        // Arrange
        var builder = new XlsxBuilder();
        builder.SharedStrings.Add(new string('x', 800));
        builder.Sheet("A", Row(1, Cell("A1", "0", "s")));

        // Act
        var result = _extractor.Extract(builder.Build());

        // Assert
        Assert.True(result.HasTruncatedCells);
        Assert.Equal(XlsxTextExtractor.MaxCellLength + 1, result.Sheets[0].Rows[0].Cells[0].Length);
    }

    [Fact]
    public void Extract_ShouldIgnoreMacrosCommentsDrawingsAndExternalLinksParts()
    {
        // Arrange: partes que nunca devem ser lidas, inclusive com XML inválido
        var builder = new XlsxBuilder();
        builder.Sheet("A", Row(1, Cell("A1", "1")))
            .Part("xl/vbaProject.bin", "MZ binário qualquer")
            .Part("xl/comments1.xml", "<nao-e-xml")
            .Part("xl/drawings/drawing1.xml", "<nao-e-xml")
            .Part("xl/externalLinks/externalLink1.xml", "<!DOCTYPE x [<!ENTITY e SYSTEM \"file:///etc/passwd\">]><x>&e;</x>");

        // Act
        var result = _extractor.Extract(builder.Build());

        // Assert
        Assert.Equal("1", result.Sheets[0].Rows[0].Cells[0]);
    }

    [Fact]
    public void Extract_WithDtdInSheet_ShouldRejectWithoutResolvingEntities()
    {
        // Arrange: XXE / expansão de entidades
        var builder = new XlsxBuilder();
        builder.Sheet("A", Row(1, Cell("A1", "1")));
        var bytes = builder.Build();
        bytes = ReplaceEntry(bytes, "xl/worksheets/sheet1.xml",
            $"<?xml version=\"1.0\"?><!DOCTYPE w [<!ENTITY a \"aaaaaaaaaa\"><!ENTITY b \"&a;&a;&a;&a;&a;\">]><worksheet xmlns=\"{MainNs}\"><sheetData><row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>&b;</t></is></c></row></sheetData></worksheet>");

        // Act & Assert
        var ex = Assert.Throws<InvalidSpreadsheetException>(() => _extractor.Extract(bytes));
        Assert.Equal(XlsxTextExtractor.InvalidFileMessage, ex.Message);
    }

    [Fact]
    public void Extract_WithOleSignature_ShouldAskToExportAsXlsx()
    {
        // Arrange: .xls antigo ou .xlsx protegido por senha
        byte[] ole = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0, 0, 0, 0];

        // Act & Assert
        var ex = Assert.Throws<InvalidSpreadsheetException>(() => _extractor.Extract(ole));
        Assert.Equal(XlsxTextExtractor.ExportAsXlsxMessage, ex.Message);
    }

    [Fact]
    public void Extract_WithNonZipContent_ShouldReject()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidSpreadsheetException>(() => _extractor.Extract("Data;Valor\n01/10;10,00"u8.ToArray()));
        Assert.Equal(XlsxTextExtractor.InvalidFileMessage, ex.Message);
    }

    [Fact]
    public void Extract_WithZipThatIsNotASpreadsheet_ShouldReject()
    {
        // Arrange: ZIP qualquer (ex.: .docx renomeado) sem o content type de planilha
        var builder = new XlsxBuilder { MainContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml" };
        builder.Sheet("A", Row(1, Cell("A1", "1")));

        // Act & Assert
        Assert.Throws<InvalidSpreadsheetException>(() => _extractor.Extract(builder.Build()));
    }

    [Fact]
    public void Extract_WithMacroEnabledWorkbook_ShouldReject()
    {
        // Arrange
        var builder = new XlsxBuilder { MainContentType = "application/vnd.ms-excel.sheet.macroEnabled.main+xml" };
        builder.Sheet("A", Row(1, Cell("A1", "1")));

        // Act & Assert
        var ex = Assert.Throws<InvalidSpreadsheetException>(() => _extractor.Extract(builder.Build()));
        Assert.Equal(XlsxTextExtractor.MacroMessage, ex.Message);
    }

    [Fact]
    public void Extract_WithTooManyEntries_ShouldReject()
    {
        // Arrange
        var builder = new XlsxBuilder();
        builder.Sheet("A", Row(1, Cell("A1", "1")));
        for (var i = 0; i < XlsxTextExtractor.MaxEntries; i++)
            builder.Part($"xl/media/image{i}.png", "x");

        // Act & Assert
        var ex = Assert.Throws<InvalidSpreadsheetException>(() => _extractor.Extract(builder.Build()));
        Assert.Equal(XlsxTextExtractor.TooLargeContentMessage, ex.Message);
    }

    [Fact]
    public void Extract_WithHighlyCompressedPart_ShouldRejectAsZipBomb()
    {
        // Arrange: 15 MB de espaços comprimem muito acima de 100:1
        var builder = new XlsxBuilder();
        builder.Sheet("A", Row(1, Cell("A1", "1")));
        var bytes = builder.Build();
        var huge = $"<?xml version=\"1.0\"?><sst xmlns=\"{MainNs}\">" + new string(' ', 15 * 1024 * 1024) + "</sst>";
        bytes = ReplaceEntry(bytes, "xl/sharedStrings.xml", huge);

        // Act & Assert
        var ex = Assert.Throws<InvalidSpreadsheetException>(() => _extractor.Extract(bytes));
        Assert.Equal(XlsxTextExtractor.TooLargeContentMessage, ex.Message);
    }

    [Fact]
    public void Extract_WithPartAboveTwentyMegabytes_ShouldReject()
    {
        // Arrange: conteúdo pouco compressível (taxa baixa), mas acima do limite por parte
        var builder = new XlsxBuilder();
        builder.Sheet("A", Row(1, Cell("A1", "1")));
        var bytes = builder.Build();
        var random = new Random(3);
        var sb = new StringBuilder($"<?xml version=\"1.0\"?><sst xmlns=\"{MainNs}\"><si><t>");
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        while (sb.Length < 21 * 1024 * 1024)
            sb.Append(alphabet[random.Next(alphabet.Length)]);
        sb.Append("</t></si></sst>");
        bytes = ReplaceEntry(bytes, "xl/sharedStrings.xml", sb.ToString());

        // Act & Assert
        var ex = Assert.Throws<InvalidSpreadsheetException>(() => _extractor.Extract(bytes));
        Assert.Equal(XlsxTextExtractor.TooLargeContentMessage, ex.Message);
    }

    [Fact]
    public void Extract_WithMoreThanTenVisibleSheets_ShouldReject()
    {
        // Arrange
        var builder = new XlsxBuilder();
        for (var i = 1; i <= 11; i++)
            builder.Sheet($"Aba{i}", Row(1, Cell("A1", "1")));

        // Act & Assert
        var ex = Assert.Throws<InvalidSpreadsheetException>(() => _extractor.Extract(builder.Build()));
        Assert.Equal(XlsxTextExtractor.TooManySheetsMessage, ex.Message);
    }

    [Fact]
    public void Extract_WithMoreThanFiveThousandRows_ShouldReject()
    {
        // Arrange
        var builder = new XlsxBuilder();
        builder.Sheet("A", string.Concat(Enumerable.Range(1, XlsxTextExtractor.MaxRows + 1).Select(r => Row(r, Cell($"A{r}", "1")))));

        // Act & Assert
        var ex = Assert.Throws<InvalidSpreadsheetException>(() => _extractor.Extract(builder.Build()));
        Assert.Equal(XlsxTextExtractor.TooManyRowsMessage, ex.Message);
    }

    [Fact]
    public void Extract_WithDataBeyondColumnFifty_ShouldReject()
    {
        // Arrange: coluna AY = 51
        var builder = new XlsxBuilder();
        builder.Sheet("A", Row(1, Cell("AY1", "1")));

        // Act & Assert
        var ex = Assert.Throws<InvalidSpreadsheetException>(() => _extractor.Extract(builder.Build()));
        Assert.Equal(XlsxTextExtractor.TooManyColumnsMessage, ex.Message);
    }

    [Fact]
    public void Extract_WithDate1904System_ShouldUseOtherEpoch()
    {
        // Arrange: serial 0 no sistema 1904 = 01/01/1904
        var builder = new XlsxBuilder { StylesXml = DateStyles, Date1904 = true };
        builder.Sheet("A", Row(1, Cell("A1", "44836", style: 1)));

        // Act
        var cells = _extractor.Extract(builder.Build()).Sheets[0].Rows[0].Cells;

        // Assert
        Assert.Equal("2026-10-03", cells[0]);
    }

    [Theory]
    [InlineData(1, false, "1900-01-01")]
    [InlineData(59, false, "1900-02-28")]
    [InlineData(60, false, "1900-02-28")] // falso 29/02/1900 do Excel
    [InlineData(61, false, "1900-03-01")]
    [InlineData(46298.75, false, "2026-10-03")]
    [InlineData(0, true, "1904-01-01")]
    public void SerialToDate_ShouldHandleBothSystemsAndFakeLeapDay(double serial, bool date1904, string expected)
    {
        // Act & Assert
        Assert.Equal(expected, XlsxTextExtractor.SerialToDate(serial, date1904)!.Value.ToString("yyyy-MM-dd"));
    }

    [Theory]
    [InlineData("dd/mm/yyyy", true)]
    [InlineData("[$-416]d-mmm-yy", true)]
    [InlineData("\"R$\"\\ #,##0.00", false)]
    [InlineData("0.00%", false)]
    [InlineData("\"dia\" 0", false)]
    [InlineData("General", false)]
    public void IsDateFormatCode_ShouldIgnoreQuotedTextAndLocaleBrackets(string code, bool expected)
    {
        // Act & Assert
        Assert.Equal(expected, XlsxTextExtractor.IsDateFormatCode(code));
    }

    [Theory]
    [InlineData(1234.5599999999999, "1234.56")]
    [InlineData(-0.1, "-0.1")]
    [InlineData(100, "100")]
    public void FormatNumber_ShouldUseInvariantCultureWithoutFloatingNoise(double number, string expected)
    {
        // Act & Assert
        Assert.Equal(expected, XlsxTextExtractor.FormatNumber(number));
    }

    [Fact]
    public void FormattedText_ShouldRoundTripThroughParse()
    {
        // Arrange
        var builder = new XlsxBuilder();
        builder.SharedStrings.Add("texto\tcom tab");
        builder.Sheet("Fatura", Row(4, Cell("A4", "0", "s"), Cell("B4", "10")));
        var content = _extractor.Extract(builder.Build());

        // Act
        var parsed = SpreadsheetText.Parse(SpreadsheetText.Format(content));

        // Assert
        var row = Assert.Single(Assert.Single(parsed.Sheets).Rows);
        Assert.Equal(4, row.Number);
        Assert.Equal(["texto com tab", "10"], row.Cells);
    }

    private static byte[] ReplaceEntry(byte[] zipBytes, string path, string content)
    {
        using var ms = new MemoryStream();
        ms.Write(zipBytes);
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
        {
            zip.GetEntry(path)?.Delete();
            using var stream = zip.CreateEntry(path, CompressionLevel.Optimal).Open();
            stream.Write(Encoding.UTF8.GetBytes(content));
        }
        return ms.ToArray();
    }
}
