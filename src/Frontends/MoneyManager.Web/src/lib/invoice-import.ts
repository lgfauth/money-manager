import { ApiClientError, getApiErrorMessage } from "@/lib/api-errors";
import type {
  InvoiceImportSourceType,
  InvoiceImportStatus,
} from "@/types/invoice-import";

// Textos e regras de exibição da importação de fatura, centralizados para não divergirem
// entre telas. As regras de negócio continuam no backend; aqui só orientamos o usuário.

/** Rota da tela de uma importação — a mesma URL enviada no push pelo Worker. */
export const invoiceImportHref = (importId: string) =>
  `/credit-cards/invoice-imports/${importId}`;

/** Estados em que o job ainda muda sozinho e a tela deve consultar a API periodicamente. */
export const ACTIVE_INVOICE_IMPORT_STATUSES: readonly InvoiceImportStatus[] = [
  "Queued",
  "Processing",
  "Confirming",
];

export const isActiveInvoiceImport = (status: InvoiceImportStatus | undefined) =>
  !!status && ACTIVE_INVOICE_IMPORT_STATUSES.includes(status);

/** Intervalo de consulta da tela da importação enquanto o job está ativo. */
export const INVOICE_IMPORT_DETAIL_POLL_MS = 5_000;

/** Tempo na fila a partir do qual a tela orienta o usuário (sem prometer prazo). */
export const INVOICE_IMPORT_LONG_QUEUE_MS = 10 * 60 * 1000;

export const invoiceImportStatusLabels: Record<InvoiceImportStatus, string> = {
  Queued: "Na fila",
  Processing: "Lendo a fatura",
  AwaitingReview: "Aguardando revisão",
  Failed: "Falhou",
  Confirming: "Cadastrando",
  Confirmed: "Importada",
  Discarded: "Descartada",
};

export const invoiceImportSourceLabels: Record<InvoiceImportSourceType, string> = {
  Pdf: "PDF",
  Images: "Imagens",
  Spreadsheet: "Planilha",
};

export function describeInvoiceImportFiles(
  sourceType: InvoiceImportSourceType,
  fileCount: number
): string {
  switch (sourceType) {
    case "Pdf":
      return "1 arquivo PDF";
    case "Spreadsheet":
      return "1 planilha .xlsx";
    case "Images":
      return fileCount === 1 ? "1 imagem" : `${fileCount} imagens`;
  }
}

/**
 * Formata uma data sem horário vinda da API (ex.: "2026-10-05T00:00:00Z") pelo calendário,
 * sem converter fuso — senão a meia-noite UTC vira o dia anterior no horário de Brasília.
 */
export function formatDateOnly(iso: string | null | undefined): string {
  if (!iso) return "—";
  const [year, month, day] = iso.slice(0, 10).split("-").map(Number);
  if (!year || !month || !day) return "—";
  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(new Date(year, month - 1, day));
}

/** Data e hora de um instante (ex.: envio da importação), no fuso do navegador. */
export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return "—";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "—";
  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}

/** "2026-10" → "outubro de 2026". */
export function formatReferenceMonth(ref: string | null | undefined): string {
  if (!ref) return "—";
  const [year, month] = ref.split("-").map(Number);
  if (!year || !month) return ref;
  return new Intl.DateTimeFormat("pt-BR", { month: "long", year: "numeric" }).format(
    new Date(year, month - 1, 1)
  );
}

/** Tempo decorrido em texto curto: "agora há pouco", "há 3 min", "há 2 h". */
export function formatElapsed(fromIso: string, now: number): string {
  const elapsedMs = Math.max(0, now - new Date(fromIso).getTime());
  const minutes = Math.floor(elapsedMs / 60_000);
  if (minutes < 1) return "agora há pouco";
  if (minutes < 60) return `há ${minutes} min`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `há ${hours} h`;
  const days = Math.floor(hours / 24);
  return days === 1 ? "há 1 dia" : `há ${days} dias`;
}

// ── Envio da fatura ──────────────────────────────────────────────────────────────────────────
// Espelho de CreateInvoiceImportValidator (limites e mensagens). O servidor continua sendo a
// validação definitiva; aqui só evitamos um envio que certamente seria recusado.

/** Tela de envio, com o cartão pré-selecionado quando informado. */
export const newInvoiceImportHref = (cardId?: string) =>
  cardId
    ? `/credit-cards/invoice-imports/new?cardId=${encodeURIComponent(cardId)}`
    : "/credit-cards/invoice-imports/new";

const MB = 1024 * 1024;

