export interface BankMcpConnectionDto {
  itemId: string;
  connectorId: string;
  connectorName: string;
  status: string; // "UPDATED" | "LOGIN_ERROR" | "WAITING_USER_INPUT"
  alreadyRegistered: boolean;
}

export interface BankMcpAvailableConnectionsResponseDto {
  connections: BankMcpConnectionDto[];
  addConnectionUrl: string;
}

export interface BankMcpAccountDto {
  accountId: string;
  type: string; // "BANK" | "CREDIT"
  subtype: string;
  displayName: string;
  number: string;
  balance: number;
}

export interface SelectedBankAccountDto {
  externalAccountId: string;
  bankName: string;
  type: string;
  subtype: string;
  number: string;
  moneyManagerAccountId: string | null;
  lastSyncAt: string | null;
}

export interface BankConnectionDto {
  id: string;
  institutionName: string;
  status: string; // "Connected" | "Disconnected" | "Error"
  connectedAt: string | null;
  lastSyncAt: string | null;
  selectedAccounts: SelectedBankAccountDto[];
}

export interface BankMcpUserInviteResponseDto {
  connectUrl: string;
}

export type OnboardingStrategy = "CleanSlate" | "Coexistence";

export interface AccountMappingDto {
  externalAccountId: string;
  externalAccountType: string;
  externalAccountSubtype: string;
  externalAccountNumber: string;
  bankName: string;
  moneyManagerAccountId: string;
}

export interface CompleteOnboardingRequestDto {
  accountMappings: AccountMappingDto[];
  strategy: OnboardingStrategy;
  customCutoffDate?: string; // ISO date string, só em Coexistence
}
