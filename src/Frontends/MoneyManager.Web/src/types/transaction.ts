export enum TransactionType {
  Income = "Income",
  Expense = "Expense",
  Investment = "Investment",
}

export interface TransactionRequestDto {
  description: string;
  amount: number;
  date: string;
  type: TransactionType;
  accountId: string;
  categoryId: string;
  notes?: string;
  clientRequestId?: string;
}

export interface InstallmentPurchaseRequestDto {
  description: string;
  totalAmount: number;
  installmentCount: number;
  firstInstallmentInCurrentInvoice: boolean;
  date: string;
  type: TransactionType;
  accountId: string;
  categoryId: string;
  notes?: string;
  clientRequestId?: string;
}

export interface TransactionResponseDto {
  id: string;
  description: string;
  amount: number;
  currency: string;
  date: string;
  type: TransactionType;
  accountId: string;
  accountName: string;
  accountColor: string;
  categoryId: string;
  categoryName: string;
  categoryColor: string;
  notes?: string;
  installmentGroupId?: string;
  installmentNumber?: number;
  installmentCount?: number;
  createdAt: string;
  updatedAt: string;
}

export interface TransactionFilters {
  page: number;
  pageSize: number;
  sortBy: string;
  startDate?: string;
  endDate?: string;
  type?: string;
  accountId?: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
