import type { InvoiceStatus } from "@/types/credit-card";

export interface DashboardUpcomingInvoice {
  invoiceId: string;
  creditCardId: string;
  creditCardName: string;
  color: string;
  referenceMonth: string;
  closingDate: string;
  dueDate: string;
  amount: number;
  status: Capitalize<InvoiceStatus>;
  isOverdue: boolean;
  currency: string;
}

export interface DashboardUpcomingRecurring {
  id: string;
  description: string;
  amount: number;
  type: "Income" | "Expense" | "Transfer";
  date: string;
  accountName: string;
  categoryName: string;
  categoryColor: string;
}

export interface DashboardSummary {
  currency: string;
  accountsBalance: number;
  creditCardDebt: number;
  netWorth: number;
  upcomingInvoices: DashboardUpcomingInvoice[];
  upcomingRecurring: DashboardUpcomingRecurring[];
}
