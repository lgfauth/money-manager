"use client";

import { useRouter } from "next/navigation";
import { useHealthScore, useFinancialHealthSettings } from "@/hooks/use-financial-health";
import { ScoreRing } from "@/components/financial-health/score-ring";
import { MetricCard } from "@/components/financial-health/metric-card";
import { HealthInsightBanner } from "@/components/financial-health/health-insight-banner";
import { CheckinModal } from "@/components/financial-health/checkin-modal";
import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Settings, CalendarCheck } from "lucide-react";
import { useState, useEffect } from "react";
import { useSnapshotStatus } from "@/hooks/use-financial-health";
import { DEFAULT_CURRENCY, DEFAULT_LOCALE } from "@/config/constants";

function formatCurrency(value: number) {
  return new Intl.NumberFormat(DEFAULT_LOCALE, {
    style: "currency",
    currency: DEFAULT_CURRENCY,
  }).format(value);
}

function formatMonths(months: number | null): string {
  if (months === null) return "—";
  if (months >= 12) {
    const years = Math.floor(months / 12);
    const rem = months % 12;
    return rem > 0 ? `${years} anos e ${rem} meses` : `${years} anos`;
  }
  return `${months} meses`;
}

export default function FinancialHealthPage() {
  const router = useRouter();
  const [showCheckinModal, setShowCheckinModal] = useState(false);

  const { data: settings, isLoading: loadingSettings, isFetching: fetchingSettings } = useFinancialHealthSettings();
  const { data: score, isLoading: loadingScore } = useHealthScore();
  const { data: snapshotStatus } = useSnapshotStatus();

  useEffect(() => {
    if (!loadingSettings && !fetchingSettings && settings === null) {
      router.replace("/financial-health/setup");
    }
  }, [loadingSettings, fetchingSettings, settings, router]);

  if (loadingSettings || fetchingSettings || loadingScore) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-10 w-48" />
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-36" />
          ))}
        </div>
      </div>
    );
  }

  if (!score) return null;

  if (!score.hasData) {
    return (
      <div className="space-y-6">
        <PageHeader title="Saúde Financeira" />
        <div className="flex flex-col items-center justify-center py-20 text-center">
          <p className="text-lg font-medium text-muted-foreground">
            Nenhuma movimentação registrada este mês ainda
          </p>
          <p className="mt-2 text-sm text-muted-foreground">
            O score será calculado assim que houver receitas lançadas no mês atual.
          </p>
        </div>
      </div>
    );
  }

  const pendingBuckets = snapshotStatus?.pendingBuckets ?? [];
  const referenceMonth = snapshotStatus?.referenceMonth ?? "";

  return (
    <div className="space-y-6">
      <PageHeader title="Saúde Financeira">
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={() => router.push("/financial-health/setup")}>
            <Settings className="mr-2 h-4 w-4" />
            Configurações
          </Button>
          {pendingBuckets.length > 0 && (
            <Button size="sm" onClick={() => setShowCheckinModal(true)}>
              <CalendarCheck className="mr-2 h-4 w-4" />
              Fazer check-in
            </Button>
          )}
        </div>
      </PageHeader>

      {/* Score + resumo */}
      <div className="grid gap-4 lg:grid-cols-4">
        <Card className="flex flex-col items-center justify-center gap-3 py-6 lg:col-span-1">
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
            Score geral
          </p>
          <ScoreRing score={score.overallScore} size={140} />
          <p className="text-xs text-muted-foreground">
            {score.referenceMonth} · mês em andamento
          </p>
        </Card>

        <div className="grid gap-4 sm:grid-cols-3 lg:col-span-3">
          <Card>
            <CardHeader className="pb-1">
              <CardTitle className="text-xs text-muted-foreground font-medium">Receita</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-xl font-bold text-emerald-600">{formatCurrency(score.totalIncome)}</p>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="pb-1">
              <CardTitle className="text-xs text-muted-foreground font-medium">Gastos totais</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-xl font-bold text-red-500">{formatCurrency(score.totalExpenses)}</p>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="pb-1">
              <CardTitle className="text-xs text-muted-foreground font-medium">Investimentos</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-xl font-bold text-primary">{formatCurrency(score.totalInvestments)}</p>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Métricas */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <MetricCard
          title="Aporte mensal"
          subtitle="% da renda investida"
          currentValue={formatCurrency(score.investmentMetric.currentValue)}
          targetValue={formatCurrency(score.investmentMetric.targetValue)}
          progressPercent={score.investmentMetric.progressPercent}
          status={score.investmentMetric.status}
        />
        <MetricCard
          title="Reserva de emergência"
          subtitle={`Meta: ${settings?.reserveMonths ?? "—"} meses de gastos`}
          currentValue={formatCurrency(score.reserveMetric.currentValue)}
          targetValue={formatCurrency(score.reserveMetric.targetValue)}
          progressPercent={score.reserveMetric.progressPercent}
          status={score.reserveMetric.status}
        />
        <MetricCard
          title="Meta FIRE"
          subtitle={`${settings?.fireMultiplier ?? "—"}× a renda mensal`}
          currentValue={formatCurrency(score.fireMetric.currentValue)}
          targetValue={formatCurrency(score.fireMetric.targetValue)}
          progressPercent={score.fireMetric.progressPercent}
          status={score.fireMetric.status}
        />
        <MetricCard
          title="Controle de gastos"
          subtitle={`Teto: ${settings?.fixedExpensePercent ?? "—"}% da renda`}
          currentValue={formatCurrency(score.expenseMetric.currentValue)}
          targetValue={formatCurrency(score.expenseMetric.targetValue)}
          progressPercent={score.expenseMetric.progressPercent}
          status={score.expenseMetric.status}
        />
      </div>

      {/* Projeções */}
      <div className="grid gap-4 sm:grid-cols-3">
        <Card>
          <CardHeader className="pb-1">
            <CardTitle className="text-sm">Meta FIRE</CardTitle>
          </CardHeader>
          <CardContent className="space-y-1">
            <p className="text-xs text-muted-foreground">Patrimônio alvo</p>
            <p className="text-lg font-bold">{formatCurrency(score.projection.fireTarget)}</p>
            <p className="text-xs text-muted-foreground">Saldo atual</p>
            <p className="text-base font-semibold text-primary">
              {formatCurrency(score.projection.currentFireBalance)}
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-1">
            <CardTitle className="text-sm">Reserva de emergência</CardTitle>
          </CardHeader>
          <CardContent className="space-y-1">
            <p className="text-xs text-muted-foreground">Meta</p>
            <p className="text-lg font-bold">{formatCurrency(score.projection.reserveTarget)}</p>
            <p className="text-xs text-muted-foreground">Saldo atual</p>
            <p className="text-base font-semibold text-primary">
              {formatCurrency(score.projection.currentReserveBalance)}
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-1">
            <CardTitle className="text-sm">Prazo estimado para FIRE</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold text-primary">
              {formatMonths(score.projection.estimatedMonthsToFire)}
            </p>
            <p className="mt-1 text-xs text-muted-foreground">
              {score.projection.estimatedMonthsToFire === null
                ? "Sem aporte ativo este mês"
                : "com o aporte atual mantido"}
            </p>
          </CardContent>
        </Card>
      </div>

      <HealthInsightBanner score={score} />

      {showCheckinModal && (
        <CheckinModal
          open={showCheckinModal}
          onClose={() => setShowCheckinModal(false)}
          referenceMonth={referenceMonth}
          pendingBuckets={pendingBuckets}
        />
      )}
    </div>
  );
}
