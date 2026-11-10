/**
 * Datas sem horário (data da compra, da transação, fechamento, vencimento, próxima
 * ocorrência…). A API as grava como meia-noite UTC (ex.: "2026-10-22T00:00:00Z"); convertê-las
 * com `new Date(iso)` mostra o dia anterior no horário de Brasília. Aqui vale sempre o dia do
 * calendário da string (parte "yyyy-MM-dd"), sem fuso.
 *
 * Instantes com horário real (sincronização, envio de uma importação) NÃO usam este arquivo:
 * continuam com `new Date(iso)` no fuso do navegador.
 */

const DATE_ONLY_PATTERN = /^(\d{4})-(\d{2})-(\d{2})/;

interface DateParts {
  year: string;
  month: string;
  day: string;
}

function readDateParts(value: string | null | undefined): DateParts | null {
  if (!value) return null;
  const match = DATE_ONLY_PATTERN.exec(value.trim());
  if (!match) return null;
  const [, year, month, day] = match;
  const m = Number(month);
  const d = Number(day);
  if (m < 1 || m > 12 || d < 1 || d > 31) return null;
  return { year, month, day };
}

/** "dd/MM/aaaa" (ou "dd/MM" com `short`); "—" quando ausente ou inválida. */
export function formatDateOnly(
  value: string | null | undefined,
  options: { short?: boolean } = {}
): string {
  const parts = readDateParts(value);
  if (!parts) return "—";
  return options.short
    ? `${parts.day}/${parts.month}`
    : `${parts.day}/${parts.month}/${parts.year}`;
}

/** Valor de `<input type="date">` ("yyyy-MM-dd"); "" quando ausente ou inválida. */
export function toDateInputValue(value: string | null | undefined): string {
  const parts = readDateParts(value);
  return parts ? `${parts.year}-${parts.month}-${parts.day}` : "";
}

/** Hoje no calendário local ("yyyy-MM-dd") — não usar `toISOString()`, que é o dia em UTC. */
export function todayDateInputValue(now: Date = new Date()): string {
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}
