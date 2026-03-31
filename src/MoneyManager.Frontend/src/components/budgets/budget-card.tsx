"use client";

import type { BudgetItemResponseDto } from "@/types/budget";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { BudgetProgress } from "@/components/budgets/budget-progress";

interface BudgetCardProps {
  item: BudgetItemResponseDto;
}

export function BudgetCard({ item }: BudgetCardProps) {
  return (
    <Card className="rounded-xl">
      <CardHeader className="flex flex-row items-center gap-2 pb-2">
        <div
          className="h-3 w-3 rounded-full"
          style={{ backgroundColor: item.categoryColor }}
        />
        <CardTitle className="text-sm font-medium">
          {item.categoryName}
        </CardTitle>
      </CardHeader>
      <CardContent>
        <BudgetProgress spent={item.spentAmount} limit={item.limitAmount} />
      </CardContent>
    </Card>
  );
}
