// Espelho dos DTOs de importação de fatura de cartão por IA
// (CreditCardInvoiceImportResponseDto e afins). Enums chegam como texto em PascalCase.

export type InvoiceImportStatus =
  | "Queued"
  | "Processing"
  | "AwaitingReview"
  | "Failed"
  | "Confirming"
  | "Confirmed"
  | "Discarded";

export type InvoiceImportSourceType = "Pdf" | "Images" | "Spreadsheet";

export type InvoiceImportHolderType = "Unknown" | "Primary" | "Additional";

export type InvoiceImportCardType = "Unknown" | "Physical" | "Virtual";

export type InvoiceImportItemType = "Purchase" | "Refund";

export type InvoiceImportWarningCode =
  // Alertas de item
  | "MissingDate"
  | "MissingDescription"
  | "InvalidAmount"
  | "FutureDate"
  | "OutOfAllowedPeriod"
  | "PossibleDuplicate"
  | "AlreadyImported"
  | "AlreadyRegistered"
  | "PreviousInstallmentsNotRegistered"
  | "NotFoundInSpreadsheet"
  | "LowConfidence"
  // Alertas gerais da importação
  | "ClosingDateCalculated"
  | "ClosingDayMismatch"
  | "DueDayMismatch"
  | "InvoiceAlreadyImported"
  | "UnreadableContent"
  | "CellTruncated";

export interface InvoiceImportCreatedResponseDto {
  id: string;
  status: InvoiceImportStatus;
}

export interface InvoiceImportEligibleCardResponseDto {
  id: string;
  name: string;
  brand: string | null;
  color: string;
  closingDay: number;
  billingDueDay: number;
}

export interface InvoiceImportDocumentInfoResponseDto {
  issuer: string | null;
  cardLastDigits: string | null;
  closingDate: string | null;
  closingDateCalculated: boolean;
  dueDate: string | null;
  referenceMonth: string | null;
  totalAmount: number | null;
  isOpenOrPartial: boolean;
}

export interface InvoiceImportHolderResponseDto {
  name: string;
  type: InvoiceImportHolderType;
  cardLastDigits: string[];
  itemCount: number;
  totalAmount: number;
  isSuggested: boolean;
}

export interface InvoiceImportWarningResponseDto {
  code: InvoiceImportWarningCode;
  message: string;
  isBlocking: boolean;
}

export interface InvoiceImportItemWarningResponseDto {
  code: InvoiceImportWarningCode;
  isBlocking: boolean;
}

export interface InvoiceImportSourceResponseDto {
  fileIndex: number | null;
  page: number | null;
  sheet: string | null;
  row: number | null;
}

export interface InvoiceImportItemResponseDto {
  id: string;
  date: string | null;
  description: string;
  amount: number;
  type: InvoiceImportItemType;
  installmentNumber: number | null;
  totalInstallments: number | null;
  holderName: string | null;
  holderType: InvoiceImportHolderType;
  cardLastDigits: string | null;
  cardType: InvoiceImportCardType;
  source: InvoiceImportSourceResponseDto | null;
  categoryHint: string | null;
  categoryId: string | null;
  confidence: number | null;
  notes: string | null;
  warnings: InvoiceImportItemWarningResponseDto[];
  isSelectedByDefault: boolean;
}

export interface InvoiceImportConfirmationResultResponseDto {
  createdCount: number;
  skippedCount: number;
  affectedInvoiceIds: string[];
}

export interface CreditCardInvoiceImportResponseDto {
  id: string;
  creditCardId: string;
  status: InvoiceImportStatus;
  sourceType: InvoiceImportSourceType;
  fileCount: number;
  errorMessage: string | null;
  referenceMonth: string | null;
  invoice: InvoiceImportDocumentInfoResponseDto | null;
  holders: InvoiceImportHolderResponseDto[];
  warnings: InvoiceImportWarningResponseDto[];
  items: InvoiceImportItemResponseDto[];
  selectedHolders: string[];
  result: InvoiceImportConfirmationResultResponseDto | null;
  createdAt: string;
  updatedAt: string;
  processedAt: string | null;
  confirmedAt: string | null;
  version: number;
}

export interface ConfirmInvoiceImportItemRequest {
  id: string;
  date: string | null;
  description: string;
  /** Valor da parcela (positivo, inclusive para estorno). */
  amount: number;
  categoryId: string | null;
  installmentNumber: number | null;
  totalInstallments: number | null;
}

export interface ConfirmInvoiceImportRequest {
  selectedHolders: string[];
  items: ConfirmInvoiceImportItemRequest[];
}
