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
  // Chaves sob "dashboard" são invalidadas por invalidateFinancialSummaries
  dashboardOverview: (month: string) => ["dashboard", "overview", month] as const,
  dashboardSummary: ["dashboard", "summary"] as const,
  dashboardRecentTransactions: (today: string) => ["dashboard", "recent-transactions", today] as const,
  reports: (filters: { startDate: string; endDate: string; basis: string }) =>
    ["reports", filters] as const,
  profile: ["profile"] as const,
  settings: ["settings"] as const,
  dataCount: ["accountdeletion", "data-count"] as const,
  financialHealthSettings: ["financial-health", "settings"] as const,
  financialHealthBuckets: ["financial-health", "buckets"] as const,
  financialHealthSnapshotStatus: ["financial-health", "snapshots", "current"] as const,
  financialHealthSnapshotHistory: (year: number) =>
    ["financial-health", "snapshots", year] as const,
  financialHealthScore: ["financial-health", "score"] as const,
  subscription: ["subscription"] as const,
  bankConnections: ["bank-connections"] as const,
  bankConnectionInvite: ["bank-connections", "invite"] as const,
  bankConnectionsAvailable: ["bank-connections", "available"] as const,
  bankConnectionAccounts: (id: string) =>
    ["bank-connections", id, "accounts"] as const,
};

// Dashboard e relatórios agregam transações, contas, cartões, categorias, orçamentos e
// recorrências: qualquer alteração nesses dados precisa refazer os resumos.
export function invalidateFinancialSummaries(client: QueryClient) {
  client.invalidateQueries({ queryKey: ["dashboard"] });
  client.invalidateQueries({ queryKey: ["reports"] });
}
