"use client";

import { useState, useMemo } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { format } from "date-fns";
import { ptBR } from "date-fns/locale";
import {
  ArrowLeft,
  Calendar,
  CreditCard,
  DollarSign,
  Receipt,
  CheckCircle,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { MoneyInput } from "@/components/shared/money-input";
import { PageHeader } from "@/components/shared/page-header";
import { StatCard } from "@/components/shared/stat-card";
import { CardGridSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { InvoiceStatusBadge } from "@/components/credit-cards/invoice-status-badge";
import {
  useInvoiceSummary,
  useInvoiceTransactions,
  usePayInvoiceDirect,
} from "@/hooks/use-invoices";
import { useAccounts } from "@/hooks/use-accounts";
import { useCategories } from "@/hooks/use-categories";
import { AccountType } from "@/types/account";
import { InvoiceStatus } from "@/types/invoice";

function fmt(value: number, currency = "BRL"): string {
  return value.toLocaleString("pt-BR", { style: "currency", currency });
}

export default function InvoiceDetailsPage() {
  const { invoiceId } = useParams<{ invoiceId: string }>();
  const router = useRouter();
  const [payOpen, setPayOpen] = useState(false);
  const [sourceAccountId, setSourceAccountId] = useState("");
  const [paymentAmount, setPaymentAmount] = useState(0);

  const { data: summary, isLoading: loadingSummary } = useInvoiceSummary(invoiceId);
  const { data: transactions, isLoading: loadingTx } = useInvoiceTransactions(invoiceId);
  const { data: accounts } = useAccounts();
  const { data: categories } = useCategories();
  const payMutation = usePayInvoiceDirect();

  const isLoading = loadingSummary || loadingTx;

  const categoryColorMap = useMemo(() => {
    const map: Record<string, string> = {};
    categories?.forEach((c) => {
      map[c.id] = c.color;
    });
    return map;
  }, [categories]);

  // Group transactions by category
  const categoryBreakdown = useMemo(() => {
    if (!transactions) return [];
    const acc: Record<string, { name: string; total: number; count: number; color: string }> = {};
    for (const t of transactions) {
      if (!acc[t.categoryId]) {
        acc[t.categoryId] = {
          name: t.categoryName,
          total: 0,
          count: 0,
          color: categoryColorMap[t.categoryId] ?? "#64748b",
        };
      }
      acc[t.categoryId].total += t.amount;
      acc[t.categoryId].count += 1;
    }
    return Object.values(acc).sort((a, b) => b.total - a.total);
  }, [transactions, categoryColorMap]);

  const debitAccounts = accounts?.filter(
    (a) => a.type !== AccountType.CreditCard
  );

  if (isLoading) {
    return (
      <div className="space-y-6">
        <PageHeader title="Fatura" description="Carregando..." />
        <CardGridSkeleton count={4} />
      </div>
    );
  }

  if (!summary) {
    return (
      <EmptyState
        icon={Receipt}
        title="Fatura não encontrada"
        description="A fatura selecionada não existe."
        actionLabel="Voltar para Contas"
        onAction={() => router.push("/accounts")}
      />
    );
  }

  const isOverdue =
    summary.status !== InvoiceStatus.Paid &&
    new Date(summary.dueDate) < new Date();

  const canPay =
    summary.status !== InvoiceStatus.Paid &&
    summary.status !== InvoiceStatus.Open &&
    summary.remainingAmount > 0;

  const handlePay = () => {
    if (!sourceAccountId || paymentAmount <= 0) return;
    payMutation.mutate(
      {
        data: {
          invoiceId,
          sourceAccountId,
          paymentDate: new Date().toISOString(),
          amount: paymentAmount,
        },
        remainingAmount: summary.remainingAmount,
      },
      { onSuccess: () => setPayOpen(false) }
    );
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={`Fatura — ${summary.accountName}`}
        description={`${format(new Date(summary.referenceMonth + "-01"), "MMMM yyyy", { locale: ptBR })}`}
      >
        <Link href={`/credit-cards/${summary.accountId}`}>
          <Button variant="outline" size="sm">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Voltar
          </Button>
        </Link>
        {canPay && (
          <Button
            size="sm"
            onClick={() => {
              setPaymentAmount(summary.remainingAmount);
              setPayOpen(true);
            }}
          >
            Pagar Fatura
          </Button>
        )}
      </PageHeader>

      {/* Summary stat cards */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title="Total da Fatura"
          value={fmt(summary.totalAmount)}
          icon={DollarSign}
        />
        <StatCard
          title="Valor Pago"
          value={fmt(summary.paidAmount)}
          icon={CheckCircle}
          variant="income"
        />
        <StatCard
          title="Restante"
          value={fmt(summary.remainingAmount)}
          icon={CreditCard}
          variant={summary.remainingAmount > 0 ? "expense" : "income"}
        />
        <StatCard
          title="Vencimento"
          value={format(new Date(summary.dueDate), "dd/MM/yyyy")}
          icon={Calendar}
          variant={isOverdue ? "warning" : "default"}
        />
      </div>

      {/* Period info */}
      <Card>
        <CardContent className="flex flex-wrap items-center gap-4 p-4">
          <div className="flex items-center gap-2 text-sm">
            <span className="text-muted-foreground">Referência:</span>
            <span className="font-medium capitalize">
              {format(new Date(summary.referenceMonth + "-01"), "MMMM yyyy", { locale: ptBR })}
            </span>
          </div>
          <div className="flex items-center gap-2 text-sm">
            <span className="text-muted-foreground">Período:</span>
            <span>
              {format(new Date(summary.periodStart), "dd/MM/yyyy", { locale: ptBR })} —{" "}
              {format(new Date(summary.periodEnd), "dd/MM/yyyy", { locale: ptBR })}
            </span>
          </div>
          <InvoiceStatusBadge status={summary.status} isOverdue={isOverdue} />
          <span className="text-sm text-muted-foreground">
            {summary.transactionCount} transações
          </span>
        </CardContent>
      </Card>

      {/* Category breakdown */}
      {categoryBreakdown.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Detalhamento por Categoria</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {categoryBreakdown.map((cat) => {
                const percentage =
                  summary.totalAmount > 0
                    ? (cat.total / summary.totalAmount) * 100
                    : 0;
                return (
                  <div
                    key={cat.name}
                    className="flex flex-col gap-2 rounded-lg border p-3"
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <span
                          className="h-3 w-3 rounded-full shrink-0"
                          style={{ backgroundColor: cat.color }}
                        />
                        <span className="text-sm font-medium">{cat.name}</span>
                      </div>
                      <span className="text-sm font-semibold">
                        {fmt(cat.total)}
                      </span>
                    </div>
                    <Progress value={percentage} className="h-2" />
                    <div className="flex justify-between text-xs text-muted-foreground">
                      <span>{percentage.toFixed(1)}% do total</span>
                      <span>{cat.count} transações</span>
                    </div>
                  </div>
                );
              })}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Transactions table */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Transações da Fatura</CardTitle>
        </CardHeader>
        <CardContent>
          {!transactions || transactions.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-6">
              Nenhuma transação nesta fatura.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Data</TableHead>
                    <TableHead>Descrição</TableHead>
                    <TableHead className="hidden sm:table-cell">
                      Categoria
                    </TableHead>
                    <TableHead className="text-right">Valor</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {transactions.map((tx) => (
                    <TableRow key={tx.id}>
                      <TableCell className="text-muted-foreground">
                        {format(new Date(tx.date), "dd/MM/yyyy")}
                      </TableCell>
                      <TableCell className="font-medium">
                        {tx.description}
                      </TableCell>
                      <TableCell className="hidden sm:table-cell">
                        <div className="flex items-center gap-1.5">
                          <span
                            className="h-2.5 w-2.5 rounded-full"
                            style={{
                              backgroundColor:
                                categoryColorMap[tx.categoryId] ?? "#64748b",
                            }}
                          />
                          <span className="text-sm">{tx.categoryName}</span>
                        </div>
                      </TableCell>
                      <TableCell className="text-right font-medium text-expense">
                        {fmt(tx.amount)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Payment dialog */}
      <Dialog open={payOpen} onOpenChange={setPayOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Pagar Fatura</DialogTitle>
            <DialogDescription>
              Pagar fatura de {summary.accountName} —{" "}
              {format(new Date(summary.referenceMonth + "-01"), "MMMM yyyy", {
                locale: ptBR,
              })}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Conta de Débito</Label>
              <Select
                value={sourceAccountId}
                onValueChange={(v) => v && setSourceAccountId(v)}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Selecione a conta" />
                </SelectTrigger>
                <SelectContent>
                  {debitAccounts?.map((acc) => (
                    <SelectItem key={acc.id} value={acc.id}>
                      {acc.name} — {fmt(acc.balance, acc.currency)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Valor do Pagamento</Label>
              <MoneyInput value={paymentAmount} onChange={setPaymentAmount} />
              <p className="text-xs text-muted-foreground">
                Restante: {fmt(summary.remainingAmount)}
              </p>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setPayOpen(false)}>
              Cancelar
            </Button>
            <Button
              onClick={handlePay}
              disabled={
                !sourceAccountId ||
                paymentAmount <= 0 ||
                paymentAmount > summary.remainingAmount ||
                payMutation.isPending
              }
            >
              {payMutation.isPending ? "Pagando..." : "Confirmar Pagamento"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
