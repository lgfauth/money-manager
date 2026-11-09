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

    // Prefixos de adquirentes/gateways que o banco põe antes do nome do estabelecimento
    private static readonly string[] GatewayPrefixes =
        ["MERCADOPAGO", "MERCADO PAGO", "PAGSEGURO", "PAYPAL", "PICPAY", "SUMUP"];

    private static readonly System.Text.RegularExpressions.Regex InstallmentPattern = new(
        @"\b(PARCELA|PARC)?\s*\d{1,2}\s*(/|DE)\s*\d{1,2}\b",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>
    /// Descrição comparável de um lançamento: normalizada, sem número de parcela ("3/10", "Parcela 3 de 10"),
    /// sem pontuação e sem prefixo de adquirente/gateway ("IFD*", "PAG*", "MERCADOPAGO ").
    /// </summary>
    public static string NormalizeDescription(string? value)
    {
        var text = Normalize(value);
        if (text.Length == 0)
            return text;

        // "XXX*LOJA" → "LOJA" (prefixo curto terminado em asterisco)
        var star = text.IndexOf('*');
        if (star is > 0 and <= 12 && star < text.Length - 1)
            text = text[(star + 1)..];

        text = InstallmentPattern.Replace(text, " ");

        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
            sb.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        text = Normalize(sb.ToString());

        foreach (var prefix in GatewayPrefixes)
        {
            if (text.StartsWith(prefix + " ", StringComparison.Ordinal) && text.Length > prefix.Length + 1)
            {
                text = text[(prefix.Length + 1)..];
                break;
            }
        }

        return text;
    }

    /// <summary>
    /// Descrições parecidas (desempate na detecção de duplicados): iguais, uma contém a outra
    /// ou compartilham alguma palavra com 3+ letras.
    /// </summary>
    public static bool DescriptionsLookAlike(string? a, string? b)
    {
        var na = NormalizeDescription(a);
        var nb = NormalizeDescription(b);
        if (na.Length == 0 || nb.Length == 0)
            return false;

        if (na == nb || na.Contains(nb, StringComparison.Ordinal) || nb.Contains(na, StringComparison.Ordinal))
            return true;

        var wordsA = na.Split(' ').Where(w => w.Length >= 3 && !w.All(char.IsDigit)).ToHashSet(StringComparer.Ordinal);
        return nb.Split(' ').Any(w => w.Length >= 3 && wordsA.Contains(w));
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
