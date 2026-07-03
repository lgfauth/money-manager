"use client";

import { useMemo } from "react";
import { useBankConnections } from "@/hooks/use-bank-connections";

export interface BankSyncInfo {
  bankName: string;
  bankLogo: string | null; // InstitutionLogo da conexão, quando disponível
  status: string; // "Connected" | "Disconnected" | "Error"
  lastSyncAt: string | null;
  connectionId: string;
}

export type BankSyncStatusMap = Record<string, BankSyncInfo>;

/**
 * Mapa de moneyManagerAccountId (conta ou cartão) → informação de sync bancário.
 * Chamar uma vez por página e passar o syncInfo como prop para os cards.
 */
export function useBankSyncStatus(): BankSyncStatusMap {
  const { data: connections } = useBankConnections();

  return useMemo(() => {
    const map: BankSyncStatusMap = {};
    for (const connection of connections ?? []) {
      for (const selected of connection.selectedAccounts) {
        if (!selected.moneyManagerAccountId || !selected.externalAccountId)
          continue;
        map[selected.moneyManagerAccountId] = {
          bankName: selected.bankName || connection.institutionName,
          bankLogo: connection.institutionLogo ?? null,
          status: connection.status,
          lastSyncAt: selected.lastSyncAt ?? connection.lastSyncAt,
          connectionId: connection.id,
        };
      }
    }
    return map;
  }, [connections]);
}
