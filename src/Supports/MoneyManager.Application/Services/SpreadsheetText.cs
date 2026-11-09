using System.Text;

namespace MoneyManager.Application.Services;

/// <summary>Planilha convertida: abas visíveis com as linhas não vazias.</summary>
public sealed record SpreadsheetContent(IReadOnlyList<SpreadsheetSheet> Sheets, bool HasTruncatedCells);

public sealed record SpreadsheetSheet(string Name, IReadOnlyList<SpreadsheetRow> Rows);

/// <summary>Linha não vazia com o número original da planilha e as células na ordem das colunas.</summary>
public sealed record SpreadsheetRow(int Number, IReadOnlyList<string> Cells);

/// <summary>
/// Formato de texto da planilha gravado no lugar do <c>.xlsx</c> e enviado à IA: uma linha por linha da
/// planilha, começando pelo número original, com as células separadas por TAB. O mesmo texto é lido de
/// volta no Worker para conferir cada item com a linha de origem.
/// </summary>
public static class SpreadsheetText
{
    public const string ContentType = "text/plain";

    private const string Header =
        "[Planilha convertida em texto. Cada linha começa com o número da linha original; as células vêm separadas por TAB.]";
    private const string TruncatedMarker =
        "[Aviso: células com mais de 500 caracteres foram cortadas.]";
    private const string SheetPrefix = "[Aba: ";

    public static string Format(SpreadsheetContent content)
    {
        var sb = new StringBuilder();
        sb.Append(Header).Append('\n');
        if (content.HasTruncatedCells)
            sb.Append(TruncatedMarker).Append('\n');

        foreach (var sheet in content.Sheets)
        {
            sb.Append(SheetPrefix).Append(Clean(sheet.Name)).Append("]\n");
            foreach (var row in sheet.Rows)
            {
                sb.Append(row.Number);
                foreach (var cell in row.Cells)
                    sb.Append('\t').Append(Clean(cell));
                sb.Append('\n');
            }
        }

        return sb.ToString();
    }

    public static SpreadsheetContent Parse(string text)
    {
        var sheets = new List<SpreadsheetSheet>();
        var truncated = false;
        string? currentName = null;
        var currentRows = new List<SpreadsheetRow>();

        void Flush()
        {
            if (currentName is not null)
                sheets.Add(new SpreadsheetSheet(currentName, currentRows));
        }

        foreach (var line in text.Split('\n'))
        {
            if (line == TruncatedMarker)
            {
                truncated = true;
            }
            else if (line.StartsWith(SheetPrefix, StringComparison.Ordinal) && line.EndsWith(']'))
            {
                Flush();
                currentName = line[SheetPrefix.Length..^1];
                currentRows = [];
            }
            else if (currentName is not null && line.Length > 0)
            {
                var parts = line.Split('\t');
                if (int.TryParse(parts[0], out var number))
                    currentRows.Add(new SpreadsheetRow(number, parts[1..]));
            }
        }

        Flush();
        return new SpreadsheetContent(sheets, truncated);
    }

    // TAB e quebras de linha separam células e linhas no formato
    private static string Clean(string value) =>
        value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}

/// <summary>
/// Converte um <c>.xlsx</c> em <see cref="SpreadsheetContent"/> com as regras de segurança da importação
/// (assinatura, ZIP bomb, XML sem DTD, só partes necessárias, limites de abas/linhas/colunas/células).
/// Lança <see cref="MoneyManager.Domain.Exceptions.InvalidSpreadsheetException"/> (400) quando o arquivo viola alguma regra.
/// </summary>
public interface ISpreadsheetTextExtractor
{
    SpreadsheetContent Extract(byte[] content);
}
