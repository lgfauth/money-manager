export enum InvoiceStatus {
  Open = "Open",
  Closed = "Closed",
  Paid = "Paid",
  PartiallyPaid = "PartiallyPaid",
  Overdue = "Overdue",
}

export interface CreditCardInvoiceResponseDto {
  id: string;
  accountId: string;
  accountName: string;
  referenceMonth: string;
  periodStart: string;
  periodEnd: string;
  dueDate: string;
  totalAmount: number;
  paidAmount: number;
  remainingAmount: number;
  status: InvoiceStatus;
  transactionCount: number;
  daysUntilDue: number;
}

export interface InvoicePaymentRequestDto {
  invoiceId: string;
  payFromAccountId: string;
  paymentDate: string;
  amount: number;
}

export interface CreditCardReconciliationSummaryDto {
  accountsProcessed: number;
  invoicesRecalculated: number;
  accountsUpdated: number;
  totalCommittedCredit: number;
}

export interface AdminActionResponse<T> {
  success: boolean;
  message: string;
  result: T;
}

export interface InvoiceSummaryDto {
  invoice: CreditCardInvoiceResponseDto;
  transactions: import("@/types/transaction").TransactionResponseDto[];
  averageTransactionAmount: number;
  totalTransactions: number;
  amountByCategory: Record<string, number>;
}
