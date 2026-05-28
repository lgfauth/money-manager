"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { LineChartComponent } from "@/components/charts/line-chart";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";

interface CreditLimitExpenseData {
  cardName: string;
  availableLimit: number;
  expense: number;
}

interface CreditLimitExpenseChartProps {
  data: CreditLimitExpenseData[];
  totalAvailableLimit: number;
  totalExpense: number;
}

export function CreditLimitExpenseChart({
  data,
  totalAvailableLimit,
  totalExpense,
}: CreditLimitExpenseChartProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm font-medium">Limite vs Despesas</CardTitle>
      </CardHeader>
      <CardContent>
        {data.length === 0 ? (
          <p className="text-sm text-muted-foreground text-center py-8">
            Nenhum cartao de credito cadastrado.
          </p>
        ) : (
          <>
            <LineChartComponent
              data={data}
              xAxisKey="cardName"
              series={[
                {
                  dataKey: "availableLimit",
                  name: "Limite disponivel",
                  color: "oklch(0.74 0.14 166)",
                },
                {
                  dataKey: "expense",
                  name: "Despesa",
                  color: "oklch(0.63 0.24 25)",
                },
              ]}
              height={250}
              formatter={formatMonetaryValue}
            />

            <div className="mt-3 grid gap-2 sm:grid-cols-2">
              <div>
                <p className="text-xs text-muted-foreground">Limite disponivel total</p>
                <p className="text-sm font-semibold text-foreground">
                  {formatMonetaryValue(totalAvailableLimit)}
                </p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Despesas totais no cartao</p>
                <p className="text-sm font-semibold text-foreground">
                  {formatMonetaryValue(totalExpense)}
                </p>
              </div>
            </div>
          </>
        )}
      </CardContent>
    </Card>
  );
}
