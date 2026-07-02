"use client";

import { useEffect } from "react";
import {
  useAvailableConnections,
  useBankConnections,
  useRegisterConnection,
} from "@/hooks/use-bank-connections";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Building2, CheckCircle2, AlertCircle, ExternalLink } from "lucide-react";
import type { BankMcpConnectionDto } from "@/types/bank-connection";

interface StepSelectConnectionProps {
  onBack: () => void;
  onSuccess: (
    connection: BankMcpConnectionDto,
    registeredConnectionId: string
  ) => void;
  onResume: (connectionId: string, connection: BankMcpConnectionDto) => void;
}

export function StepSelectConnection({
  onBack,
  onSuccess,
  onResume,
}: StepSelectConnectionProps) {
  const { data, isLoading, error, refetch } = useAvailableConnections();
  const { data: registeredConnections = [] } = useBankConnections();
  const registerConnection = useRegisterConnection();

  useEffect(() => {
    refetch();
  }, [refetch]);

  function handleSelect(connection: BankMcpConnectionDto) {
    if (connection.pendingSetup && connection.pendingConnectionId) {
      onResume(connection.pendingConnectionId, connection);
      return;
    }

    if (connection.alreadyRegistered || connection.status === "LOGIN_ERROR")
      return;

    registerConnection.mutate(connection.itemId, {
      onSuccess: (registered) => onSuccess(connection, registered.id),
    });
  }

  if (isLoading) {
    return (
      <div className="space-y-3">
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} className="h-16 w-full rounded-lg" />
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center space-y-3 py-8">
        <AlertCircle className="h-8 w-8 text-destructive mx-auto" />
        <p className="text-sm text-muted-foreground">
          Não foi possível carregar os bancos conectados.
        </p>
        <div className="flex gap-2 justify-center">
          <Button variant="outline" size="sm" onClick={onBack}>
            Voltar
          </Button>
          <Button size="sm" onClick={() => refetch()}>
            Tentar novamente
          </Button>
        </div>
      </div>
    );
  }

  const connections = data?.connections ?? [];
  const availableToConfigure = connections.filter((c) => !c.alreadyRegistered);
  const existingConnections = registeredConnections.filter(
    (connection) => connection.status !== "Disconnected"
  );

  if (availableToConfigure.length === 0 && existingConnections.length === 0) {
    return (
      <div className="text-center space-y-3 py-8">
        <CheckCircle2 className="h-8 w-8 text-primary mx-auto" />
        <p className="text-sm text-muted-foreground">
          Todos os seus bancos já estão conectados ao MoneyManager.
        </p>
        {data?.addConnectionUrl && (
          <a
            href={data.addConnectionUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-1 text-sm text-primary hover:underline"
          >
            Adicionar outro banco <ExternalLink className="h-3 w-3" />
          </a>
        )}
        <div>
          <Button variant="outline" size="sm" onClick={onBack}>
            Voltar
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {availableToConfigure.length > 0 && (
        <div className="space-y-3">
          {availableToConfigure.map((connection) => (
            <button
              key={connection.itemId}
              onClick={() => handleSelect(connection)}
              disabled={
                registerConnection.isPending ||
                (!connection.pendingSetup && connection.status === "LOGIN_ERROR")
              }
              className={`w-full rounded-lg border p-4 text-left transition-colors disabled:opacity-50 disabled:cursor-not-allowed ${
                connection.pendingSetup
                  ? "border-amber-500/30 bg-amber-500/5 hover:border-amber-500/60"
                  : "hover:border-primary hover:bg-primary/5"
              }`}
            >
              <div className="flex items-center justify-between gap-3">
                <div className="flex items-center gap-3">
                  <div className="h-8 w-8 rounded-md bg-muted flex items-center justify-center">
                    <Building2 className="h-4 w-4 text-muted-foreground" />
                  </div>
                  <span className="font-medium text-sm">
                    {connection.connectorName}
                  </span>
                </div>
                {connection.pendingSetup ? (
                  <Badge variant="outline" className="text-amber-500 border-amber-500/50 text-xs">
                    Configuração pendente
                  </Badge>
                ) : connection.status === "LOGIN_ERROR" ? (
                  <Badge variant="destructive" className="text-xs">
                    Erro de login
                  </Badge>
                ) : (
                  <Badge variant="secondary" className="text-xs">
                    Disponível
                  </Badge>
                )}
              </div>
              {!connection.pendingSetup && connection.status === "LOGIN_ERROR" && (
                <p className="text-xs text-destructive mt-2">
                  Reconecte este banco no Banco MCP antes de continuar.
                </p>
              )}
            </button>
          ))}
        </div>
      )}

      {existingConnections.length > 0 && (
        <div className="space-y-2">
          {availableToConfigure.length === 0 && (
            <div className="rounded-lg border border-primary/20 bg-primary/5 p-3">
              <p className="text-xs text-muted-foreground">
                Todos os seus bancos já estão conectados. Você ainda pode entrar e revisar os vínculos de contas.
              </p>
            </div>
          )}
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
            Bancos já conectados
          </p>
          {existingConnections.map((connection) => (
            <button
              key={connection.id}
              onClick={() =>
                onResume(connection.id, {
                  itemId: connection.id,
                  connectorId: connection.id,
                  connectorName: connection.institutionName,
                  status: "UPDATED",
                  alreadyRegistered: true,
                  pendingSetup: false,
                  pendingConnectionId: connection.id,
                })
              }
              className="w-full rounded-lg border p-4 text-left transition-colors hover:border-primary hover:bg-primary/5"
            >
              <div className="flex items-center justify-between gap-3">
                <div className="flex items-center gap-3">
                  <div className="h-8 w-8 rounded-md bg-muted flex items-center justify-center">
                    <Building2 className="h-4 w-4 text-muted-foreground" />
                  </div>
                  <span className="font-medium text-sm">{connection.institutionName}</span>
                </div>
                <Badge variant="outline" className="text-xs">
                  Reconfigurar vínculo
                </Badge>
              </div>
            </button>
          ))}
        </div>
      )}

      {data?.addConnectionUrl && (
        <a
          href={data.addConnectionUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex items-center gap-1 text-sm text-primary hover:underline"
        >
          Adicionar outro banco <ExternalLink className="h-3 w-3" />
        </a>
      )}

      <Button variant="outline" className="w-full" onClick={onBack}>
        Voltar
      </Button>
    </div>
  );
}
