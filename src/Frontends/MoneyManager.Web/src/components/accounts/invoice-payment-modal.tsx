"use client";

import { useState } from "react";
import { CreditCard } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
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
import { useAccounts } from "@/hooks/use-accounts";
import { useOpenInvoice, usePayInvoice } from "@/hooks/use-invoices";
import { AccountType, type AccountResponseDto } from "@/types/account";

interface InvoicePaymentModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  creditCardAccount: AccountResponseDto;
}

export function InvoicePaymentModal({
  open,
  onOpenChange,
  creditCardAccount,
}: InvoicePaymentModalProps) {
  const { data: invoice } = useOpenInvoice(
    open ? creditCardAccount.id : undefined
  );
  const { data: accounts } = useAccounts();
  const payInvoice = usePayInvoice();

  const [amount, setAmount] = useState(0);
  const [paymentAccountId, setPaymentAccountId] = useState("");

  const payableAccounts = accounts?.filter(
    (a) => a.type !== AccountType.CreditCard && a.id !== creditCardAccount.id
  );

  const remainingAmount = invoice?.remainingAmount ?? 0;

  const handleSubmit = () => {
    if (!invoice || !paymentAccountId || amount <= 0) return;
    payInvoice.mutate(
      {
        invoiceId: invoice.id,
        amount,
        paymentAccountId,
        paymentDate: new Date().toISOString().split("T")[0],
        remainingAmount,
      },
      {
        onSuccess: () => {
          onOpenChange(false);
          setAmount(0);
          setPaymentAccountId("");
        },
      }
    );
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <CreditCard className="h-4 w-4" />
            Pagar Fatura — {creditCardAccount.name}
          </DialogTitle>
          <DialogDescription>
            {invoice
              ? `Fatura ${invoice.referenceMonth} · Valor restante: ${new Intl.NumberFormat("pt-BR", {
                  style: "currency",
                  currency: creditCardAccount.currency,
                }).format(remainingAmount)}`
              : "Carregando fatura..."}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-2">
          <div className="space-y-2">
            <Label>Conta pagadora</Label>
            <Select value={paymentAccountId} onValueChange={(v) => setPaymentAccountId(v ?? "")}>
              <SelectTrigger>
                <SelectValue placeholder="Selecione uma conta" />
              </SelectTrigger>
              <SelectContent>
                {payableAccounts?.map((a) => (
                  <SelectItem key={a.id} value={a.id}>
                    {a.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label>Valor do pagamento</Label>
            <MoneyInput
              value={amount}
              onChange={setAmount}
              currencyCode={creditCardAccount.currency}
            />
            {remainingAmount > 0 && (
              <Button
                type="button"
                variant="link"
                size="sm"
                className="h-auto p-0 text-xs"
                onClick={() => setAmount(remainingAmount)}
              >
                Pagar valor total ({new Intl.NumberFormat("pt-BR", {
                  style: "currency",
                  currency: creditCardAccount.currency,
                }).format(remainingAmount)})
              </Button>
            )}
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancelar
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={
              !invoice ||
              !paymentAccountId ||
              amount <= 0 ||
              amount > remainingAmount ||
              payInvoice.isPending
            }
          >
            {payInvoice.isPending ? "Registrando..." : "Confirmar Pagamento"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
