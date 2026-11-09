using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Exceptions;

namespace MoneyManager.Infrastructure.Services.Spreadsheets;

/// <summary>
/// Leitor nativo de <c>.xlsx</c> (OpenXML) com <see cref="System.IO.Compression"/> + <see cref="System.Xml"/>, todo em
/// memória. Lê só <c>[Content_Types].xml</c>, o workbook e seus rels, <c>sharedStrings</c>, <c>styles</c> e as abas
/// visíveis; macros, links externos, conexões, desenhos, comentários e caches nunca são abertos. Fórmulas
/// entram só pelo valor calculado.
/// </summary>
public partial class XlsxTextExtractor : ISpreadsheetTextExtractor
{
    public const int MaxEntries = 200;
    public const long MaxPartBytes = 20L * 1024 * 1024;
    public const long MaxTotalBytes = 50L * 1024 * 1024;
    public const int MaxCompressionRatio = 100;
    public const int MaxSheets = 10;
    public const int MaxRows = 5_000;
    public const int MaxColumns = 50;
    public const int MaxCellLength = 500;

    // Partes pequenas não têm como ser bomba; a taxa só é avaliada depois desse volume lido
    private const long RatioCheckThresholdBytes = 1L * 1024 * 1024;

    public const string ExportAsXlsxMessage =
        "Envie a planilha no formato .xlsx. Arquivos .xls, .xlsm, .csv ou planilhas protegidas por senha não são aceitos: abra o arquivo no Excel e salve como .xlsx, sem senha.";
    public const string InvalidFileMessage =
        "O arquivo enviado não é uma planilha .xlsx válida. Exporte a fatura novamente em .xlsx.";
    public const string MacroMessage =
        "Planilhas com macros (.xlsm) não são aceitas. Salve a planilha como .xlsx, sem macros.";
    public const string TooLargeContentMessage =
        "A planilha é grande demais para ser processada (conteúdo descompactado acima do limite).";
    public const string TooManySheetsMessage = "A planilha tem mais de 10 abas visíveis. Envie só a aba da fatura.";
    public const string TooManyRowsMessage = "A planilha tem mais de 5.000 linhas preenchidas. Envie só a fatura.";
    public const string TooManyColumnsMessage = "A planilha tem dados além da coluna 50. Envie só a tabela da fatura.";
    public const string NoSheetsMessage = "A planilha não tem nenhuma aba visível com dados.";

