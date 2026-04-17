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
  creditCards: ["credit-cards"] as const,
  creditCard: (id: string) => ["credit-cards", id] as const,
  creditCardInvoices: (cardId: string) =>
    ["credit-cards", cardId, "invoices"] as const,
  creditCardInvoice: (cardId: string, invoiceId: string) =>
    ["credit-cards", cardId, "invoices", invoiceId] as const,
  creditCardTransactions: (cardId?: string) =>
    cardId
      ? (["credit-card-transactions", cardId] as const)
      : (["credit-card-transactions"] as const),
  recurring: ["recurring"] as const,
  dashboard: (month: string) => ["dashboard", month] as const,
  reports: (filters: { startDate: string; endDate: string }) =>
    ["reports", filters] as const,
  profile: ["profile"] as const,
  settings: ["settings"] as const,
  dataCount: ["accountdeletion", "data-count"] as const,
};
