export enum AccountType {
  Checking = "Checking",
  Savings = "Savings",
  Cash = "Cash",
  CreditCard = "CreditCard",
}

export interface AccountRequestDto {
  name: string;
  type: AccountType;
  initialBalance: number;
  currency: string;
  color: string;
}

export interface AccountResponseDto {
  id: string;
  name: string;
  type: AccountType;
  balance: number;
  currency: string;
  color: string;
  createdAt: string;
  updatedAt: string;
}