    private const string SheetMainContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml";
    private const string RelationshipsNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];
    private static readonly byte[] OleSignature = [0xD0, 0xCF, 0x11, 0xE0];

    // Formatos numéricos embutidos de data (inclui os de localidade do leste asiático)
    private static readonly HashSet<int> BuiltInDateFormats =
        [14, 15, 16, 17, 18, 19, 20, 21, 22, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 45, 46, 47, 50, 51, 52, 53, 54, 55, 56, 57, 58];

    public SpreadsheetContent Extract(byte[] content)
    {
        if (StartsWith(content, OleSignature))
            throw new InvalidSpreadsheetException(ExportAsXlsxMessage);
        if (!StartsWith(content, ZipSignature))
            throw new InvalidSpreadsheetException(InvalidFileMessage);

        try
        {
            using var archive = new ZipArchive(new MemoryStream(content, writable: false), ZipArchiveMode.Read);
            return new Reader(archive).Read();
        }
        catch (InvalidDataException)
        {
            throw new InvalidSpreadsheetException(InvalidFileMessage);
        }
        catch (XmlException)
        {
            throw new InvalidSpreadsheetException(InvalidFileMessage);
        }
        catch (NotSupportedException)
        {
            // Compressão ou criptografia de entrada não suportada pelo ZIP
            throw new InvalidSpreadsheetException(InvalidFileMessage);
        }
    }

    private static bool StartsWith(byte[] content, byte[] signature) =>
        content.Length >= signature.Length && content.AsSpan(0, signature.Length).SequenceEqual(signature);

    private sealed class Reader
    {
        private readonly ZipArchive _archive;
        private readonly Dictionary<string, ZipArchiveEntry> _entries;
        private long _totalBytes;

        public Reader(ZipArchive archive)
        {
            _archive = archive;
            if (_archive.Entries.Count > MaxEntries)
                throw new InvalidSpreadsheetException(TooLargeContentMessage);

            _entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in _archive.Entries)
                _entries.TryAdd(entry.FullName.TrimStart('/'), entry);
        }

        public SpreadsheetContent Read()
        {
            var workbookPath = ReadWorkbookPath();
            var workbook = LoadXml(workbookPath) ?? throw new InvalidSpreadsheetException(InvalidFileMessage);
            var rels = LoadRelationships(workbookPath);

            XNamespace ns = workbook.Root!.Name.Namespace;
            var date1904 = workbook.Root.Element(ns + "workbookPr")?.Attribute("date1904")?.Value is "1" or "true";

            var visibleSheets = workbook.Root.Element(ns + "sheets")?.Elements(ns + "sheet")
                .Where(s => s.Attribute("state")?.Value is null or "visible")
                .Select(s => (
                    Name: s.Attribute("name")?.Value ?? "Planilha",
                    RelId: s.Attribute(XName.Get("id", RelationshipsNs))?.Value))
                .ToList() ?? [];

            if (visibleSheets.Count > MaxSheets)
                throw new InvalidSpreadsheetException(TooManySheetsMessage);

            var baseDir = DirectoryOf(workbookPath);
            var sharedStrings = ReadSharedStrings(ResolveTarget(baseDir, rels, "sharedStrings"));
            var dateStyles = ReadDateStyles(ResolveTarget(baseDir, rels, "styles"));

            var sheets = new List<SpreadsheetSheet>();
            var totalRows = 0;
            var truncated = false;

            foreach (var (name, relId) in visibleSheets)
            {
                if (relId is null || !rels.TryGetValue(relId, out var rel))
                    continue;

                var sheetXml = LoadXml(Combine(baseDir, rel.Target));
                if (sheetXml is null)
                    continue;

                var rows = ReadRows(sheetXml, sharedStrings, dateStyles, date1904, ref truncated);
                totalRows += rows.Count;
                if (totalRows > MaxRows)
                    throw new InvalidSpreadsheetException(TooManyRowsMessage);

                if (rows.Count > 0)
                    sheets.Add(new SpreadsheetSheet(name, rows));
            }

            if (sheets.Count == 0)
                throw new InvalidSpreadsheetException(NoSheetsMessage);

            return new SpreadsheetContent(sheets, truncated);
        }

        /// <summary>Confere o tipo do pacote e devolve o caminho do workbook declarado em [Content_Types].xml.</summary>
        private string ReadWorkbookPath()
        {
            var types = LoadXml("[Content_Types].xml") ?? throw new InvalidSpreadsheetException(InvalidFileMessage);
            var overrides = types.Root!.Elements().Where(e => e.Name.LocalName == "Override").ToList();

            if (overrides.Any(o => (o.Attribute("ContentType")?.Value ?? "").Contains("macroEnabled", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidSpreadsheetException(MacroMessage);

            var main = overrides.FirstOrDefault(o => o.Attribute("ContentType")?.Value == SheetMainContentType)
                       ?? throw new InvalidSpreadsheetException(InvalidFileMessage);

            return (main.Attribute("PartName")?.Value ?? "").TrimStart('/');
        }

        private Dictionary<string, (string Type, string Target)> LoadRelationships(string partPath)
        {
            var relsPath = Combine(DirectoryOf(partPath), "_rels/" + Path.GetFileName(partPath) + ".rels");
            var xml = LoadXml(relsPath);
            var result = new Dictionary<string, (string Type, string Target)>(StringComparer.Ordinal);
            if (xml is null)
                return result;

            foreach (var rel in xml.Root!.Elements().Where(e => e.Name.LocalName == "Relationship"))
            {
                var id = rel.Attribute("Id")?.Value;
                var target = rel.Attribute("Target")?.Value;
                // Links externos (TargetMode=External) nunca são seguidos
                if (id is null || target is null || rel.Attribute("TargetMode")?.Value == "External")
                    continue;
                result[id] = (rel.Attribute("Type")?.Value ?? "", target);
            }
            return result;
        }

        private static string? ResolveTarget(string baseDir, Dictionary<string, (string Type, string Target)> rels, string typeSuffix)
        {
            var rel = rels.Values.FirstOrDefault(r => r.Type.EndsWith("/" + typeSuffix, StringComparison.Ordinal));
            return rel.Target is null ? null : Combine(baseDir, rel.Target);
        }

        private List<string> ReadSharedStrings(string? path)
        {
            var xml = path is null ? null : LoadXml(path);
            if (xml is null)
                return [];

            XNamespace ns = xml.Root!.Name.Namespace;
            // Texto do item: <t> direto ou dos trechos <r><t>, sem a leitura fonética (<rPh>)
            return xml.Root.Elements(ns + "si")
                .Select(si => string.Concat(si.Descendants(ns + "t")
                    .Where(t => t.Parent?.Name.LocalName != "rPh")
                    .Select(t => t.Value)))
                .ToList();
        }

        /// <summary>Índices de <c>cellXfs</c> cujo formato numérico é de data.</summary>
        private HashSet<int> ReadDateStyles(string? path)
        {
            var result = new HashSet<int>();
            var xml = path is null ? null : LoadXml(path);
            if (xml is null)
                return result;

            XNamespace ns = xml.Root!.Name.Namespace;
            var customDateFormats = (xml.Root.Element(ns + "numFmts")?.Elements(ns + "numFmt") ?? [])
                .Where(f => IsDateFormatCode(f.Attribute("formatCode")?.Value))
                .Select(f => int.TryParse(f.Attribute("numFmtId")?.Value, out var id) ? id : -1)
                .ToHashSet();

            var index = 0;
            foreach (var xf in xml.Root.Element(ns + "cellXfs")?.Elements(ns + "xf") ?? [])
            {
                if (int.TryParse(xf.Attribute("numFmtId")?.Value, out var fmt)
                    && (BuiltInDateFormats.Contains(fmt) || customDateFormats.Contains(fmt)))
                {
                    result.Add(index);
                }
                index++;
            }
            return result;
        }

        private static List<SpreadsheetRow> ReadRows(
            XDocument sheet, List<string> sharedStrings, HashSet<int> dateStyles, bool date1904, ref bool truncated)
        {
            XNamespace ns = sheet.Root!.Name.Namespace;
            var rows = new List<SpreadsheetRow>();
            var sheetData = sheet.Root.Element(ns + "sheetData");
            if (sheetData is null)
                return rows;

            var nextRowNumber = 1;
            foreach (var row in sheetData.Elements(ns + "row"))
            {
                var rowNumber = int.TryParse(row.Attribute("r")?.Value, out var r) ? r : nextRowNumber;
                nextRowNumber = rowNumber + 1;

                var cells = new SortedDictionary<int, string>();
                var nextColumn = 1;
                foreach (var cell in row.Elements(ns + "c"))
                {
                    var column = ColumnIndex(cell.Attribute("r")?.Value) ?? nextColumn;
                    nextColumn = column + 1;

                    var value = CellValue(cell, ns, sharedStrings, dateStyles, date1904);
                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    if (column > MaxColumns)
                        throw new InvalidSpreadsheetException(TooManyColumnsMessage);

                    value = value.Trim();
                    if (value.Length > MaxCellLength)
                    {
                        value = value[..MaxCellLength] + "…";
                        truncated = true;
                    }
                    cells[column] = value;
                }

                if (cells.Count == 0)
                    continue;

                // Mantém a posição das colunas: células vazias no meio viram campos vazios
                var lastColumn = cells.Keys.Max();
                var values = Enumerable.Range(1, lastColumn)
                    .Select(c => cells.TryGetValue(c, out var v) ? v : string.Empty)
                    .ToList();
                rows.Add(new SpreadsheetRow(rowNumber, values));
            }
            return rows;
        }

        private static string? CellValue(XElement cell, XNamespace ns, List<string> sharedStrings, HashSet<int> dateStyles, bool date1904)
        {
            var type = cell.Attribute("t")?.Value;
            if (type == "inlineStr")
                return string.Concat(cell.Element(ns + "is")?.Descendants(ns + "t").Select(t => t.Value) ?? []);

            // Fórmulas (<f>) são ignoradas: só o valor calculado em <v>
            var raw = cell.Element(ns + "v")?.Value;
            if (raw is null)
                return null;

            switch (type)
            {
                case "s":
                    return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) && i >= 0 && i < sharedStrings.Count
                        ? sharedStrings[i]
                        : null;
                case "b":
                    return raw == "1" ? "VERDADEIRO" : "FALSO";
                case "str":
                case "e":
                    return raw;
            }

            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                return raw;

            var styleIndex = int.TryParse(cell.Attribute("s")?.Value, out var s) ? s : 0;
            if (dateStyles.Contains(styleIndex) && SerialToDate(number, date1904) is { } date)
                return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            return FormatNumber(number);
        }

        private XDocument? LoadXml(string path)
        {
            if (!_entries.TryGetValue(path.TrimStart('/'), out var entry))
                return null;

            var bytes = ReadEntry(entry);
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaxPartBytes,
                MaxCharactersFromEntities = 0,
                CloseInput = true
            };
            using var reader = XmlReader.Create(new MemoryStream(bytes, writable: false), settings);
            return XDocument.Load(reader);
        }

        /// <summary>
        /// Descompacta contando os bytes de fato lidos (não confia no tamanho declarado no ZIP).
        /// </summary>
        private byte[] ReadEntry(ZipArchiveEntry entry)
        {
            using var stream = entry.Open();
            using var output = new MemoryStream();
            var buffer = new byte[81920];
            long read = 0;
            int n;
            while ((n = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                read += n;
                _totalBytes += n;
                if (read > MaxPartBytes || _totalBytes > MaxTotalBytes)
                    throw new InvalidSpreadsheetException(TooLargeContentMessage);
                if (read > RatioCheckThresholdBytes && read > (long)Math.Max(1, entry.CompressedLength) * MaxCompressionRatio)
                    throw new InvalidSpreadsheetException(TooLargeContentMessage);
                output.Write(buffer, 0, n);
            }
            return output.ToArray();
        }
    }

    // --- Utilitários (internos para teste) ---

    internal static DateTime? SerialToDate(double serial, bool date1904)
    {
        if (serial < 0 || serial > 2_958_465) // 31/12/9999
            return null;

        var days = Math.Floor(serial);
        if (date1904)
            return new DateTime(1904, 1, 1).AddDays(days);

        // Sistema 1900: o Excel conta um 29/02/1900 que não existe (serial 60)
        if (days < 1)
            return null;
        if (days < 60)
            return new DateTime(1899, 12, 31).AddDays(days);
        if (days == 60)
            return new DateTime(1900, 2, 28);
        return new DateTime(1899, 12, 30).AddDays(days);
    }

    internal static string FormatNumber(double number)
    {
        var value = Math.Round((decimal)number, 8, MidpointRounding.AwayFromZero);
        return value.ToString("0.########", CultureInfo.InvariantCulture);
    }

    /// <summary>Formato numérico personalizado de data: tem dia ou ano fora de textos entre aspas, escapes e colchetes.</summary>
    internal static bool IsDateFormatCode(string? code)
    {
        if (string.IsNullOrEmpty(code))
            return false;
        var stripped = FormatNoiseRegex().Replace(code, string.Empty);
        return stripped.Contains('d', StringComparison.OrdinalIgnoreCase)
               || stripped.Contains('y', StringComparison.OrdinalIgnoreCase);
    }

    internal static int? ColumnIndex(string? cellReference)
    {
        if (string.IsNullOrEmpty(cellReference))
            return null;
        var index = 0;
        foreach (var ch in cellReference)
        {
            if (ch is >= 'A' and <= 'Z')
                index = index * 26 + (ch - 'A' + 1);
            else if (ch is >= 'a' and <= 'z')
                index = index * 26 + (ch - 'a' + 1);
            else
                break;
            if (index > 16_384)
                return 16_385;
        }
        return index == 0 ? null : index;
    }

    private static string DirectoryOf(string path)
    {
        var slash = path.LastIndexOf('/');
        return slash < 0 ? string.Empty : path[..(slash + 1)];
    }

    /// <summary>Resolve o alvo de um relacionamento (relativo à pasta da parte ou absoluto a partir da raiz).</summary>
    private static string Combine(string baseDir, string target)
    {
        if (target.StartsWith('/'))
            return target.TrimStart('/');

        var parts = new List<string>((baseDir + target).Split('/', StringSplitOptions.RemoveEmptyEntries));
        var resolved = new List<string>();
        foreach (var part in parts)
        {
            if (part == "..")
            {
                if (resolved.Count > 0)
                    resolved.RemoveAt(resolved.Count - 1);
            }
            else if (part != ".")
            {
                resolved.Add(part);
            }
        }
        return string.Join('/', resolved);
    }

    [GeneratedRegex("\"[^\"]*\"|\\\\.|\\[[^\\]]*\\]")]
    private static partial Regex FormatNoiseRegex();
}
