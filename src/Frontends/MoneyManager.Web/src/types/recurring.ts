export enum RecurrenceFrequency {
  Daily = "Daily",
  Weekly = "Weekly",
  Biweekly = "Biweekly",
  Monthly = "Monthly",
  Quarterly = "Quarterly",
  Semiannual = "Semiannual",
  Annual = "Annual",
}

export interface RecurringTransactionRequestDto {
  description: string;
  amount: number;
  type: string;
  accountId: string;
  categoryId: string;
  frequency: RecurrenceFrequency;
  startDate: string;
  endDate?: string;
  isActive: boolean;
  notes?: string;
}

export interface RecurringTransactionResponseDto {
  id: string;
  description: string;
  amount: number;
  type: string;
  accountId: string;
  accountName: string;
  categoryId: string;
  categoryName: string;
  frequency: RecurrenceFrequency;
  startDate: string;
  endDate?: string;
  isActive: boolean;
  nextOccurrence?: string;
  lastProcessedDate?: string;
  notes?: string;
  createdAt: string;
  updatedAt: string;
}
