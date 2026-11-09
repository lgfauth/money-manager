import type {
  ConfirmInvoiceImportItemRequest,
  CreditCardInvoiceImportResponseDto,
  InvoiceImportItemResponseDto,
  InvoiceImportSourceType,
  InvoiceImportWarningCode,
} from "@/types/invoice-import";

// Regras da revisão de uma importação de fatura no navegador. Espelham o backend
// (InvoiceImportReviewService, CreditCardInvoiceImportService e ConfirmInvoiceImportValidator)
// só para orientar o usuário: a confirmação é sempre revalidada pelo servidor.

/** Nome usado pelo backend para lançamentos sem titular identificado. */
export const UNIDENTIFIED_HOLDER_NAME = "Titular não identificado";

/** Compras à vista ou 1ª parcela com data anterior a hoje − 60 dias não podem ser importadas. */
export const MAX_DAYS_IN_PAST = 60;
export const MAX_INSTALLMENTS = 48;
export const MAX_DESCRIPTION_LENGTH = 200;

/** Alertas de item que impedem a confirmação sem correção (BlockingItemCodes do backend). */
export const BLOCKING_ITEM_WARNINGS: ReadonlySet<InvoiceImportWarningCode> = new Set([
  "MissingDate",
  "MissingDescription",
  "InvalidAmount",
  "FutureDate",
  "OutOfAllowedPeriod",
  "AlreadyImported",
  "NotFoundInSpreadsheet",
]);

/** Alertas recalculados após a edição do usuário (ItemValidationCodes do backend). */
const EDITABLE_ITEM_WARNINGS: ReadonlySet<InvoiceImportWarningCode> = new Set([
  "MissingDate",
  "MissingDescription",
  "InvalidAmount",
  "FutureDate",
  "OutOfAllowedPeriod",
]);

export const itemWarningTexts: Partial<
  Record<InvoiceImportWarningCode, { label: string; help: string }>
> = {
  MissingDate: {
    label: "Sem data",
    help: "Informe a data da compra.",
  },
  MissingDescription: {
    label: "Sem descrição",
    help: "Informe a descrição do lançamento.",
  },
  InvalidAmount: {
    label: "Valor inválido",
    help: "O valor deve ser maior que zero.",
  },
  FutureDate: {
    label: "Data futura",
    help: "A data não pode ser futura nem posterior ao fechamento da fatura.",
  },
  OutOfAllowedPeriod: {
    label: `Mais de ${MAX_DAYS_IN_PAST} dias`,
    help: `Compras à vista ou 1ª parcela com mais de ${MAX_DAYS_IN_PAST} dias não podem ser importadas. Corrija a data ou desmarque o lançamento.`,
  },
  PossibleDuplicate: {
    label: "Possível duplicado",
    help: "Já existe uma transação igual neste cartão. Marque só se for outra compra.",
  },
  AlreadyImported: {
    label: "Já importado",
    help: "Este lançamento já foi importado em outra importação e não será cadastrado de novo.",
  },
  AlreadyRegistered: {
    label: "Já cadastrado",
    help: "Esta parcela já está cadastrada no cartão. Marque só se for outra compra.",
  },
  PreviousInstallmentsNotRegistered: {
    label: "Parcelas anteriores ausentes",
    help: "As parcelas anteriores desta compra não estão cadastradas. Se importar, entram só esta parcela e as seguintes.",
  },
  NotFoundInSpreadsheet: {
    label: "Não encontrado na planilha",
    help: "O valor ou a data não aparecem na linha indicada da planilha. Desmarque o lançamento.",
  },
  LowConfidence: {
    label: "Leitura incerta",
    help: "A leitura deste lançamento pode ter erros. Confira data, descrição e valor.",
  },
};

export const isBlockingItemWarning = (code: InvoiceImportWarningCode) =>
  BLOCKING_ITEM_WARNINGS.has(code);

