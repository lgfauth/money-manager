import { QueryClient } from "@tanstack/react-query";
import type { TransactionFilters } from "@/types/transaction";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000,
      gcTime: 10 * 60 * 1000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

export const queryKeys = {
  accounts: ["accounts"] as const,
  account: (id: string) => ["accounts", id] as const,
  categories: ["categories"] as const,
  transactions: (filters: TransactionFilters) =>
    ["transactions", filters] as const,
  budgets: (month: string) => ["budgets", month] as const,
  invoices: (accountId: string) => ["invoices", accountId] as const,
  invoice: (id: string) => ["invoices", "detail", id] as const,
  invoiceSummary: (id: string) => ["invoices", "summary", id] as const,
  invoiceTransactions: (id: string) =>
    ["invoices", "transactions", id] as const,
  pendingInvoices: ["invoices", "pending"] as const,
  overdueInvoices: ["invoices", "overdue"] as const,
  recurring: ["recurring"] as const,
  dashboard: (month: string) => ["dashboard", month] as const,
  reports: (filters: { startDate: string; endDate: string }) =>
    ["reports", filters] as const,
  profile: ["profile"] as const,
  settings: ["settings"] as const,
  dataCount: ["accountdeletion", "data-count"] as const,
};
