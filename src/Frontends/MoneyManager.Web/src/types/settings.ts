export interface UserSettingsDto {
  currency: string;
  dateFormat: string;
  monthClosingDay: number;
  defaultBudget?: number;
  theme: string;
  primaryColor: string;
}
