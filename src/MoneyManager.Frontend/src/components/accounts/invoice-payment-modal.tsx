"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { queryKeys } from "@/lib/query-client";
import { format } from "date-fns";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { MoneyInput } from "@/components/shared/money-input";

import type { AccountResponseDto } from "@/types/account";
import { AccountType } from "@/types/account";
import type {
  CreditCardInvoiceResponseDto,
  InvoicePaymentRequestDto,
} from "@/types/invoice";
import { InvoiceStatus } from "@/types/invoice";

interface InvoicePaymentModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  creditCardAccount: AccountResponseDto;
}

const statusLabels: Record<string, string> = {
  Open: "Aberta",
  Closed: "Fechada",
  Paid: "Paga",
  PartiallyPaid: "Parcial",
  Overdue: "Vencida",
};

const statusVariants: Record<string, "default" | "secondary" | "destructive" | "outline"> = {
  Open: "secondary",
  Closed: "default",
  Paid: "outline",
  PartiallyPaid: "default",
  Overdue: "destructive",
};

export function InvoicePaymentModal({
  open,
  onOpenChange,
  creditCardAccount,
}: InvoicePaymentModalProps) {
  const qc = useQueryClient();
  const [selectedInvoiceId, setSelectedInvoiceId] = useState<string>("");
  const [sourceAccountId, setSourceAccountId] = useState<string>("");
  const [amount, setAmount] = useState(0);

  const { data: invoices } = useQuery({
    queryKey: queryKeys.invoices(creditCardAccount.id),
    queryFn: () =>
      apiClient.get<CreditCardInvoiceResponseDto[]>(
        `/api/credit-card-invoices/account/${creditCardAccount.id}`
      ),
    enabled: open,
  });

  const { data: accounts } = useQuery({
    queryKey: queryKeys.accounts,
    queryFn: () => apiClient.get<AccountResponseDto[]>("/api/accounts"),
    enabled: open,
  });

  const payInvoice = useMutation({
    mutationFn: (data: InvoicePaymentRequestDto) =>
      apiClient.post<void>("/api/credit-card-invoices/pay", data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.accounts });
      qc.invalidateQueries({
        queryKey: queryKeys.invoices(creditCardAccount.id),
      });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Pagamento registrado com sucesso");
      onOpenChange(false);
    },
    onError: () => toast.error("Erro ao registrar pagamento"),
  });

  const debitAccounts = accounts?.filter(
    (a) => a.type !== AccountType.CreditCard && a.id !== creditCardAccount.id
  );

  const payableInvoices = invoices?.filter(
    (inv) =>
      inv.status === InvoiceStatus.Closed ||
      inv.status === InvoiceStatus.Overdue ||
      inv.status === InvoiceStatus.PartiallyPaid
  );

  const selectedInvoice = payableInvoices?.find(
    (inv) => inv.id === selectedInvoiceId
  );

  const handlePay = () => {
    if (!selectedInvoiceId || !sourceAccountId || amount <= 0) return;
    payInvoice.mutate({
      invoiceId: selectedInvoiceId,
      sourceAccountId,
      paymentDate: new Date().toISOString(),
      amount,
    });
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Pagar Fatura</DialogTitle>
          <DialogDescription>
            Pagar fatura de {creditCardAccount.name}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-2">
            <Label>Fatura</Label>
            {payableInvoices && payableInvoices.length > 0 ? (
              <Select
                value={selectedInvoiceId}
                onValueChange={(v) => {
                  if (!v) return;
                  setSelectedInvoiceId(v);
                  const inv = payableInvoices.find((i) => i.id === v);
                  if (inv) setAmount(inv.remainingAmount);
                }}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Selecione uma fatura" />
                </SelectTrigger>
                <SelectContent>
                  {payableInvoices.map((inv) => (
                    <SelectItem key={inv.id} value={inv.id}>
                      <span className="flex items-center gap-2">
                        {format(new Date(inv.referenceMonth), "MM/yyyy")}
                        <Badge variant={statusVariants[inv.status]}>
                          {statusLabels[inv.status]}
                        </Badge>
                        <span className="text-muted-foreground">
                          {new Intl.NumberFormat("pt-BR", {
                            style: "currency",
                            currency: creditCardAccount.currency,
                          }).format(inv.remainingAmount)}
                        </span>
                      </span>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            ) : (
              <p className="text-sm text-muted-foreground">
                Nenhuma fatura pendente de pagamento.
              </p>
            )}
          </div>

          {selectedInvoice && (
            <>
              <div className="space-y-2">
                <Label>Conta de Debito</Label>
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
                        {acc.name} —{" "}
                        {new Intl.NumberFormat("pt-BR", {
                          style: "currency",
                          currency: acc.currency,
                        }).format(acc.balance)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label>Valor do Pagamento</Label>
                <MoneyInput
                  value={amount}
                  onChange={setAmount}
                  currencyCode={creditCardAccount.currency}
                />
                <p className="text-xs text-muted-foreground">
                  Valor restante:{" "}
                  {new Intl.NumberFormat("pt-BR", {
                    style: "currency",
                    currency: creditCardAccount.currency,
                  }).format(selectedInvoice.remainingAmount)}
                </p>
              </div>
            </>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancelar
          </Button>
          <Button
            onClick={handlePay}
            disabled={
              !selectedInvoiceId ||
              !sourceAccountId ||
              amount <= 0 ||
              payInvoice.isPending
            }
          >
            {payInvoice.isPending ? "Pagando..." : "Pagar"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
