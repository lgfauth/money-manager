export interface AdminUserSubscriptionDto {
  userId: string;
  name: string;
  email: string;
  plan: string;
  status: string;
  isPremiumActive: boolean;
  trialEndsAt: string | null;
  currentPeriodEnd: string | null;
  paymentProvider: string | null;
  userCreatedAt: string;
}
