"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { BarChartComponent } from "@/components/charts/bar-chart";
import { formatReportMonth } from "@/hooks/use-reports";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import type { ReportUpcomingInstallment } from "@/types/report";

interface UpcomingInstallmentsCardProps {
  currency: string;
  installments: ReportUpcomingInstallment[];
}

export function UpcomingInstallmentsCard({ currency, installments }: UpcomingInstallmentsCardProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const formatMoney = (value: number) => formatMonetaryValue(value, currency);
  const total = installments.reduce((sum, installment) => sum + installment.total, 0);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Parcelas Futuras</CardTitle>
        <p className="text-sm text-muted-foreground">
          {installments.length > 0
            ? `${formatMoney(total)} já comprometidos no cartão nos próximos meses`
            : "Compras parceladas que ainda vão cair nas próximas faturas"}
        </p>
      </CardHeader>
      <CardContent>
        {installments.length > 0 ? (
          <BarChartComponent
            data={installments.map((installment) => ({
              name: formatReportMonth(installment.month),
              value: installment.total,
            }))}
            color="var(--color-expense)"
            height={220}
            formatter={formatMoney}
          />
        ) : (
          <p className="text-sm text-muted-foreground text-center py-10">Nenhuma parcela futura.</p>
        )}
      </CardContent>
    </Card>
  );
}
