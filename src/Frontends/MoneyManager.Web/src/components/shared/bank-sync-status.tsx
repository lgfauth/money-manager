"use client";

import Image from "next/image";
import { RefreshCw } from "lucide-react";
import { formatDistanceToNow } from "date-fns";
import { ptBR } from "date-fns/locale";
import { cn } from "@/lib/utils";
import {
  useSyncBank,
  useSyncingBankConnectionIds,
} from "@/hooks/use-bank-connections";
import type { BankSyncInfo } from "@/hooks/use-bank-sync-status";

interface BankSyncStatusProps {
  syncInfo: BankSyncInfo;
  className?: string;
}

export function BankSyncStatus({ syncInfo, className }: BankSyncStatusProps) {
  const syncMutation = useSyncBank();
  const syncingConnectionIds = useSyncingBankConnectionIds();
  const isSyncing = syncingConnectionIds.has(syncInfo.connectionId);

  return (
    <div
      className={cn(
        "flex w-full items-center justify-between gap-2",
        className
      )}
    >
      <div className="flex items-center gap-1.5 text-xs text-muted-foreground min-w-0">
        {syncInfo.bankLogo ? (
          <Image
            src={syncInfo.bankLogo}
            width={16}
            height={16}
            className="h-4 w-4 rounded-sm object-contain shrink-0"
            alt=""
          />
        ) : (
          <span className="h-4 w-4 rounded-sm bg-primary/20 flex items-center justify-center text-[9px] font-bold text-primary shrink-0">
            {syncInfo.bankName.charAt(0).toUpperCase()}
          </span>
        )}
        <span className="truncate">
          {isSyncing
            ? "Sincronizando..."
            : syncInfo.lastSyncAt
            ? `Atualizado ${formatDistanceToNow(new Date(syncInfo.lastSyncAt), {
                addSuffix: true,
                locale: ptBR,
              })}`
            : "Aguardando sync"}
        </span>
      </div>
      <button
        type="button"
        onClick={() => syncMutation.mutate(syncInfo.connectionId)}
        disabled={isSyncing}
        className="h-7 w-7 rounded-md border flex items-center justify-center hover:bg-muted transition-colors disabled:opacity-50 shrink-0"
        aria-label="Sincronizar agora"
      >
        <RefreshCw
          className={cn("h-3.5 w-3.5", isSyncing && "animate-spin")}
        />
      </button>
    </div>
  );
}
