"use client";

import { useState, useMemo } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { format } from "date-fns";
import { ptBR } from "date-fns/locale";
import {
  AlertCircle,
  ArrowLeft,
  Calendar,
  CreditCard,
  DollarSign,
  Receipt,
  CheckCircle,
  Wallet,
} from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
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
  const [payFromAccountId, setPayFromAccountId] = useState("");
  const [paymentAmount, setPaymentAmount] = useState(0);

  const { data: summary, isLoading: loadingSummary } = useInvoiceSummary(invoiceId);
  const { data: transactions, isLoading: loadingTx } = useInvoiceTransactions(invoiceId);
  const { data: accounts } = useAccounts();
  const { data: categories } = useCategories();
  const payMutation = usePayInvoiceDirect();

  const isLoading = loadingSummary || loadingTx;
  const invoice = summary?.invoice;
  const account = accounts?.find((item) => item.id === invoice?.accountId);

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

  if (!summary || !invoice) {
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
    invoice.status !== InvoiceStatus.Paid &&
    new Date(invoice.dueDate) < new Date();

  const canPay =
    invoice.status !== InvoiceStatus.Paid &&
    invoice.status !== InvoiceStatus.Open &&
    invoice.remainingAmount > 0;

  const cardDebt = account ? Math.abs(account.balance) : invoice.remainingAmount;
  const committedCredit = account?.committedCredit ?? cardDebt;
  const availableCredit =
    account?.availableCredit ?? Math.max((account?.creditLimit ?? 0) - committedCredit, 0);
  const futureReservedAmount = Math.max(committedCredit - invoice.totalAmount, 0);

  const handlePay = () => {
    if (!payFromAccountId || paymentAmount <= 0) return;
    payMutation.mutate(
      {
        data: {
          invoiceId,
          payFromAccountId,
          paymentDate: new Date().toISOString(),
          amount: paymentAmount,
        },
        remainingAmount: invoice.remainingAmount,
      },
      { onSuccess: () => setPayOpen(false) }
    );
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={`Fatura — ${invoice.accountName}`}
        description={`${format(new Date(invoice.referenceMonth + "-01"), "MMMM yyyy", { locale: ptBR })}`}
      >
        <Link href={`/credit-cards/${invoice.accountId}`}>
          <Button variant="outline" size="sm">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Voltar
          </Button>
        </Link>
        {canPay && (
          <Button
            size="sm"
            onClick={() => {
              setPaymentAmount(invoice.remainingAmount);
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
          value={fmt(invoice.totalAmount)}
          icon={DollarSign}
        />
        <StatCard
          title="Valor Pago"
          value={fmt(invoice.paidAmount)}
          icon={CheckCircle}
          variant="income"
        />
        <StatCard
          title="Restante"
          value={fmt(invoice.remainingAmount)}
          icon={CreditCard}
          variant={invoice.remainingAmount > 0 ? "expense" : "income"}
        />
        <StatCard
          title="Vencimento"
          value={format(new Date(invoice.dueDate), "dd/MM/yyyy")}
          icon={Calendar}
          variant={isOverdue ? "warning" : "default"}
        />
      </div>

      {account && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <StatCard
            title="Débito do Cartão"
            value={fmt(cardDebt, account.currency)}
            icon={CreditCard}
            variant={cardDebt > 0 ? "expense" : "default"}
          />
          <StatCard
            title="Limite Total"
            value={fmt(account.creditLimit ?? 0, account.currency)}
            icon={DollarSign}
          />
          <StatCard
            title="Limite Comprometido"
            value={fmt(committedCredit, account.currency)}
            icon={Receipt}
            variant={committedCredit > 0 ? "warning" : "default"}
          />
          <StatCard
            title="Limite Disponível"
            value={fmt(availableCredit, account.currency)}
            icon={Wallet}
            variant={availableCredit > 0 ? "income" : "expense"}
          />
        </div>
      )}

      <Alert>
        <AlertCircle className="h-4 w-4" />
        <AlertTitle>Esta tela mostra apenas uma fatura</AlertTitle>
        <AlertDescription>
          O total desta fatura corresponde somente às compras elegíveis deste período. O limite comprometido do cartão pode ser maior porque inclui parcelas futuras já reservadas. Hoje, {fmt(futureReservedAmount, account?.currency ?? "BRL")} do limite estão fora desta fatura específica.
        </AlertDescription>
      </Alert>

      {/* Period info */}
      <Card>
        <CardContent className="flex flex-wrap items-center gap-4 p-4">
          <div className="flex items-center gap-2 text-sm">
            <span className="text-muted-foreground">Referência:</span>
            <span className="font-medium capitalize">
              {format(new Date(invoice.referenceMonth + "-01"), "MMMM yyyy", { locale: ptBR })}
            </span>
          </div>
          <div className="flex items-center gap-2 text-sm">
            <span className="text-muted-foreground">Período:</span>
            <span>
              {format(new Date(invoice.periodStart), "dd/MM/yyyy", { locale: ptBR })} —{" "}
              {format(new Date(invoice.periodEnd), "dd/MM/yyyy", { locale: ptBR })}
            </span>
          </div>
          <InvoiceStatusBadge status={invoice.status} isOverdue={isOverdue} />
          <span className="text-sm text-muted-foreground">
            {invoice.transactionCount} transações
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
                  invoice.totalAmount > 0
                    ? (cat.total / invoice.totalAmount) * 100
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
                        <div className="space-y-1">
                          <p>{tx.description}</p>
                          {tx.installmentCount && tx.installmentCount > 1 && tx.installmentNumber ? (
                            <Badge variant="outline" className="text-[10px]">
                              Parcela {tx.installmentNumber}/{tx.installmentCount}
                            </Badge>
                          ) : null}
                        </div>
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
              Pagar fatura de {invoice.accountName} —{" "}
              {format(new Date(invoice.referenceMonth + "-01"), "MMMM yyyy", {
                locale: ptBR,
              })}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Conta de Débito</Label>
              <Select
                value={payFromAccountId}
                onValueChange={(v) => v && setPayFromAccountId(v)}
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
                Restante: {fmt(invoice.remainingAmount)}
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
                !payFromAccountId ||
                paymentAmount <= 0 ||
                paymentAmount > invoice.remainingAmount ||
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
