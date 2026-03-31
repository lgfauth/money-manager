export enum AccountType {
  Checking = "Checking",
  Savings = "Savings",
  Cash = "Cash",
  CreditCard = "CreditCard",
  Investment = "Investment",
}

export interface AccountRequestDto {
  name: string;
  type: AccountType;
  balance: number;
  currency: string;
  color: string;
  invoiceClosingDay?: number;
  invoiceDueDayOffset?: number;
  creditLimit?: number;
}

export interface AccountResponseDto {
  id: string;
  name: string;
  type: AccountType;
  balance: number;
  currency: string;
  color: string;
  invoiceClosingDay?: number;
  invoiceDueDayOffset?: number;
  creditLimit?: number;
  createdAt: string;
  updatedAt: string;
}
