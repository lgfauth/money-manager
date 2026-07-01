"use client";

import { useEffect, useState } from "react";
import { useBankMcpInviteUrl } from "@/hooks/use-bank-connections";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { ExternalLink, AlertCircle } from "lucide-react";

interface StepInviteProps {
  onSuccess: () => void;
}

export function StepInvite({ onSuccess }: StepInviteProps) {
  const { data, isLoading, error, refetch } = useBankMcpInviteUrl();
  const [opened, setOpened] = useState(false);

  useEffect(() => {
    refetch();
  }, [refetch]);

  function handleOpenInvite() {
    if (!data?.connectUrl) return;
    window.open(data.connectUrl, "_blank", "noopener,noreferrer");
    setOpened(true);
  }

  if (isLoading) {
    return <Skeleton className="h-32 w-full rounded-lg" />;
  }

  if (error || !data?.connectUrl) {
    return (
      <div className="text-center space-y-3 py-8">
        <AlertCircle className="h-8 w-8 text-destructive mx-auto" />
        <p className="text-sm text-muted-foreground">
          Não foi possível gerar o link de conexão. Tente novamente.
        </p>
        <Button size="sm" onClick={() => refetch()}>
          Tentar novamente
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="rounded-lg bg-muted/50 p-4 space-y-2 text-sm text-muted-foreground">
        <p className="font-medium text-foreground">Como conectar seu banco:</p>
        <ol className="list-decimal list-inside space-y-1">
          <li>Clique em "Conectar banco" abaixo</li>
          <li>Faça login no seu banco na página do Banco MCP que abrir</li>
          <li>Volte aqui e clique em "Já conectei, continuar"</li>
        </ol>
      </div>

      <Button variant="outline" className="w-full" onClick={handleOpenInvite}>
        Conectar banco <ExternalLink className="ml-1 h-4 w-4" />
      </Button>

      <Button className="w-full" disabled={!opened} onClick={onSuccess}>
        Já conectei, continuar
      </Button>
    </div>
  );
}