/** Lançamento editável na revisão: valores atuais (editados ou não) e se está marcado. */
export interface ReviewItemState {
  selected: boolean;
  /** "yyyy-mm-dd" ou "" quando sem data. */
  date: string;
  description: string;
  /** Valor da parcela, sempre positivo (estorno também). */
  amount: number;
  categoryId: string | null;
  installmentNumber: number | null;
  totalInstallments: number | null;
}

export interface ReviewState {
  selectedHolders: string[];
  items: Record<string, ReviewItemState>;
}

export const holderOf = (item: InvoiceImportItemResponseDto) =>
  item.holderName ?? UNIDENTIFIED_HOLDER_NAME;

/** Titular escolhido só é exigido quando a fatura tem mais de um. */
export const requiresHolderChoice = (job: CreditCardInvoiceImportResponseDto) =>
  job.holders.length > 1;

export function buildInitialReviewState(job: CreditCardInvoiceImportResponseDto): ReviewState {
  const suggested = job.holders.filter((h) => h.isSuggested).map((h) => h.name);
  const items: Record<string, ReviewItemState> = {};
  for (const item of job.items) {
    items[item.id] = {
      selected:
        item.isSelectedByDefault && !item.warnings.some((w) => w.code === "AlreadyImported"),
      date: item.date ? item.date.slice(0, 10) : "",
      description: item.description,
      amount: Math.abs(item.amount),
      categoryId: item.categoryId,
      installmentNumber: item.totalInstallments && item.totalInstallments > 1 ? item.installmentNumber : null,
      totalInstallments: item.totalInstallments && item.totalInstallments > 1 ? item.totalInstallments : null,
    };
  }
  return {
    selectedHolders: suggested.length > 0 ? suggested : job.holders.map((h) => h.name),
    items,
  };
}

/** Item entra na revisão quando o titular dele foi escolhido (ou há só um titular). */
export function isItemIncluded(
  job: CreditCardInvoiceImportResponseDto,
  item: InvoiceImportItemResponseDto,
  selectedHolders: string[]
) {
  return !requiresHolderChoice(job) || selectedHolders.includes(holderOf(item));
}

