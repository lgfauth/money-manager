"use client";

import { useEffect } from "react";
import {
  useAvailableConnections,
  useRegisterConnection,
} from "@/hooks/use-bank-connections";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Building2, CheckCircle2, AlertCircle } from "lucide-react";
import type { BankMcpConnectionDto } from "@/types/bank-connection";

interface StepSelectConnectionProps {
  onBack: () => void;
  onSuccess: (
    connection: BankMcpConnectionDto,
    registeredConnectionId: string
  ) => void;
}

export function StepSelectConnection({
  onBack,
  onSuccess,
}: StepSelectConnectionProps) {
  const {
    data: available = [],
    isLoading,
    error,
    refetch,
  } = useAvailableConnections();
  const registerConnection = useRegisterConnection();

  useEffect(() => {
    refetch();
  }, [refetch]);

  function handleSelect(connection: BankMcpConnectionDto) {
    if (connection.alreadyRegistered || connection.status === "LOGIN_ERROR")
      return;

    registerConnection.mutate(connection.externalConnectionId, {
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
          Não foi possível carregar os bancos. Verifique se sua API key está
          correta.
        </p>
        <div className="flex gap-2 justify-center">
          <Button variant="outline" size="sm" onClick={onBack}>
            Alterar API key
          </Button>
          <Button size="sm" onClick={() => refetch()}>
            Tentar novamente
          </Button>
        </div>
      </div>
    );
  }

  const unregistered = available.filter((c) => !c.alreadyRegistered);

  if (unregistered.length === 0) {
    return (
      <div className="text-center space-y-3 py-8">
        <CheckCircle2 className="h-8 w-8 text-primary mx-auto" />
        <p className="text-sm text-muted-foreground">
          Todos os seus bancos já estão conectados ao MoneyManager.
        </p>
        <Button variant="outline" size="sm" onClick={onBack}>
          Voltar
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {unregistered.map((connection) => (
        <button
          key={connection.externalConnectionId}
          onClick={() => handleSelect(connection)}
          disabled={
            registerConnection.isPending ||
            connection.status === "LOGIN_ERROR"
          }
          className="w-full rounded-lg border p-4 text-left transition-colors hover:border-primary hover:bg-primary/5 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          <div className="flex items-center justify-between gap-3">
            <div className="flex items-center gap-3">
              {connection.institutionLogo ? (
                <img
                  src={connection.institutionLogo}
                  alt={connection.institutionName}
                  className="h-8 w-8 rounded-md object-contain"
                />
              ) : (
                <div className="h-8 w-8 rounded-md bg-muted flex items-center justify-center">
                  <Building2 className="h-4 w-4 text-muted-foreground" />
                </div>
              )}
              <span className="font-medium text-sm">
                {connection.institutionName}
              </span>
            </div>
            {connection.status === "LOGIN_ERROR" ? (
              <Badge variant="destructive" className="text-xs">
                Erro de login
              </Badge>
            ) : (
              <Badge variant="secondary" className="text-xs">
                Disponível
              </Badge>
            )}
          </div>
          {connection.status === "LOGIN_ERROR" && (
            <p className="text-xs text-destructive mt-2">
              Reconecte este banco no Banco MCP antes de continuar.
            </p>
          )}
        </button>
      ))}
      <Button variant="outline" className="w-full" onClick={onBack}>
        Voltar
      </Button>
    </div>
  );
}