export const INVOICE_IMPORT_UPLOAD_LIMITS = {
  maxPdfBytes: 20 * MB,
  maxImages: 10,
  maxImageBytes: 10 * MB,
  maxTotalImageBytes: 40 * MB,
  maxSpreadsheetBytes: 5 * MB,
} as const;

export const INVOICE_IMPORT_UPLOAD_MESSAGES = {
  noFiles: "Envie a fatura em PDF ou em imagens.",
  emptyFile: "Um dos arquivos enviados está vazio.",
  unsupportedType:
    "Tipo de arquivo não suportado. Envie a fatura em PDF, em imagens JPEG, PNG ou WebP, ou em planilha .xlsx.",
  exportAsXlsx:
    "Envie a planilha no formato .xlsx. Arquivos .xls, .xlsm, .csv ou planilhas protegidas por senha não são aceitos: abra o arquivo no Excel e salve como .xlsx, sem senha.",
  heic:
    "Fotos em HEIC (padrão do iPhone) não são aceitas. Tire um print da tela, ou em Ajustes › Câmera › Formatos escolha \"Mais compatível\" para salvar as fotos em JPEG.",
  mixedTypes:
    "Envie um único PDF, uma única planilha ou só imagens, sem misturar os tipos de arquivo.",
  singleSpreadsheet: "Envie apenas uma planilha por importação.",
  spreadsheetTooLarge: "A planilha excede o tamanho máximo de 5 MB.",
  singlePdf: "Envie apenas um PDF por importação.",
  pdfTooLarge: "O PDF excede o tamanho máximo de 20 MB.",
  tooManyImages: "Envie no máximo 10 imagens por importação.",
  imageTooLarge: "Cada imagem pode ter no máximo 10 MB.",
  imagesTooLarge: "As imagens somam mais de 40 MB.",
} as const;

/** Valor do atributo `accept` do seletor de arquivos (inclui .xlsx enviado como octet-stream). */
export const INVOICE_IMPORT_ACCEPT =
  "application/pdf,.pdf,image/jpeg,image/png,image/webp,.jpg,.jpeg,.png,.webp," +
  "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,.xlsx";

const IMAGE_TYPES = new Set(["image/jpeg", "image/png", "image/webp"]);
const XLSX_TYPE = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
const LEGACY_SPREADSHEET_TYPES = new Set([
  "application/vnd.ms-excel",
  "application/vnd.ms-excel.sheet.macroenabled.12",
  "application/vnd.ms-excel.sheet.binary.macroenabled.12",
  "text/csv",
  "application/csv",
]);
const LEGACY_SPREADSHEET_EXTENSIONS = [".xls", ".xlsm", ".xlsb", ".csv"];

type UploadFileKind = InvoiceImportSourceType | "Legacy" | "Heic" | "Unsupported";

const normalizedType = (file: File) => (file.type ?? "").split(";")[0].trim().toLowerCase();
const hasExtension = (file: File, ...extensions: string[]) =>
  extensions.some((ext) => file.name.toLowerCase().endsWith(ext));

/** Classifica o arquivo como o servidor faria (tipo declarado e, para .xlsx, a extensão). */
export function classifyInvoiceImportFile(file: File): UploadFileKind {
  const type = normalizedType(file);
  const isXlsx = hasExtension(file, ".xlsx");

  if (!isXlsx && (LEGACY_SPREADSHEET_TYPES.has(type) || hasExtension(file, ...LEGACY_SPREADSHEET_EXTENSIONS)))
    return "Legacy";
  if (type === "application/pdf") return "Pdf";
  if (IMAGE_TYPES.has(type)) return "Images";
  if (type === XLSX_TYPE) return "Spreadsheet";
  // Alguns sistemas mandam .xlsx sem tipo, como octet-stream ou com o tipo do Excel antigo
  if (isXlsx && (type === "" || type === "application/octet-stream" || type === "application/vnd.ms-excel"))
    return "Spreadsheet";
  if (type === "image/heic" || type === "image/heif" || hasExtension(file, ".heic", ".heif"))
    return "Heic";
  // Navegadores que não informam o tipo: decide pela extensão (o servidor confere a assinatura)
  if (type === "") {
    if (hasExtension(file, ".pdf")) return "Pdf";
    if (hasExtension(file, ".jpg", ".jpeg", ".png", ".webp")) return "Images";
  }
  return "Unsupported";
}

const EXTENSION_CONTENT_TYPES: Record<string, string> = {
  ".pdf": "application/pdf",
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".png": "image/png",
  ".webp": "image/webp",
  ".xlsx": XLSX_TYPE,
};

