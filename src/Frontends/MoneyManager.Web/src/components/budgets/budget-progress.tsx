"use client";

import { cn } from "@/lib/utils";
import { BUDGET_THRESHOLD_WARNING, BUDGET_THRESHOLD_DANGER } from "@/config/constants";

interface BudgetProgressProps {
  spent: number;
  limit: number;
  className?: string;
}

export function BudgetProgress({ spent, limit, className }: BudgetProgressProps) {
  const percent = limit > 0 ? (spent / limit) * 100 : 0;
  const clamped = Math.min(percent, 100);

  const barColor =
    percent >= BUDGET_THRESHOLD_DANGER
      ? "bg-expense"
      : percent >= BUDGET_THRESHOLD_WARNING
        ? "bg-warning"
        : "bg-income";

  return (
    <div className={cn("space-y-1", className)}>
      <div className="h-2 w-full rounded-full bg-muted">
        <div
          className={cn("h-full rounded-full transition-all", barColor)}
          style={{ width: `${clamped}%` }}
        />
      </div>
      <div className="flex justify-between text-[10px] text-muted-foreground">
        <span>
          {new Intl.NumberFormat("pt-BR", {
            style: "currency",
            currency: "BRL",
          }).format(spent)}
        </span>
        <span>{percent.toFixed(0)}%</span>
        <span>
          {new Intl.NumberFormat("pt-BR", {
            style: "currency",
            currency: "BRL",
          }).format(limit)}
        </span>
      </div>
    </div>
  );
}
