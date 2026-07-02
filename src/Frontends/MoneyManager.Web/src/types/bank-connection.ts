export interface BankMcpConnectionDto {
  itemId: string;
  connectorId: string;
  connectorName: string;
  status: string; // "UPDATED" | "LOGIN_ERROR" | "WAITING_USER_INPUT"
  alreadyRegistered: boolean;
  pendingSetup: boolean;
  pendingConnectionId: string | null;
}

export interface BankMcpAvailableConnectionsResponseDto {
  hasApiKey: boolean;
  apiKeyExpired: boolean;
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
  moneyManagerEntityType: "Account" | "CreditCard";
  lastSyncAt: string | null;
}

export interface BankConnectionDto {
  id: string;
  institutionName: string;
  institutionLogo?: string | null; // não fornecido pela API hoje — reservado para logo do banco

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
  externalAccountType: string;
  externalAccountSubtype: string;
  externalAccountNumber: string;
  bankName: string;
  moneyManagerAccountId: string;
  moneyManagerEntityType: "Account" | "CreditCard";
}

export interface CompleteOnboardingRequestDto {
  accountMappings: AccountMappingDto[];
  strategy: OnboardingStrategy;
  customCutoffDate?: string; // ISO date string, só em Coexistence
}
