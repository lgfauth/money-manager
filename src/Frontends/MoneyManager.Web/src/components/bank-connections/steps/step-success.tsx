"use client";

import { CheckCircle2 } from "lucide-react";
import { Button } from "@/components/ui/button";

interface StepSuccessProps {
  institutionName: string;
  hasMoreBanks: boolean;
  onAddAnother: () => void;
  onFinish: () => void;
}

export function StepSuccess({
  institutionName,
  hasMoreBanks,
  onAddAnother,
  onFinish,
}: StepSuccessProps) {
  return (
    <div className="flex flex-col items-center text-center py-8 space-y-6">
      <div className="rounded-full bg-primary/10 p-4">
        <CheckCircle2 className="h-10 w-10 text-primary" />
      </div>

      <div className="space-y-1">
        <p className="text-lg font-semibold">{institutionName} conectado!</p>
        <p className="text-sm text-muted-foreground">
          Suas transacoes serao importadas em breve. A sincronizacao automatica
          ocorre 4 vezes ao dia.
        </p>
      </div>

      <div className="flex flex-col gap-2 w-full max-w-xs">
        {hasMoreBanks && (
          <Button onClick={onAddAnother} className="w-full">
            Conectar outro banco
          </Button>
        )}
        <Button
          variant={hasMoreBanks ? "outline" : "default"}
          onClick={onFinish}
          className="w-full"
        >
          {hasMoreBanks ? "Finalizar por agora" : "Concluir"}
        </Button>
      </div>
    </div>
  );
}
