"use client";

import { useEffect, useState } from "react";
import { useCompleteOnboarding } from "@/hooks/use-bank-connections";
import { Button } from "@/components/ui/button";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Trash2, GitMerge, Loader2 } from "lucide-react";
import type { AccountMappingDto, OnboardingStrategy } from "@/types/bank-connection";

interface StepStrategyProps {
  connectionId: string;
  accountMappings: AccountMappingDto[];
  onBack: () => void;
  onSuccess: () => void;
}

export function StepStrategy({
  connectionId,
  accountMappings,
  onBack,
  onSuccess,
}: StepStrategyProps) {
  const [strategy, setStrategy] = useState<OnboardingStrategy>("Coexistence");
  const [customDate, setCustomDate] = useState("");
  const completeOnboarding = useCompleteOnboarding(connectionId);

  // Enquanto a configuração roda (migração de categorias + primeiro sync), impede
  // que o usuário saia da página com F5/fechar aba sem ver um aviso do navegador.
  useEffect(() => {
    if (!completeOnboarding.isPending) return;

    function handleBeforeUnload(event: BeforeUnloadEvent) {
      event.preventDefault();
      event.returnValue = "";
    }

    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [completeOnboarding.isPending]);

  function handleFinish() {
    completeOnboarding.mutate(
      {
        accountMappings,
        strategy,
        customCutoffDate:
          strategy === "Coexistence" && customDate ? customDate : undefined,
      },
      { onSuccess }
    );
  }

  return (
    <div className="space-y-6">
      {/* Overlay de tela cheia durante a configuração — bloqueia cliques em toda a página
          (inclusive o X do modal) até o onboarding terminar. */}
      {completeOnboarding.isPending && (
        <div
          className="fixed inset-0 z-[100] flex flex-col items-center justify-center gap-4 bg-background/80 backdrop-blur-sm"
          role="alert"
          aria-busy="true"
        >
          <Loader2 className="h-10 w-10 animate-spin text-primary" />
          <p className="max-w-xs text-center text-sm font-medium">
            Configurando conta, isso pode demorar alguns minutos!
          </p>
          <p className="max-w-xs text-center text-xs text-muted-foreground">
            Estamos preparando suas categorias e importando as transações do
            banco. Não feche nem recarregue a página.
          </p>
        </div>
      )}
      <p className="text-sm text-muted-foreground">
        O que fazer com os lançamentos que você já tem no MoneyManager?
      </p>

      <RadioGroup
        value={strategy}
        onValueChange={(v) => setStrategy(v as OnboardingStrategy)}
        className="space-y-3"
      >
        {/* Coexistência */}
        <div
          className={`rounded-lg border p-4 cursor-pointer transition-colors ${
            strategy === "Coexistence"
              ? "border-primary bg-primary/5"
              : "hover:border-muted-foreground/30"
          }`}
          onClick={() => setStrategy("Coexistence")}
        >
          <div className="flex items-start gap-3">
            <RadioGroupItem
              value="Coexistence"
              id="coexistence"
              className="mt-0.5"
            />
            <div className="space-y-1">
              <Label
                htmlFor="coexistence"
                className="flex items-center gap-2 cursor-pointer font-medium"
              >
                <GitMerge className="h-4 w-4 text-primary" />
                Manter lançamentos existentes
              </Label>
              <p className="text-xs text-muted-foreground">
                Seus lançamentos manuais são mantidos. O banco importa apenas a
                partir de uma data de corte calculada automaticamente (15 dias
                antes do seu último lançamento manual).
              </p>
            </div>
          </div>
          {strategy === "Coexistence" && (
            <div className="mt-3 ml-6 space-y-1.5">
              <Label htmlFor="customDate" className="text-xs">
                Data de corte personalizada (opcional)
              </Label>
              <Input
                id="customDate"
                type="date"
                value={customDate}
                onChange={(e) => setCustomDate(e.target.value)}
                className="h-8 text-xs w-48"
              />
              <p className="text-xs text-muted-foreground">
                Se não preenchido, calculamos automaticamente.
              </p>
            </div>
          )}
        </div>

        {/* Começar do zero */}
        <div
          className={`rounded-lg border p-4 cursor-pointer transition-colors ${
            strategy === "CleanSlate"
              ? "border-destructive bg-destructive/5"
              : "hover:border-muted-foreground/30"
          }`}
          onClick={() => setStrategy("CleanSlate")}
        >
          <div className="flex items-start gap-3">
            <RadioGroupItem
              value="CleanSlate"
              id="clean-slate"
              className="mt-0.5"
            />
            <div className="space-y-1">
              <Label
                htmlFor="clean-slate"
                className="flex items-center gap-2 cursor-pointer font-medium"
              >
                <Trash2 className="h-4 w-4 text-destructive" />
                Começar do zero
              </Label>
              <p className="text-xs text-muted-foreground">
                Todos os lançamentos manuais são arquivados (não deletados —
                você pode recuperá-los depois). O banco importa os últimos 12
                meses completos.
              </p>
            </div>
          </div>
        </div>
      </RadioGroup>

      {strategy === "CleanSlate" && (
        <div className="rounded-lg bg-destructive/10 border border-destructive/20 p-3">
          <p className="text-xs text-destructive">
            ⚠️ Seus lançamentos manuais serão arquivados. Esta ação pode ser
            revertida pelo suporte, mas não automaticamente pelo app.
          </p>
        </div>
      )}

      <div className="flex gap-2">
        <Button variant="outline" className="flex-1" onClick={onBack}>
          Voltar
        </Button>
        <Button
          className="flex-1"
          onClick={handleFinish}
          disabled={completeOnboarding.isPending}
          variant={strategy === "CleanSlate" ? "destructive" : "default"}
        >
          {completeOnboarding.isPending ? "Configurando..." : "Finalizar"}
        </Button>
      </div>
    </div>
  );
}
