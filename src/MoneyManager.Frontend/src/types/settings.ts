export interface UserSettingsDto {
  currency: string;
  dateFormat: string;
  monthClosingDay: number;
  defaultBudget?: number;
  pushNotificationsEnabled: boolean;
  notifyRecurringProcessed: boolean;
  notifyDailyReminder: boolean;
  theme: string;
  primaryColor: string;
}
