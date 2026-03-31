export interface ApiError {
  message: string;
  statusCode: number;
}

export interface LoginRequestDto {
  email: string;
  password: string;
}

export interface LoginResponseDto {
  token: string;
}

export interface RegisterRequestDto {
  name: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface DataCountDto {
  totalAccounts: number;
  totalTransactions: number;
  totalCategories: number;
  totalBudgets: number;
  totalRecurringTransactions: number;
}

export interface DeleteAccountRequestDto {
  password: string;
  confirmationText: string;
}
