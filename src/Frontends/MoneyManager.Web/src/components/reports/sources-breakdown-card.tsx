"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import { cn } from "@/lib/utils";
import type { ReportAccountBreakdown, ReportCreditCardBreakdown } from "@/types/report";

interface SourcesBreakdownCardProps {
  currency: string;
  accounts: ReportAccountBreakdown[];
  creditCards: ReportCreditCardBreakdown[];
}

export function SourcesBreakdownCard({ currency, accounts, creditCards }: SourcesBreakdownCardProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const formatMoney = (value: number) => formatMonetaryValue(value, currency);
  const maxCardTotal = Math.max(...creditCards.map((card) => card.total), 0);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Contas e Cartões</CardTitle>
        <p className="text-sm text-muted-foreground">Entradas e saídas por origem, sem transferências</p>
      </CardHeader>
      <CardContent className="space-y-6">
        {accounts.length === 0 && creditCards.length === 0 && (
          <p className="text-sm text-muted-foreground text-center py-6">Nenhuma movimentação no período.</p>
        )}

        {accounts.length > 0 && (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-muted-foreground">
                  <th className="pb-2 text-left font-medium">Conta</th>
                  <th className="pb-2 text-right font-medium">Entradas</th>
                  <th className="pb-2 text-right font-medium">Saídas</th>
                  <th className="pb-2 text-right font-medium">Líquido</th>
                </tr>
              </thead>
              <tbody>
                {accounts.map((account) => {
                  const net = account.income - account.expense;
                  return (
                    <tr key={account.accountId} className="border-b last:border-0">
                      <td className="py-2.5">
                        <div className="flex items-center gap-2">
                          <span
                            className="h-3 w-3 rounded-full shrink-0"
                            style={{ backgroundColor: account.color }}
                          />
                          <span className="font-medium">{account.accountName}</span>
                        </div>
                      </td>
                      <td className="py-2.5 text-right whitespace-nowrap">{formatMoney(account.income)}</td>
                      <td className="py-2.5 text-right whitespace-nowrap">{formatMoney(account.expense)}</td>
                      <td
                        className={cn(
                          "py-2.5 text-right font-medium whitespace-nowrap",
                          net >= 0 ? "text-income" : "text-expense"
                        )}
                      >
                        {formatMoney(net)}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        {creditCards.length > 0 && (
          <div className="space-y-3">
            <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Gastos no cartão</p>
            {creditCards.map((card) => (
              <div key={card.creditCardId} className="space-y-1">
                <div className="flex items-center justify-between gap-2 text-sm">
                  <span className="font-medium">{card.creditCardName}</span>
                  <span className="font-medium whitespace-nowrap">{formatMoney(card.total)}</span>
                </div>
                <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                  <div
                    className="h-full rounded-full"
                    style={{
                      width: `${maxCardTotal > 0 ? Math.max((card.total / maxCardTotal) * 100, 0) : 0}%`,
                      backgroundColor: card.color,
                    }}
                  />
                </div>
                <p className="text-xs text-muted-foreground">{card.transactionCount} lançamento(s)</p>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
