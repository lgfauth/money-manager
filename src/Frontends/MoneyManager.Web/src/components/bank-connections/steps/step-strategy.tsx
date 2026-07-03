"use client";

import { useState } from "react";
import { useCompleteOnboarding } from "@/hooks/use-bank-connections";
import { Button } from "@/components/ui/button";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Trash2, GitMerge } from "lucide-react";
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
