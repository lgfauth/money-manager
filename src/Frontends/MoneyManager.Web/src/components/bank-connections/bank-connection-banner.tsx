"use client";

import { useState } from "react";
import { Building2, ArrowRight, RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useIsPremium } from "@/hooks/use-subscription";
import { useBankConnections } from "@/hooks/use-bank-connections";
import { BankSetupSheet } from "./bank-setup-sheet";
import { formatDistanceToNow } from "date-fns";
import { ptBR } from "date-fns/locale";

export function BankConnectionBanner() {
  const isPremium = useIsPremium();
  const { data: connections = [] } = useBankConnections();
  const [setupOpen, setSetupOpen] = useState(false);

  if (!isPremium) return null;

  const activeConnections = connections.filter((c) => c.status === "Connected");
  const lastSync = activeConnections
    .flatMap((c) => (c.lastSyncAt ? [new Date(c.lastSyncAt)] : []))
    .sort((a, b) => b.getTime() - a.getTime())[0];

  return (
    <>
      <div className="rounded-xl border border-primary/20 bg-primary/5 p-4 flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="rounded-lg bg-primary/10 p-2 text-primary">
            <Building2 className="h-5 w-5" />
          </div>
          <div>
            {activeConnections.length === 0 ? (
              <>
                <p className="text-sm font-medium">Conecte seu banco</p>
                <p className="text-xs text-muted-foreground">
                  Importe suas transações automaticamente
                </p>
              </>
            ) : (
              <>
                <p className="text-sm font-medium">
                  {activeConnections.length}{" "}
                  {activeConnections.length === 1
                    ? "banco conectado"
                    : "bancos conectados"}
                </p>
                <p className="text-xs text-muted-foreground flex items-center gap-1">
                  <RefreshCw className="h-3 w-3" />
                  {lastSync
                    ? `Atualizado ${formatDistanceToNow(lastSync, { addSuffix: true, locale: ptBR })}`
                    : "Aguardando primeira sincronização"}
                </p>
              </>
            )}
          </div>
        </div>
        <Button
          size="sm"
          variant={activeConnections.length === 0 ? "default" : "outline"}
          onClick={() => setSetupOpen(true)}
          className="shrink-0"
        >
          {activeConnections.length === 0 ? (
            <>
              Conectar <ArrowRight className="ml-1 h-4 w-4" />
            </>
          ) : (
            "Gerenciar"
          )}
        </Button>
      </div>

      <BankSetupSheet open={setupOpen} onOpenChange={setSetupOpen} />
    </>
  );
}
