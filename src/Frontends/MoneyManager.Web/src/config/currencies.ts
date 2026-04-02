export interface CurrencyInfo {
  code: string;
  symbol: string;
  name: string;
  locale: string;
}

export const currencies: CurrencyInfo[] = [
  { code: "BRL", symbol: "R$", name: "Real Brasileiro", locale: "pt-BR" },
  { code: "USD", symbol: "$", name: "US Dollar", locale: "en-US" },
  { code: "EUR", symbol: "€", name: "Euro", locale: "de-DE" },
  { code: "GBP", symbol: "£", name: "British Pound", locale: "en-GB" },
  { code: "JPY", symbol: "¥", name: "Japanese Yen", locale: "ja-JP" },
  { code: "ARS", symbol: "ARS$", name: "Peso Argentino", locale: "es-AR" },
  { code: "CLP", symbol: "CLP$", name: "Peso Chileno", locale: "es-CL" },
  { code: "COP", symbol: "COP$", name: "Peso Colombiano", locale: "es-CO" },
  { code: "MXN", symbol: "MX$", name: "Peso Mexicano", locale: "es-MX" },
  { code: "PEN", symbol: "S/", name: "Sol Peruano", locale: "es-PE" },
];

export function getCurrencySymbol(code: string): string {
  return currencies.find((c) => c.code === code)?.symbol ?? code;
}

export function getCurrencyLocale(code: string): string {
  return currencies.find((c) => c.code === code)?.locale ?? "pt-BR";
}
