export type InvoiceStatus = "pending" | "open" | "closed" | "paid" | "overdue";

export interface CreditCardInvoiceSummaryDto {
  id: string;
  referenceMonth: string;
  closingDate: string;
  dueDate: string;
  totalAmount: number;
  status: InvoiceStatus;
}

export interface CreditCardResponseDto {
  id: string;
  name: string;
  limit: number;
  currentBalance: number;
  availableLimit: number;
  closingDay: number;
  billingDueDay: number;
  bestPurchaseDay: number;
  color: string;
  currency: string;
  currentInvoice: CreditCardInvoiceSummaryDto | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreditCardInvoiceResponseDto {
  id: string;
  creditCardId: string;
  creditCardName: string;
  referenceMonth: string;
  closingDate: string;
  dueDate: string;
  status: InvoiceStatus;
  totalAmount: number;
  paidAt: string | null;
  paidWithAccountId: string | null;
  paidAmount: number | null;
  currency: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreditCardTransactionResponseDto {
  id: string;
  creditCardId: string;
  invoiceId: string;
  description: string;
  categoryId: string | null;
  categoryName: string;
  categoryColor: string;
  purchaseDate: string;
  totalAmount: number;
  installmentAmount: number;
  installmentNumber: number;
  totalInstallments: number;
  parentTransactionId: string | null;
  currency: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreditCardInvoiceDetailResponseDto {
  invoice: CreditCardInvoiceResponseDto;
  transactions: CreditCardTransactionResponseDto[];
}

export interface CreateCreditCardRequest {
  name: string;
  limit: number;
  closingDay: number;
  billingDueDay: number;
  bestPurchaseDay?: number;
  color: string;
  currency: string;
}

export interface CreateCreditCardTransactionRequest {
  creditCardId: string;
  description: string;
  categoryId?: string;
  purchaseDate: string;
  totalAmount: number;
  totalInstallments: number;
  firstInstallmentOnCurrentInvoice: boolean;
  clientRequestId?: string;
}

export interface PayCreditCardInvoiceRequest {
  paidWithAccountId: string;
  paidAmount: number;
  paidAt: string;
}
