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
  sourceAccountId: string;
  paymentDate: string;
  amount: number;
}

export interface InvoiceSummaryDto {
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
