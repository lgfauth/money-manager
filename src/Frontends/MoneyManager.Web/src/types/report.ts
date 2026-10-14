export type ReportBasis = "Accrual" | "Cash";

export interface ReportPeriod {
  startDate: string;
  endDate: string;
}

export interface ReportTotals {
  income: number;
  expense: number;
  netBalance: number;
  savingsRate: number | null;
}

export interface ReportCategoryBreakdown {
  categoryId: string;
  categoryName: string;
  color: string;
  total: number;
  percentage: number;
  previousTotal: number;
  monthlyAverage: number;
  budgetLimit: number | null;
  transactionCount: number;
}

export interface ReportMonthlyTrend {
  month: string;
  income: number;
  expense: number;
  netBalance: number;
}

export interface ReportDailyPoint {
  date: string;
  balance: number;
  income: number;
  expense: number;
}

export interface ReportAccountBreakdown {
  accountId: string;
  accountName: string;
  color: string;
  income: number;
  expense: number;
}

export interface ReportCreditCardBreakdown {
  creditCardId: string;
  creditCardName: string;
  color: string;
  total: number;
  transactionCount: number;
}

export interface ReportTopExpense {
  description: string;
  date: string;
  amount: number;
  categoryName: string;
  categoryColor: string;
  sourceName: string;
  sourceType: "account" | "credit_card";
  installmentLabel: string | null;
}

export interface ReportUpcomingInstallment {
  month: string;
  total: number;
  installmentCount: number;
}

export interface ReportCurrencyTotals {
  currency: string;
  income: number;
  expense: number;
}

export interface ReportDataQuality {
  uncategorizedCount: number;
  uncategorizedTotal: number;
  pendingCardTransactionsCount: number;
  excludedTransfersCount: number;
  excludedInvoicePaymentsCount: number;
  otherCurrencyEntriesCount: number;
}

export interface ReportOverview {
  basis: ReportBasis;
  currency: string;
  period: ReportPeriod;
  previousPeriod: ReportPeriod;
  totals: ReportTotals;
  previousTotals: ReportTotals;
  expensesByCategory: ReportCategoryBreakdown[];
  incomesByCategory: ReportCategoryBreakdown[];
  monthlyTrends: ReportMonthlyTrend[];
  dailyPoints: ReportDailyPoint[];
  byAccount: ReportAccountBreakdown[];
  byCreditCard: ReportCreditCardBreakdown[];
  topExpenses: ReportTopExpense[];
  upcomingInstallments: ReportUpcomingInstallment[];
  otherCurrencies: ReportCurrencyTotals[];
  dataQuality: ReportDataQuality;
}