/** Hoje no calendário brasileiro, como "yyyy-mm-dd" (as regras de data do backend usam esse fuso). */
export function brazilToday(now = new Date()): string {
  return new Intl.DateTimeFormat("en-CA", {
    timeZone: "America/Sao_Paulo",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(now);
}

function addDays(isoDate: string, days: number): string {
  const [y, m, d] = isoDate.split("-").map(Number);
  const date = new Date(Date.UTC(y, m - 1, d + days));
  return date.toISOString().slice(0, 10);
}

/** Regras de item que a edição pode corrigir (ValidateItem do backend). */
export function validateItemLocally(
  state: ReviewItemState,
  closingDate: string | null,
  today: string
): InvoiceImportWarningCode[] {
  const codes: InvoiceImportWarningCode[] = [];
  if (!state.date) {
    codes.push("MissingDate");
  } else {
    const closing = closingDate ? closingDate.slice(0, 10) : null;
    if (state.date > today || (closing && state.date > closing)) {
      codes.push("FutureDate");
    } else if ((state.installmentNumber ?? 1) <= 1 && state.date < addDays(today, -MAX_DAYS_IN_PAST)) {
      // Parcelas X > 1 são isentas: a data é a da compra original
      codes.push("OutOfAllowedPeriod");
    }
  }
  if (!state.description.trim()) codes.push("MissingDescription");
  if (!(state.amount > 0)) codes.push("InvalidAmount");
  return codes;
}

/**
 * Alertas atuais do item: os do servidor que a edição não altera (duplicidade, planilha,
 * leitura incerta) mais os recalculados localmente a partir dos valores editados.
 */
export function currentItemWarnings(
  item: InvoiceImportItemResponseDto,
  state: ReviewItemState,
  closingDate: string | null,
  today: string
): InvoiceImportWarningCode[] {
  const fromServer = item.warnings
    .map((w) => w.code)
    .filter((code) => !EDITABLE_ITEM_WARNINGS.has(code));
  return [...fromServer, ...validateItemLocally(state, closingDate, today)];
}

export const isAlreadyImported = (item: InvoiceImportItemResponseDto) =>
  item.warnings.some((w) => w.code === "AlreadyImported");

/** Valor com sinal para os totais: estornos abatem do total. */
export const signedAmount = (item: InvoiceImportItemResponseDto, state: ReviewItemState) =>
  item.type === "Refund" ? -state.amount : state.amount;

/** "Descrição (dd/MM)" — mesmo formato que o backend usa nas mensagens de erro da confirmação. */
export function describeItemForErrors(state: ReviewItemState): string {
  const description = state.description.trim() || "Lançamento sem descrição";
  if (!state.date) return description;
  const [, month, day] = state.date.split("-");
  return `${description} (${day}/${month})`;
}

export function toConfirmItem(id: string, state: ReviewItemState): ConfirmInvoiceImportItemRequest {
  const hasInstallments = (state.totalInstallments ?? 1) > 1 && state.installmentNumber !== null;
  return {
    id,
    date: state.date || null,
    description: state.description.trim(),
    amount: state.amount,
    categoryId: state.categoryId,
    installmentNumber: hasInstallments ? state.installmentNumber : null,
    totalInstallments: hasInstallments ? state.totalInstallments : null,
  };
}

/** Onde o lançamento aparece no documento, em texto curto. */
export function describeItemSource(
  sourceType: InvoiceImportSourceType,
  item: InvoiceImportItemResponseDto
): string | null {
  const source = item.source;
  if (!source) return null;
  if (sourceType === "Spreadsheet") {
    const parts = [source.sheet ? `aba ${source.sheet}` : null, source.row ? `linha ${source.row}` : null];
    return parts.filter(Boolean).join(", ") || null;
  }
  const parts = [
    sourceType === "Images" && source.fileIndex !== null ? `imagem ${source.fileIndex + 1}` : null,
    source.page ? `página ${source.page}` : null,
  ];
  return parts.filter(Boolean).join(", ") || null;
}

/** Tolerância de dias entre o documento e o cadastro do cartão (fins de semana e feriados). */
export const SCHEDULE_TOLERANCE_DAYS = 3;

function safeDayUtc(year: number, month: number, day: number): number {
  const lastDay = new Date(Date.UTC(year, month, 0)).getUTCDate();
  return Date.UTC(year, month - 1, Math.min(day, lastDay));
}

function dateOnlyUtc(iso: string): number {
  const [y, m, d] = iso.slice(0, 10).split("-").map(Number);
  return Date.UTC(y, m - 1, d);
}

const DAY_MS = 24 * 60 * 60 * 1000;

/**
 * Divergências bloqueantes entre a fatura e o cadastro atual do cartão — mesma conta do
 * CheckCardSchedule do backend (ComputeClosingDate/ComputeDueDate, tolerância de 3 dias),
 * para a tela saber se o ajuste do cartão já resolveu o alerta.
 */
export function blockingScheduleMismatches(
  invoice: CreditCardInvoiceImportResponseDto["invoice"],
  referenceMonth: string | null,
  closingDay: number,
  billingDueDay: number
): InvoiceImportWarningCode[] {
  if (!invoice || !referenceMonth) return [];
  const [year, month] = referenceMonth.split("-").map(Number);
  if (!year || !month) return [];
  const codes: InvoiceImportWarningCode[] = [];
  const exceeds = (documentIso: string, expected: number) =>
    Math.abs(dateOnlyUtc(documentIso) - expected) / DAY_MS > SCHEDULE_TOLERANCE_DAYS;

  if (!invoice.closingDateCalculated && invoice.closingDate) {
    if (exceeds(invoice.closingDate, safeDayUtc(year, month, closingDay)))
      codes.push("ClosingDayMismatch");
  }
  if (invoice.dueDate) {
    const expectedDue =
      billingDueDay > closingDay
        ? safeDayUtc(year, month, billingDueDay)
        : safeDayUtc(month === 12 ? year + 1 : year, month === 12 ? 1 : month + 1, billingDueDay);
    if (exceeds(invoice.dueDate, expectedDue)) codes.push("DueDayMismatch");
  }
  return codes;
}
