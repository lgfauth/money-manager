export interface BankMcpConnectionDto {
  externalConnectionId: string;
  institutionName: string;
  institutionLogo: string | null;
  status: string; // "UPDATED" | "LOGIN_ERROR" | "WAITING_USER_INPUT"
  alreadyRegistered: boolean;
}

export interface BankMcpAccountDto {
  externalAccountId: string;
  name: string;
  type: string; // "BANK" | "CREDIT"
  balance: number;
}

export interface SelectedBankAccountDto {
  externalAccountId: string;
  name: string;
  type: string;
  moneyManagerAccountId: string | null;
  lastSyncAt: string | null;
}

export interface BankConnectionDto {
  id: string;
  institutionName: string;
  institutionLogo: string | null;
  status: string; // "Connected" | "Disconnected" | "Error"
  connectedAt: string | null;
  lastSyncAt: string | null;
  selectedAccounts: SelectedBankAccountDto[];
}

export interface SaveApiKeyResultDto {
  isValid: boolean;
  availableConnections: number;
}

export type OnboardingStrategy = "CleanSlate" | "Coexistence";

export interface AccountMappingDto {
  externalAccountId: string;
  externalAccountName: string;
  externalAccountType: string;
  moneyManagerAccountId: string;
}

export interface CompleteOnboardingRequestDto {
  accountMappings: AccountMappingDto[];
  strategy: OnboardingStrategy;
  customCutoffDate?: string; // ISO date string, só em Coexistence
}
