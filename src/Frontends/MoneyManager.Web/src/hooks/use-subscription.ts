"use client";

import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { queryKeys } from "@/lib/query-client";

interface SubscriptionDto {
  id: string;
  plan: string;
  status: string;
  isPremiumActive: boolean;
  trialEndsAt: string | null;
  currentPeriodEnd: string | null;
}

export function useSubscription() {
  return useQuery({
    queryKey: queryKeys.subscription,
    queryFn: () => apiClient.get<SubscriptionDto>("/api/subscriptions/me"),
    staleTime: 5 * 60 * 1000,
  });
}

export function useIsPremium() {
  const { data } = useSubscription();
  return data?.isPremiumActive ?? false;
}
