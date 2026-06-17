"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useCategories } from "@/hooks/use-categories";
import {
  useUpsertFinancialHealthSettings,
  useUpsertPatrimonyBucket,
} from "@/hooks/use-financial-health";
import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { MoneyInput } from "@/components/shared/money-input";
import { cn } from "@/lib/utils";
import { Check, ChevronRight, ChevronLeft } from "lucide-react";
import type { UpsertFinancialHealthSettingsRequest } from "@/types/financial-health";
import { CategoryType } from "@/types/category";

interface ModePreset {
  modeName: string;
  label: string;
  description: string;
  emoji: string;
  investPercent: number;
  reserveMonths: number;
  fireMultiplier: number;
  fixedExpensePercent: number;
  installmentPercent: number;
}

const MODE_PRESETS: ModePreset[] = [
  {
    modeName: "conservador",
    label: "Conservador",
    emoji: "🐢",
    description: "Prioriza segurança com reserva longa e metas conservadoras.",
    investPercent: 10,
    reserveMonths: 12,
    fireMultiplier: 300,
    fixedExpensePercent: 40,
    installmentPercent: 20,
  },
  {
    modeName: "moderado",
    label: "Moderado",
    emoji: "🦊",
    description: "Equilíbrio entre segurança e crescimento patrimonial.",
    investPercent: 20,
    reserveMonths: 6,
    fireMultiplier: 250,
    fixedExpensePercent: 50,
    installmentPercent: 30,
  },
  {
    modeName: "agressivo_fire",
    label: "Agressivo FIRE",
    emoji: "🐇",
    description: "Foco em independência financeira acelerada com altos aportes.",
    investPercent: 40,
    reserveMonths: 3,
    fireMultiplier: 200,
    fixedExpensePercent: 35,
    installmentPercent: 15,
  },
  {
    modeName: "personalizado",
    label: "Personalizado",
    emoji: "⚙️",
    description: "Defina cada parâmetro de acordo com sua realidade.",
    investPercent: 20,
    reserveMonths: 6,
    fireMultiplier: 250,
    fixedExpensePercent: 50,
    installmentPercent: 30,
  },
];

const DEFAULT_RESERVE_RATE = 0.105;
const DEFAULT_FIRE_RATE = 0.12;