/**
 * Arquivo pronto para o envio: quando o navegador não informa o tipo, usa o da extensão —
 * sem isso ele seguiria como `application/octet-stream` e o servidor recusaria PDF e imagens.
 */
export function withInferredContentType(file: File): File {
  if (normalizedType(file) !== "") return file;
  const ext = Object.keys(EXTENSION_CONTENT_TYPES).find((e) => hasExtension(file, e));
  return ext ? new File([file], file.name, { type: EXTENSION_CONTENT_TYPES[ext], lastModified: file.lastModified }) : file;
}

/** Tipo de origem do lote, quando todos os arquivos são do mesmo tipo aceito. */
export function resolveInvoiceImportSourceType(files: File[]): InvoiceImportSourceType | null {
  const kinds = new Set(files.map(classifyInvoiceImportFile));
  if (kinds.size !== 1) return null;
  const [kind] = kinds;
  return kind === "Pdf" || kind === "Images" || kind === "Spreadsheet" ? kind : null;
}

/**
 * Primeira regra de envio violada pelos arquivos, com a mesma mensagem do servidor, ou `null`
 * se o lote pode ser enviado.
 */
export function validateInvoiceImportFiles(files: File[]): string | null {
  const m = INVOICE_IMPORT_UPLOAD_MESSAGES;
  const limits = INVOICE_IMPORT_UPLOAD_LIMITS;

  if (files.length === 0) return m.noFiles;
  if (files.some((f) => f.size <= 0)) return m.emptyFile;

  const kinds = files.map(classifyInvoiceImportFile);
  if (kinds.includes("Legacy")) return m.exportAsXlsx;
  if (kinds.includes("Heic")) return m.heic;
  if (kinds.includes("Unsupported")) return m.unsupportedType;

  const sourceType = resolveInvoiceImportSourceType(files);
  if (!sourceType) return m.mixedTypes;

  switch (sourceType) {
    case "Spreadsheet":
      if (files.length > 1) return m.singleSpreadsheet;
      if (files[0].size > limits.maxSpreadsheetBytes) return m.spreadsheetTooLarge;
      return null;
    case "Pdf":
      if (files.length > 1) return m.singlePdf;
      if (files[0].size > limits.maxPdfBytes) return m.pdfTooLarge;
      return null;
    case "Images":
      if (files.length > limits.maxImages) return m.tooManyImages;
      if (files.some((f) => f.size > limits.maxImageBytes)) return m.imageTooLarge;
      if (files.reduce((sum, f) => sum + f.size, 0) > limits.maxTotalImageBytes)
        return m.imagesTooLarge;
      return null;
  }
}

/** Tamanho de arquivo em texto curto: "820 KB", "3,4 MB". */
export function formatFileSize(bytes: number): string {
  if (bytes < MB) return `${Math.max(1, Math.round(bytes / 1024))} KB`;
  return `${(bytes / MB).toLocaleString("pt-BR", { maximumFractionDigits: 1 })} MB`;
}

/** Mensagem de erro do envio por status HTTP, quando a API não traz uma própria. */
export const INVOICE_IMPORT_UPLOAD_ERROR_FALLBACKS: Record<number, string> = {
  400: "Os arquivos da fatura foram recusados. Confira o formato e tente de novo.",
  404: "Cartão não encontrado. Escolha outro cartão.",
  409: "Não foi possível enviar a fatura deste cartão agora.",
  413: "Os arquivos são grandes demais para envio. Envie no máximo 20 MB (PDF), 40 MB (imagens) ou 5 MB (planilha).",
  503: "A leitura de imagens está indisponível no momento. Tente novamente em alguns minutos ou envie a fatura em PDF.",
};

export const INVOICE_IMPORT_NETWORK_ERROR =
  "Não foi possível enviar: verifique sua conexão com a internet e tente de novo. Os arquivos continuam selecionados.";

/**
 * Resumo e lista de problemas do ApiErrorResponse ({ message, errors }), sem os demais campos
 * (traceId, path, horário) que getApiErrorMessages também recolhe.
 */
export function readInvoiceImportApiError(
  error: unknown,
  fallbackMessage: string
): { message: string; errors: string[] } {
  if (error instanceof ApiClientError && error.data && typeof error.data === "object") {
    const data = error.data as { message?: unknown; errors?: unknown };
    const message = typeof data.message === "string" && data.message ? data.message : null;
    const errors = Array.isArray(data.errors)
      ? data.errors.filter((e): e is string => typeof e === "string" && e.trim() !== "")
      : [];
    if (message) return { message, errors };
  }
  return { message: getApiErrorMessage(error, fallbackMessage), errors: [] };
}
