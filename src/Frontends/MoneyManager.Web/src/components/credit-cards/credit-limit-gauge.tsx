"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { RadialChart } from "@/components/charts/radial-chart";
import {
  CREDIT_LIMIT_THRESHOLD_WARNING,
  CREDIT_LIMIT_THRESHOLD_DANGER,
} from "@/config/constants";

interface CreditLimitGaugeProps {
  creditLimit: number;
  usedAmount: number;
  currency?: string;
}

function getGaugeColor(percentage: number): string {
  if (percentage <= CREDIT_LIMIT_THRESHOLD_WARNING) return "var(--color-income)";
  if (percentage <= CREDIT_LIMIT_THRESHOLD_DANGER) return "var(--color-warning)";
  return "var(--color-expense)";
}

function formatCurrency(value: number, currency = "BRL"): string {
  return value.toLocaleString("pt-BR", { style: "currency", currency });
}

export function CreditLimitGauge({
  creditLimit,
  usedAmount,
  currency = "BRL",
}: CreditLimitGaugeProps) {
  const percentage = creditLimit > 0 ? (usedAmount / creditLimit) * 100 : 0;
  const available = Math.max(creditLimit - usedAmount, 0);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Limite Comprometido</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col items-center gap-4">
        <RadialChart
          value={Math.min(percentage, 100)}
          label="Comprometido"
          color={getGaugeColor(percentage)}
          size={180}
        />
        <div className="grid w-full grid-cols-3 gap-2 text-center text-sm">
          <div>
            <p className="text-muted-foreground">Total</p>
            <p className="font-semibold">{formatCurrency(creditLimit, currency)}</p>
          </div>
          <div>
            <p className="text-muted-foreground">Comprometido</p>
            <p className="font-semibold text-expense">
              {formatCurrency(usedAmount, currency)}
            </p>
          </div>
          <div>
            <p className="text-muted-foreground">Disponível</p>
            <p className="font-semibold text-income">
              {formatCurrency(available, currency)}
            </p>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
