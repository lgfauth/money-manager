"use client";

import { useEffect } from "react";
import { useTheme } from "next-themes";
import { AlertCircle, Palette, Bell, DollarSign, RefreshCcw } from "lucide-react";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Switch } from "@/components/ui/switch";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { MoneyInput } from "@/components/shared/money-input";
import { ColorPicker } from "@/components/shared/color-picker";
import { PageHeader } from "@/components/shared/page-header";
import { CardGridSkeleton } from "@/components/shared/loading-skeleton";
import { useAccounts } from "@/hooks/use-accounts";
import { useReconcileCreditCards } from "@/hooks/use-invoices";
import { useSettings, useUpdateSettings } from "@/hooks/use-settings";
import { useSettingsStore } from "@/stores/settings-store";
import { AccountType } from "@/types/account";
import { currencies } from "@/config/currencies";

const DATE_FORMATS = [
  { value: "dd/MM/yyyy", label: "dd/MM/yyyy" },
  { value: "MM/dd/yyyy", label: "MM/dd/yyyy" },
  { value: "yyyy-MM-dd", label: "yyyy-MM-dd" },
];

const THEMES = [
  { value: "light", label: "Claro" },
  { value: "dark", label: "Escuro" },
  { value: "system", label: "Automático" },
];

export default function SettingsPage() {
  const { data: settings, isLoading } = useSettings();
  const { data: accounts } = useAccounts();
  const update = useUpdateSettings();
  const reconcileCards = useReconcileCreditCards();
  const { setTheme } = useTheme();
  const setStoreCurrency = useSettingsStore((s) => s.setCurrency);
  const creditCardCount =
    accounts?.filter((account) => account.type === AccountType.CreditCard).length ?? 0;

  // Sync theme from API to next-themes
  useEffect(() => {
    if (settings?.theme) {
      setTheme(settings.theme);
    }
  }, [settings?.theme, setTheme]);

  const handleUpdate = (partial: Record<string, unknown>) => {
    if (!settings) return;
    update.mutate({ ...settings, ...partial });
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <PageHeader title="Configurações" description="Personalize sua experiência" />
        <CardGridSkeleton count={3} />
      </div>
    );
  }

  if (!settings) return null;

  return (
    <div className="space-y-6">
      <PageHeader
        title="Configurações"
        description="Personalize sua experiência"
      />

      {/* Financial preferences */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base flex items-center gap-2">
            <DollarSign className="h-4 w-4" />
            Preferências Financeiras
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label>Moeda</Label>
              <Select
                value={settings.currency}
                onValueChange={(v) => {
                  if (!v) return;
                  handleUpdate({ currency: v });
                  setStoreCurrency(v);
                }}
              >
                <SelectTrigger className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {currencies.map((c) => (
                    <SelectItem key={c.code} value={c.code}>
                      {c.code} — {c.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label>Formato de Data</Label>
              <Select
                value={settings.dateFormat}
                onValueChange={(v) => v && handleUpdate({ dateFormat: v })}
              >
                <SelectTrigger className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {DATE_FORMATS.map((f) => (
                    <SelectItem key={f.value} value={f.value}>
                      {f.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label>Dia do Fechamento do Mês</Label>
              <Input
                type="number"
                min={1}
                max={28}
                value={settings.monthClosingDay}
                onChange={(e) =>
                  handleUpdate({
                    monthClosingDay: Math.min(
                      28,
                      Math.max(1, parseInt(e.target.value) || 1)
                    ),
                  })
                }
              />
            </div>

            <div className="space-y-2">
              <Label>Orçamento Padrão</Label>
              <MoneyInput
                value={settings.defaultBudget ?? 0}
                onChange={(v) => handleUpdate({ defaultBudget: v })}
                currencyCode={settings.currency}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Push notifications */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base flex items-center gap-2">
            <Bell className="h-4 w-4" />
            Notificações Push
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium">Habilitar Notificações</p>
              <p className="text-xs text-muted-foreground">
                Receba notificações sobre sua conta
              </p>
            </div>
            <Switch
              checked={settings.pushNotificationsEnabled}
              onCheckedChange={(v) =>
                handleUpdate({ pushNotificationsEnabled: v })
              }
            />
          </div>

          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium">
                Notificar Recorrentes Processadas
              </p>
              <p className="text-xs text-muted-foreground">
                Aviso quando transações recorrentes forem processadas
              </p>
            </div>
            <Switch
              checked={settings.notifyRecurringProcessed}
              onCheckedChange={(v) =>
                handleUpdate({ notifyRecurringProcessed: v })
              }
              disabled={!settings.pushNotificationsEnabled}
            />
          </div>

          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium">Lembrete Diário</p>
              <p className="text-xs text-muted-foreground">
                Resumo diário das suas finanças
              </p>
            </div>
            <Switch
              checked={settings.notifyDailyReminder}
              onCheckedChange={(v) =>
                handleUpdate({ notifyDailyReminder: v })
              }
              disabled={!settings.pushNotificationsEnabled}
            />
          </div>

          <Button
            variant="outline"
            size="sm"
            disabled={!settings.pushNotificationsEnabled}
          >
            Testar Notificação
          </Button>
        </CardContent>
      </Card>

      {/* Appearance */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base flex items-center gap-2">
            <Palette className="h-4 w-4" />
            Aparência
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label>Tema</Label>
            <div className="flex gap-2">
              {THEMES.map((t) => (
                <Button
                  key={t.value}
                  variant={settings.theme === t.value ? "default" : "outline"}
                  size="sm"
                  onClick={() => {
                    handleUpdate({ theme: t.value });
                    setTheme(t.value);
                  }}
                >
                  {t.label}
                </Button>
              ))}
            </div>
          </div>

          <div className="space-y-2">
            <Label>Cor de Destaque</Label>
            <ColorPicker
              value={settings.primaryColor}
              onChange={(v) => handleUpdate({ primaryColor: v })}
            />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base flex items-center gap-2">
            <RefreshCcw className="h-4 w-4" />
            Manutenção de Cartões
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <Alert>
            <AlertCircle className="h-4 w-4" />
            <AlertTitle>Quando usar a reconciliação</AlertTitle>
            <AlertDescription>
              Recalcule os totais de fatura e o limite comprometido quando houver divergência entre fatura atual, dívida do cartão e limite disponível. A rotina processa {creditCardCount} cartão(ões) deste usuário.
            </AlertDescription>
          </Alert>

          <div className="flex flex-wrap items-center gap-3">
            <Button
              onClick={() => reconcileCards.mutate()}
              disabled={reconcileCards.isPending || creditCardCount === 0}
            >
              <RefreshCcw className="mr-2 h-4 w-4" />
              {reconcileCards.isPending ? "Reconciliando..." : "Reconciliar cartões agora"}
            </Button>
            <p className="text-xs text-muted-foreground">
              Atualiza contas, faturas e métricas do dashboard após o processamento.
            </p>
          </div>

          {reconcileCards.data?.result ? (
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              <div className="rounded-lg border p-3">
                <p className="text-xs text-muted-foreground">Cartões processados</p>
                <p className="mt-1 text-lg font-semibold">{reconcileCards.data.result.accountsProcessed}</p>
              </div>
              <div className="rounded-lg border p-3">
                <p className="text-xs text-muted-foreground">Faturas recalculadas</p>
                <p className="mt-1 text-lg font-semibold">{reconcileCards.data.result.invoicesRecalculated}</p>
              </div>
              <div className="rounded-lg border p-3">
                <p className="text-xs text-muted-foreground">Contas atualizadas</p>
                <p className="mt-1 text-lg font-semibold">{reconcileCards.data.result.accountsUpdated}</p>
              </div>
              <div className="rounded-lg border p-3">
                <p className="text-xs text-muted-foreground">Limite comprometido</p>
                <p className="mt-1 text-lg font-semibold">
                  {new Intl.NumberFormat("pt-BR", {
                    style: "currency",
                    currency: settings.currency,
                  }).format(reconcileCards.data.result.totalCommittedCredit)}
                </p>
              </div>
            </div>
          ) : null}
        </CardContent>
      </Card>
    </div>
  );
}
