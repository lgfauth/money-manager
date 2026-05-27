export function obfuscateNumericChars(value: string, maskChar = "*"): string {
  return value.replace(/\d/g, maskChar);
}

export function formatCurrencyValue(
  value: number,
  currency = "BRL",
  locale = "pt-BR"
): string {
  return new Intl.NumberFormat(locale, {
    style: "currency",
    currency,
  }).format(value);
}