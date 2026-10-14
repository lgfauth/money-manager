"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import { cn } from "@/lib/utils";
import type { AccountResponseDto } from "@/types/account";

const accountTypeLabels: Record<string, string> = {
  Checking: "Conta corrente",
  Savings: "Poupança",
  Cash: "Dinheiro",
  Investment: "Investimento",
};

interface AccountsChartProps {
  accounts: AccountResponseDto[];
}

export function AccountsChart({ accounts }: AccountsChartProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const sortedAccounts = [...accounts].sort((a, b) => b.balance - a.balance);
  const maxBalance = Math.max(...accounts.map((account) => Math.abs(account.balance)), 0);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm font-medium">Saldos por Conta</CardTitle>
      </CardHeader>
      <CardContent>
        {sortedAccounts.length === 0 ? (
          <p className="text-sm text-muted-foreground text-center py-8">Nenhuma conta cadastrada.</p>
        ) : (
          <ul className="space-y-3">
            {sortedAccounts.map((account) => (
              <li key={account.id} className="space-y-1">
                <div className="flex items-center justify-between gap-2 text-sm">
                  <span className="flex min-w-0 items-center gap-2">
                    <span
                      className="h-2.5 w-2.5 rounded-full shrink-0"
                      style={{ backgroundColor: account.color || "#00C896" }}
                    />
                    <span className="truncate font-medium">{account.name}</span>
                    <span className="hidden text-xs text-muted-foreground sm:inline">
                      {accountTypeLabels[account.type] ?? account.type}
                    </span>
                  </span>
                  <span
                    className={cn("whitespace-nowrap font-medium", account.balance < 0 && "text-expense")}
                  >
                    {formatMonetaryValue(account.balance, account.currency)}
                  </span>
                </div>
                <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted">
                  <div
                    className={cn("h-full rounded-full", account.balance < 0 && "bg-expense")}
                    style={{
                      width: `${maxBalance > 0 ? (Math.abs(account.balance) / maxBalance) * 100 : 0}%`,
                      backgroundColor: account.balance < 0 ? undefined : account.color || "#00C896",
                    }}
                  />
                </div>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
