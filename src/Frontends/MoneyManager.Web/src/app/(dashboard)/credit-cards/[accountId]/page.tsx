"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { format } from "date-fns";
import { ptBR } from "date-fns/locale";
import { AlertCircle, ArrowLeft, CreditCard, DollarSign, Lock, Receipt, Wallet } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { StatCard } from "@/components/shared/stat-card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { PageHeader } from "@/components/shared/page-header";
import { CardGridSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { CreditLimitGauge } from "@/components/credit-cards/credit-limit-gauge";
import { InvoiceCard } from "@/components/credit-cards/invoice-card";
import { InvoiceStatusBadge } from "@/components/credit-cards/invoice-status-badge";
import { InvoicePaymentModal } from "@/components/accounts/invoice-payment-modal";
import { InvoiceHistoryChart } from "@/components/charts";
import { useAccounts } from "@/hooks/use-accounts";
import {
  useOpenInvoice,
  useAccountInvoices,
  useOverdueInvoices,
  useCloseInvoice,
} from "@/hooks/use-invoices";
import { AccountType } from "@/types/account";
import { InvoiceStatus } from "@/types/invoice";

function fmt(value: number, currency = "BRL"): string {
  return value.toLocaleString("pt-BR", { style: "currency", currency });
}

export default function CreditCardDashboardPage() {
  const { accountId } = useParams<{ accountId: string }>();
  const router = useRouter();
  const [payModalOpen, setPayModalOpen] = useState(false);
  const [closeConfirmOpen, setCloseConfirmOpen] = useState(false);

  const { data: accounts, isLoading: loadingAccounts } = useAccounts();
  const account = accounts?.find((a) => a.id === accountId);

  const { data: openInvoice } = useOpenInvoice(accountId);
  const { data: invoices, isLoading: loadingInvoices } =
    useAccountInvoices(accountId);
  const { data: overdueInvoices } = useOverdueInvoices();

  const closeMutation = useCloseInvoice();

  const isLoading = loadingAccounts || loadingInvoices;

  if (isLoading) {
    return (
      <div className="space-y-6">
        <PageHeader title="Cartão de Crédito" description="Carregando..." />
        <CardGridSkeleton count={4} />
      </div>
    );
  }

  if (!account || account.type !== AccountType.CreditCard) {
    return (
      <div className="space-y-6">
        <EmptyState
          icon={CreditCard}
          title="Cartão não encontrado"
          description="A conta selecionada não existe ou não é um cartão de crédito."
          actionLabel="Voltar para Contas"
          onAction={() => router.push("/accounts")}
        />
      </div>
    );
  }

  const usedAmount = account.committedCredit ?? Math.abs(account.balance);
  const creditLimit = account.creditLimit ?? 0;
  const availableCredit =
    account.availableCredit ?? Math.max(creditLimit - usedAmount, 0);
  const cardDebt = Math.abs(account.balance);
  const currentInvoiceAmount = openInvoice?.totalAmount ?? 0;
  const currentInvoiceRemaining = openInvoice?.remainingAmount ?? 0;
  const futureReservedAmount = Math.max(usedAmount - currentInvoiceAmount, 0);

  // Find the next invoice to pay (overdue for this account, or first closed)
  const overdueForAccount = overdueInvoices?.filter(
    (inv) => inv.accountId === accountId
  );
  const nextDueInvoice =
    overdueForAccount?.[0] ??
    invoices?.find(
      (inv) =>
        inv.status === InvoiceStatus.Closed ||
        inv.status === InvoiceStatus.PartiallyPaid
    );

  const canManualCloseOpen =
    !!openInvoice &&
    openInvoice.status === InvoiceStatus.Open &&
    new Date() >= new Date(openInvoice.periodEnd);

  // History: all invoices sorted by referenceMonth desc
  const invoiceHistory = [...(invoices ?? [])].sort(
    (a, b) => b.referenceMonth.localeCompare(a.referenceMonth)
  );

  const invoiceHistoryChartData = invoiceHistory
    .slice()
    .reverse()
    .map((invoice) => ({
      time: invoice.dueDate.slice(0, 10),
      invoiceTotal: invoice.totalAmount,
      paidAmount: invoice.paidAmount,
    }));

  return (
    <div className="space-y-6">
      <PageHeader
        title={account.name}
        description="Dashboard do cartão de crédito"
      >
        <Link href="/accounts">
          <Button variant="outline" size="sm">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Voltar para Contas
          </Button>
        </Link>
      </PageHeader>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          title="Fatura Atual"
          value={fmt(currentInvoiceAmount, account.currency)}
          icon={Receipt}
          variant={currentInvoiceAmount > 0 ? "expense" : "default"}
        />
        <StatCard
          title="Débito do Cartão"
          value={fmt(cardDebt, account.currency)}
          icon={CreditCard}
          variant={cardDebt > 0 ? "expense" : "default"}
        />
        <StatCard
          title="Limite Comprometido"
          value={fmt(usedAmount, account.currency)}
          icon={DollarSign}
          variant={usedAmount > 0 ? "warning" : "default"}
        />
        <StatCard
          title="Limite Disponível"
          value={fmt(availableCredit, account.currency)}
          icon={Wallet}
          variant={availableCredit > 0 ? "income" : "expense"}
        />
      </div>

      <Alert>
        <AlertCircle className="h-4 w-4" />
        <AlertTitle>Leitura dos números do cartão</AlertTitle>
        <AlertDescription>
          A fatura atual mostra apenas as compras do período em aberto. O débito do cartão representa a dívida contabilizada do cartão, enquanto o limite comprometido inclui a fatura atual e parcelas futuras já reservadas. Neste momento, {fmt(futureReservedAmount, account.currency)} do limite estão reservados fora da fatura atual e {fmt(currentInvoiceRemaining, account.currency)} permanecem em aberto na fatura corrente.
        </AlertDescription>
      </Alert>

      {canManualCloseOpen && (
        <Alert className="border-yellow-500/50 bg-yellow-500/5">
          <Lock className="h-4 w-4 text-yellow-600" />
          <AlertTitle className="text-yellow-700 dark:text-yellow-400">
            Fatura não fechada automaticamente
          </AlertTitle>
          <AlertDescription className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <span>
              A data de fechamento desta fatura já passou e o fechamento automático não ocorreu.
              Feche manualmente para iniciar um novo período.
            </span>
            <Button
              size="sm"
              variant="outline"
              className="shrink-0 border-yellow-500 text-yellow-700 hover:bg-yellow-500/10 dark:text-yellow-400"
              onClick={() => setCloseConfirmOpen(true)}
              disabled={closeMutation.isPending}
            >
              <Lock className="mr-2 h-4 w-4" />
              {closeMutation.isPending ? "Fechando..." : "Fechar fatura manualmente e abrir novo período de fatura"}
            </Button>
          </AlertDescription>
        </Alert>
      )}

      {/* Close invoice confirmation dialog */}
      {openInvoice && (
        <Dialog open={closeConfirmOpen} onOpenChange={setCloseConfirmOpen}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Fechar Fatura Manualmente</DialogTitle>
              <DialogDescription>
                A fatura de <strong>{account.name}</strong> será fechada agora.
                Um novo período de fatura será aberto automaticamente.
                Novas transações serão lançadas na próxima fatura.
              </DialogDescription>
            </DialogHeader>
            <DialogFooter>
              <Button variant="outline" onClick={() => setCloseConfirmOpen(false)}>
                Cancelar
              </Button>
              <Button
                onClick={() =>
                  closeMutation.mutate(openInvoice.id, {
                    onSuccess: () => setCloseConfirmOpen(false),
                  })
                }
                disabled={closeMutation.isPending}
              >
                {closeMutation.isPending ? "Fechando..." : "Confirmar Fechamento"}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}

      {/* Top section: gauge + invoice cards */}
      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        {/* Credit limit gauge */}
        <CreditLimitGauge
          creditLimit={creditLimit}
          usedAmount={usedAmount}
          currency={account.currency}
        />

        {/* Open invoice */}
        {openInvoice ? (
          <InvoiceCard
            invoice={openInvoice}
            currency={account.currency}
            label="Fatura Atual"
          />
        ) : (
          <Card>
            <CardContent className="flex items-center justify-center p-6 text-sm text-muted-foreground">
              Nenhuma fatura aberta
            </CardContent>
          </Card>
        )}

        {/* Next due invoice */}
        {nextDueInvoice ? (
          <InvoiceCard
            invoice={nextDueInvoice}
            currency={account.currency}
            label="Próxima a Vencer"
            onPay={() => setPayModalOpen(true)}
          />
        ) : (
          <Card>
            <CardContent className="flex items-center justify-center p-6 text-sm text-muted-foreground">
              Nenhuma fatura pendente
            </CardContent>
          </Card>
        )}
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Evolução de Faturas</CardTitle>
        </CardHeader>
        <CardContent>
          {invoiceHistoryChartData.length > 0 ? (
            <InvoiceHistoryChart data={invoiceHistoryChartData} height={260} />
          ) : (
            <p className="text-sm text-muted-foreground text-center py-6">
              Sem histórico suficiente para gráfico.
            </p>
          )}
        </CardContent>
      </Card>

      {/* Invoice history table */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Histórico de Faturas</CardTitle>
        </CardHeader>
        <CardContent>
          {invoiceHistory.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-6">
              Nenhuma fatura encontrada.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Mês</TableHead>
                    <TableHead className="hidden sm:table-cell">Período</TableHead>
                    <TableHead className="text-right">Total</TableHead>
                    <TableHead className="text-right hidden sm:table-cell">
                      Pago
                    </TableHead>
                    <TableHead className="text-right hidden md:table-cell">
                      Restante
                    </TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead className="text-right">Ações</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {invoiceHistory.map((inv) => {
                    const isOverdue =
                      inv.status !== InvoiceStatus.Paid &&
                      new Date(inv.dueDate) < new Date();
                    return (
                      <TableRow key={inv.id}>
                        <TableCell className="font-medium">
                          {format(
                            new Date(inv.referenceMonth + "-01"),
                            "MMM yyyy",
                            { locale: ptBR }
                          )}
                        </TableCell>
                        <TableCell className="hidden sm:table-cell text-muted-foreground">
                          {format(new Date(inv.periodStart), "dd/MM")} —{" "}
                          {format(new Date(inv.periodEnd), "dd/MM")}
                        </TableCell>
                        <TableCell className="text-right font-medium">
                          {fmt(inv.totalAmount, account.currency)}
                        </TableCell>
                        <TableCell className="text-right hidden sm:table-cell text-income">
                          {fmt(inv.paidAmount, account.currency)}
                        </TableCell>
                        <TableCell className="text-right hidden md:table-cell text-expense">
                          {fmt(inv.remainingAmount, account.currency)}
                        </TableCell>
                        <TableCell>
                          <InvoiceStatusBadge
                            status={inv.status}
                            isOverdue={isOverdue}
                          />
                        </TableCell>
                        <TableCell className="text-right">
                          <Link
                            href={`/invoices/${inv.id}`}
                            className="text-xs text-primary hover:underline"
                          >
                            Ver Detalhes
                          </Link>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Payment modal */}
      <InvoicePaymentModal
        open={payModalOpen}
        onOpenChange={setPayModalOpen}
        creditCardAccount={account}
      />
    </div>
  );
}
