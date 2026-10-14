"use client";

import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { PeriodSelector } from "@/components/shared/period-selector";
import type { ReportPreset } from "@/hooks/use-reports";
import type { ReportBasis, ReportPeriod } from "@/types/report";

const presetLabels: Record<ReportPreset, string> = {
  current: "Atual",
  previous: "Anterior",
  "3m": "3M",
  "6m": "6M",
  year: "Ano",
};

interface ReportFiltersProps {
  period: ReportPeriod;
  preset: ReportPreset | "custom";
  basis: ReportBasis;
  onPeriodChange: (start: string, end: string) => void;
  onPresetChange: (preset: ReportPreset) => void;
  onBasisChange: (basis: ReportBasis) => void;
}

export function ReportFilters({
  period,
  preset,
  basis,
  onPeriodChange,
  onPresetChange,
  onBasisChange,
}: ReportFiltersProps) {
  return (
    <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
      <PeriodSelector
        startDate={period.startDate}
        endDate={period.endDate}
        onChange={onPeriodChange}
        showPresets={false}
      />
      <div className="flex flex-wrap items-center gap-2">
        <Tabs value={preset} onValueChange={(value) => onPresetChange(value as ReportPreset)}>
          <TabsList className="h-8">
            {(Object.keys(presetLabels) as ReportPreset[]).map((key) => (
              <TabsTrigger key={key} value={key} className="text-xs px-2 h-6">
                {presetLabels[key]}
              </TabsTrigger>
            ))}
          </TabsList>
        </Tabs>
        <Tabs value={basis} onValueChange={(value) => onBasisChange(value as ReportBasis)}>
          <TabsList className="h-8">
            <TabsTrigger
              value="Accrual"
              className="text-xs px-2 h-6"
              title="Compras no cartão contam na data de cada parcela"
            >
              Competência
            </TabsTrigger>
            <TabsTrigger
              value="Cash"
              className="text-xs px-2 h-6"
              title="Compras no cartão contam quando a fatura é paga"
            >
              Caixa
            </TabsTrigger>
          </TabsList>
        </Tabs>
      </div>
    </div>
  );
}