export default function FinancialHealthSetupPage() {
  const router = useRouter();
  const { data: categories } = useCategories();
  const upsertSettings = useUpsertFinancialHealthSettings();
  const upsertBucket = useUpsertPatrimonyBucket();

  const [step, setStep] = useState(1);
  const [selectedPreset, setSelectedPreset] = useState<string>("moderado");
  const [settings, setSettings] = useState<UpsertFinancialHealthSettingsRequest>({
    modeName: "moderado",
    investPercent: 20,
    reserveMonths: 6,
    fireMultiplier: 250,
    fixedExpensePercent: 50,
    installmentPercent: 30,
  });

  const [reserveBalance, setReserveBalance] = useState(0);
  const [reserveDate, setReserveDate] = useState(
    new Date().toISOString().slice(0, 10)
  );
  const [reserveRate, setReserveRate] = useState(DEFAULT_RESERVE_RATE);

  const [fireBalance, setFireBalance] = useState(0);
  const [fireDate, setFireDate] = useState(
    new Date().toISOString().slice(0, 10)
  );
  const [fireRate, setFireRate] = useState(DEFAULT_FIRE_RATE);

  const [reserveCategoryIds, setReserveCategoryIds] = useState<string[]>([]);
  const [fireCategoryIds, setFireCategoryIds] = useState<string[]>([]);

  const expenseCategories = (categories ?? []).filter(
    (c) => c.type === CategoryType.Expense
  );

  function selectPreset(preset: ModePreset) {
    setSelectedPreset(preset.modeName);
    setSettings({
      modeName: preset.modeName,
      investPercent: preset.investPercent,
      reserveMonths: preset.reserveMonths,
      fireMultiplier: preset.fireMultiplier,
      fixedExpensePercent: preset.fixedExpensePercent,
      installmentPercent: preset.installmentPercent,
    });
  }

  function toggleReserveCategory(id: string) {
    if (fireCategoryIds.includes(id)) return;
    setReserveCategoryIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]
    );
  }

  function toggleFireCategory(id: string) {
    if (reserveCategoryIds.includes(id)) return;
    setFireCategoryIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]
    );
  }

  async function handleFinish() {
    await upsertSettings.mutateAsync(settings);

    await Promise.all([
      upsertBucket.mutateAsync({
        type: "emergency_reserve",
        initialBalance: reserveBalance,
        initialBalanceDate: new Date(reserveDate).toISOString(),
        trackedCategoryIds: reserveCategoryIds,
        expectedAnnualRate: reserveRate,
      }),
      upsertBucket.mutateAsync({
        type: "fire_investment",
        initialBalance: fireBalance,
        initialBalanceDate: new Date(fireDate).toISOString(),
        trackedCategoryIds: fireCategoryIds,
        expectedAnnualRate: fireRate,
      }),
    ]);

    router.push("/financial-health");
  }

  const isSaving = upsertSettings.isPending || upsertBucket.isPending;

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <PageHeader title="Configurar Saúde Financeira" />

      {/* Stepper */}
      <div className="flex items-center gap-2">
        {[1, 2, 3].map((s) => (
          <div key={s} className="flex items-center gap-2">
            <div
              className={cn(
                "flex h-7 w-7 items-center justify-center rounded-full text-xs font-bold",
                step > s
                  ? "bg-primary text-white"
                  : step === s
                    ? "bg-primary/20 text-primary ring-2 ring-primary"
                    : "bg-muted text-muted-foreground"
              )}
            >
              {step > s ? <Check className="h-3.5 w-3.5" /> : s}
            </div>
            <span
              className={cn(
                "text-sm",
                step === s ? "font-medium" : "text-muted-foreground"
              )}
            >
              {s === 1 ? "Modo" : s === 2 ? "Patrimônio" : "Categorias"}
            </span>
            {s < 3 && <ChevronRight className="h-4 w-4 text-muted-foreground" />}
          </div>
        ))}
      </div>

      {/* Passo 1 — Seleção de modo */}
      {step === 1 && (
        <div className="space-y-4">
          <p className="text-sm text-muted-foreground">
            Escolha um perfil de metas. Você pode personalizar os valores depois.
          </p>
          <div className="grid gap-3 sm:grid-cols-2">
            {MODE_PRESETS.map((preset) => (
              <button
                key={preset.modeName}
                type="button"
                onClick={() => selectPreset(preset)}
                className={cn(
                  "rounded-xl border p-4 text-left transition-all",
                  selectedPreset === preset.modeName
                    ? "border-primary bg-primary/5 ring-2 ring-primary"
                    : "border-border hover:border-primary/50"
                )}
              >
                <div className="flex items-center gap-2 mb-1">
                  <span className="text-xl">{preset.emoji}</span>
                  <span className="font-semibold text-sm">{preset.label}</span>
                </div>
                <p className="text-xs text-muted-foreground">{preset.description}</p>
                <div className="mt-3 grid grid-cols-2 gap-x-4 gap-y-1 text-xs text-muted-foreground">
                  <span>Aporte: <b>{preset.investPercent}%</b></span>
                  <span>Reserva: <b>{preset.reserveMonths}×</b></span>
                  <span>Multiplicador FIRE: <b>{preset.fireMultiplier}×</b></span>
                  <span>Teto gastos: <b>{preset.fixedExpensePercent}%</b></span>
                </div>
              </button>
            ))}
          </div>

          {selectedPreset === "personalizado" && (
            <Card>
              <CardHeader>
                <CardTitle className="text-sm">Parâmetros personalizados</CardTitle>
              </CardHeader>
              <CardContent className="grid gap-4 sm:grid-cols-2">
                {(
                  [
                    { key: "investPercent", label: "Aporte mensal (%)", min: 1, max: 70 },
                    { key: "reserveMonths", label: "Meses de reserva", min: 1, max: 24 },
                    { key: "fireMultiplier", label: "Multiplicador FIRE", min: 50, max: 600 },
                    { key: "fixedExpensePercent", label: "Teto de gastos (%)", min: 10, max: 90 },
                  ] as const
                ).map(({ key, label, min, max }) => (
                  <div key={key} className="space-y-1">
                    <Label className="text-xs">{label}</Label>
                    <Input
                      type="number"
                      min={min}
                      max={max}
                      value={settings[key]}
                      onChange={(e) =>
                        setSettings((prev) => ({
                          ...prev,
                          [key]: Number(e.target.value),
                        }))
                      }
                    />
                  </div>
                ))}
              </CardContent>
            </Card>
          )}

          <div className="flex justify-end">
            <Button onClick={() => setStep(2)}>
              Próximo <ChevronRight className="ml-1 h-4 w-4" />
            </Button>
          </div>
        </div>
      )}

      {/* Passo 2 — Patrimônio */}
      {step === 2 && (
        <div className="space-y-4">
          <p className="text-sm text-muted-foreground">
            Informe o saldo atual de cada balde e a data de referência. Aportes serão
            contabilizados a partir dessa data.
          </p>
          <div className="grid gap-4 sm:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle className="text-sm">🛡️ Reserva de emergência</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="space-y-1">
                  <Label className="text-xs">Saldo atual</Label>
                  <MoneyInput value={reserveBalance} onChange={setReserveBalance} />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs">Data de referência</Label>
                  <Input
                    type="date"
                    value={reserveDate}
                    onChange={(e) => setReserveDate(e.target.value)}
                  />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs">Taxa anual esperada (ex: 0.105)</Label>
                  <Input
                    type="number"
                    step="0.001"
                    min={0}
                    max={1}
                    value={reserveRate}
                    onChange={(e) => setReserveRate(Number(e.target.value))}
                  />
                  <p className="text-xs text-muted-foreground">
                    {(reserveRate * 100).toFixed(1)}% ao ano
                  </p>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-sm">🚀 Investimentos FIRE</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="space-y-1">
                  <Label className="text-xs">Saldo atual</Label>
                  <MoneyInput value={fireBalance} onChange={setFireBalance} />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs">Data de referência</Label>
                  <Input
                    type="date"
                    value={fireDate}
                    onChange={(e) => setFireDate(e.target.value)}
                  />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs">Taxa anual esperada (ex: 0.12)</Label>
                  <Input
                    type="number"
                    step="0.001"
                    min={0}
                    max={1}
                    value={fireRate}
                    onChange={(e) => setFireRate(Number(e.target.value))}
                  />
                  <p className="text-xs text-muted-foreground">
                    {(fireRate * 100).toFixed(1)}% ao ano
                  </p>
                </div>
              </CardContent>
            </Card>
          </div>

          <div className="flex justify-between">
            <Button variant="outline" onClick={() => setStep(1)}>
              <ChevronLeft className="mr-1 h-4 w-4" /> Voltar
            </Button>
            <Button onClick={() => setStep(3)}>
              Próximo <ChevronRight className="ml-1 h-4 w-4" />
            </Button>
          </div>
        </div>
      )}

      {/* Passo 3 — Categorias */}
      {step === 3 && (
        <div className="space-y-4">
          <p className="text-sm text-muted-foreground">
            Mapeie suas categorias de despesa para cada balde. Uma categoria só pode
            pertencer a um balde. Categorias marcadas como FIRE serão usadas para
            calcular seu aporte mensal.
          </p>

          {expenseCategories.length === 0 ? (
            <p className="text-sm text-muted-foreground italic">
              Nenhuma categoria de despesa encontrada. Você pode mapear depois em Configurações.
            </p>
          ) : (
            <div className="space-y-4">
              <div>
                <p className="mb-2 text-sm font-medium">🛡️ Reserva de emergência</p>
                <div className="flex flex-wrap gap-2">
                  {expenseCategories.map((cat) => {
                    const selected = reserveCategoryIds.includes(cat.id);
                    const disabled = fireCategoryIds.includes(cat.id);
                    return (
                      <button
                        key={cat.id}
                        type="button"
                        disabled={disabled}
                        onClick={() => toggleReserveCategory(cat.id)}
                        className={cn(
                          "rounded-full border px-3 py-1 text-xs font-medium transition-all",
                          selected
                            ? "border-emerald-500 bg-emerald-500 text-white"
                            : disabled
                              ? "border-muted bg-muted/20 text-muted-foreground opacity-40 cursor-not-allowed"
                              : "border-border hover:border-emerald-400"
                        )}
                      >
                        {cat.name}
                      </button>
                    );
                  })}
                </div>
              </div>

              <div>
                <p className="mb-2 text-sm font-medium">🚀 Investimentos FIRE</p>
                <div className="flex flex-wrap gap-2">
                  {expenseCategories.map((cat) => {
                    const selected = fireCategoryIds.includes(cat.id);
                    const disabled = reserveCategoryIds.includes(cat.id);
                    return (
                      <button
                        key={cat.id}
                        type="button"
                        disabled={disabled}
                        onClick={() => toggleFireCategory(cat.id)}
                        className={cn(
                          "rounded-full border px-3 py-1 text-xs font-medium transition-all",
                          selected
                            ? "border-primary bg-primary text-white"
                            : disabled
                              ? "border-muted bg-muted/20 text-muted-foreground opacity-40 cursor-not-allowed"
                              : "border-border hover:border-primary/50"
                        )}
                      >
                        {cat.name}
                      </button>
                    );
                  })}
                </div>
              </div>
            </div>
          )}

          <div className="flex justify-between">
            <Button variant="outline" onClick={() => setStep(2)}>
              <ChevronLeft className="mr-1 h-4 w-4" /> Voltar
            </Button>
            <Button onClick={handleFinish} disabled={isSaving}>
              {isSaving ? "Salvando..." : "Concluir configuração"}
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
