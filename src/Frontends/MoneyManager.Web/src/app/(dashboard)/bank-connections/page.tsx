"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { PageHeader } from "@/components/shared/page-header";
import {
  useBankConnections,
  useSyncBank,
  useDisconnectBank,
} from "@/hooks/use-bank-connections";
import { useIsPremium } from "@/hooks/use-subscription";
import { BankSetupSheet } from "@/components/bank-connections/bank-setup-sheet";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Building2, RefreshCw, Plus, Unplug } from "lucide-react";
import { formatDistanceToNow } from "date-fns";
import { ptBR } from "date-fns/locale";

export default function BankConnectionsPage() {
  const isPremium = useIsPremium();
  const router = useRouter();
  const { data: connections = [], isLoading } = useBankConnections();
  const syncBank = useSyncBank();
  const disconnectBank = useDisconnectBank();
  const [setupOpen, setSetupOpen] = useState(false);
  const [disconnectId, setDisconnectId] = useState<string | null>(null);
  const [syncingId, setSyncingId] = useState<string | null>(null);

  if (!isPremium) {
    router.replace("/");
    return null;
  }

  function handleSync(connectionId: string) {
    setSyncingId(connectionId);
    syncBank.mutate(connectionId, {
      onSettled: () => setSyncingId(null),
    });
  }

  return (
    <div>
      <PageHeader
        title="Bancos conectados"
        description="Seus bancos sincronizados automaticamente com o MoneyManager"
      >
        <Button size="sm" onClick={() => setSetupOpen(true)}>
          <Plus className="h-4 w-4 mr-1" /> Adicionar banco
        </Button>
      </PageHeader>

      {isLoading ? (
        <div className="space-y-3 mt-6">
          {[1, 2].map((i) => (
            <Skeleton key={i} className="h-24 w-full rounded-xl" />
          ))}
        </div>
      ) : connections.length === 0 ? (
        <div className="mt-12 text-center space-y-3">
          <Building2 className="h-10 w-10 text-muted-foreground mx-auto" />
          <p className="text-muted-foreground text-sm">
            Nenhum banco conectado ainda.
          </p>
          <Button onClick={() => setSetupOpen(true)}>
            Conectar primeiro banco
          </Button>
        </div>
      ) : (
        <div className="mt-6 space-y-3">
          {connections.map((connection) => (
            <div
              key={connection.id}
              className="rounded-xl border p-4 flex items-center justify-between gap-4"
            >
              <div className="flex items-center gap-3">
                {connection.institutionLogo ? (
                  <img
                    src={connection.institutionLogo}
                    alt={connection.institutionName}
                    className="h-10 w-10 rounded-lg object-contain"
                  />
                ) : (
                  <div className="h-10 w-10 rounded-lg bg-muted flex items-center justify-center">
                    <Building2 className="h-5 w-5 text-muted-foreground" />
                  </div>
                )}
                <div>
                  <p className="font-medium text-sm">
                    {connection.institutionName}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {connection.selectedAccounts.length} conta
                    {connection.selectedAccounts.length !== 1 ? "s" : ""}{" "}
                    sincronizada
                    {connection.selectedAccounts.length !== 1 ? "s" : ""}
                    {connection.lastSyncAt && (
                      <>
                        {" · "}
                        Atualizado{" "}
                        {formatDistanceToNow(new Date(connection.lastSyncAt), {
                          addSuffix: true,
                          locale: ptBR,
                        })}
                      </>
                    )}
                  </p>
                </div>
              </div>
              <div className="flex items-center gap-2 shrink-0">
                <Badge
                  variant={
                    connection.status === "Connected" ? "default" : "destructive"
                  }
                  className="text-xs hidden sm:inline-flex"
                >
                  {connection.status === "Connected"
                    ? "Ativo"
                    : connection.status}
                </Badge>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={syncingId === connection.id}
                  onClick={() => handleSync(connection.id)}
                >
                  <RefreshCw
                    className={`h-3.5 w-3.5 ${syncingId === connection.id ? "animate-spin" : ""}`}
                  />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  className="text-destructive hover:text-destructive"
                  onClick={() => setDisconnectId(connection.id)}
                >
                  <Unplug className="h-3.5 w-3.5" />
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      <BankSetupSheet open={setupOpen} onOpenChange={setSetupOpen} />

      <AlertDialog
        open={!!disconnectId}
        onOpenChange={() => setDisconnectId(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Desconectar banco?</AlertDialogTitle>
            <AlertDialogDescription>
              A sincronização automática será interrompida. Suas transações já
              importadas são mantidas. Esta ação não pode ser desfeita
              diretamente.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive hover:bg-destructive/90"
              onClick={() => {
                if (disconnectId) {
                  disconnectBank.mutate(disconnectId);
                  setDisconnectId(null);
                }
              }}
            >
              Desconectar
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
