using System.Globalization;
using System.Text;

namespace MoneyManager.Application.Services;

/// <summary>
/// Normalização de textos da importação de fatura (nomes de titulares, categorias e descrições):
/// sem acentos, em maiúsculas e com espaços simples.
/// </summary>
internal static class InvoiceImportText
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        var lastWasSpace = false;

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasSpace && sb.Length > 0)
                    sb.Append(' ');
                lastWasSpace = true;
                continue;
            }

            sb.Append(char.ToUpperInvariant(ch));
            lastWasSpace = false;
        }

        return sb.ToString().TrimEnd().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Nomes equivalentes quando, normalizados, um é prefixo do outro — o banco pode cortar o nome
    /// (ex.: 20 caracteres no Itaú).
    /// </summary>
    public static bool NamesMatch(string? a, string? b)
    {
        var na = Normalize(a);
        var nb = Normalize(b);
        if (na.Length == 0 || nb.Length == 0)
            return false;

        return na.StartsWith(nb, StringComparison.Ordinal) || nb.StartsWith(na, StringComparison.Ordinal);
    }
}
