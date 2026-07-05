"use client";

import { useMemo } from "react";
import { useAccounts } from "@/hooks/use-accounts";
import { useCreditCards } from "@/hooks/use-credit-cards";
import { useBankSyncStatus } from "@/hooks/use-bank-sync-status";

export function useUnlinkedTargets() {
  const { data: accounts = [] } = useAccounts();
  const { data: creditCards = [] } = useCreditCards();
  const syncStatus = useBankSyncStatus();

  return useMemo(() => {
    const linkedEntityIds = new Set(Object.keys(syncStatus));
    const unlinkedAccounts = accounts.filter(
      (account) => !linkedEntityIds.has(account.id)
    );
    const unlinkedCards = creditCards.filter(
      (creditCard) => !linkedEntityIds.has(creditCard.id)
    );

    return {
      unlinkedAccounts,
      unlinkedCards,
      hasAnyManualTarget:
        unlinkedAccounts.length > 0 || unlinkedCards.length > 0,
    };
  }, [accounts, creditCards, syncStatus]);
}