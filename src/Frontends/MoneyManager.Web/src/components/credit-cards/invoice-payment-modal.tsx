"use client";

import { useEffect, useRef } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { format } from "date-fns";

import {
  payCreditCardInvoiceSchema,
  type PayCreditCardInvoiceFormData,
} from "@/lib/validators";
import { usePayCreditCardInvoice } from "@/hooks/use-credit-cards";
import { useAccounts } from "@/hooks/use-accounts";
import type { CreditCardInvoiceResponseDto } from "@/types/credit-card";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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
import { FormErrorSummary } from "@/components/shared/form-error-summary";

interface InvoicePaymentModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  cardId: string;
  invoice: CreditCardInvoiceResponseDto;
}

export function InvoicePaymentModal({
  open,
  onOpenChange,
  cardId,
  invoice,
}: InvoicePaymentModalProps) {
  const payInvoice = usePayCreditCardInvoice();
  const { data: accounts } = useAccounts();

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors, submitCount },
  } = useForm<PayCreditCardInvoiceFormData>({
    resolver: zodResolver(payCreditCardInvoiceSchema),
    defaultValues: {
      paidWithAccountId: "",
      paidAmount: invoice.totalAmount,
      paidAt: format(new Date(), "yyyy-MM-dd"),
    },
  });

  const selectedAccount = watch("paidWithAccountId");
  const paidAmount = watch("paidAmount");
  const resetMutationsRef = useRef(() => {});

  resetMutationsRef.current = () => {
    payInvoice.reset();
  };

  useEffect(() => {
    if (!open) return;
    resetMutationsRef.current();
    reset({
      paidWithAccountId: "",
      paidAmount: invoice.totalAmount,
      paidAt: format(new Date(), "yyyy-MM-dd"),
    });
  }, [open, invoice.totalAmount, reset]);

  const onSubmit = (data: PayCreditCardInvoiceFormData) => {
    payInvoice.mutate(
      { cardId, invoiceId: invoice.id, data },
      { onSuccess: () => onOpenChange(false) }
    );
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Pagar fatura</DialogTitle>
          <DialogDescription>
            Registre o pagamento desta fatura. Um lançamento de débito será
            criado na conta selecionada.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <FormErrorSummary
            errors={errors}
            submitCount={submitCount}
            apiError={payInvoice.error}
          />

          <div className="space-y-2">
            <Label>Conta pagadora</Label>
            <Select
              value={selectedAccount || null}
              onValueChange={(v) => v && setValue("paidWithAccountId", v)}
            >
              <SelectTrigger className="w-full">
                <SelectValue placeholder="Selecione a conta" />
              </SelectTrigger>
              <SelectContent>
                {accounts?.map((acc) => (
                  <SelectItem key={acc.id} value={acc.id}>
                    {acc.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {errors.paidWithAccountId && (
              <p className="text-xs text-destructive">
                {errors.paidWithAccountId.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label>Valor pago</Label>
            <MoneyInput
              value={paidAmount}
              onChange={(v) => setValue("paidAmount", v)}
              currencyCode={invoice.currency}
            />
            {errors.paidAmount && (
              <p className="text-xs text-destructive">
                {errors.paidAmount.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="paidAt">Data do pagamento</Label>
            <Input id="paidAt" type="date" {...register("paidAt")} />
            {errors.paidAt && (
              <p className="text-xs text-destructive">{errors.paidAt.message}</p>
            )}
          </div>

          <DialogFooter>
            <Button
              type="submit"
              className="w-full"
              disabled={payInvoice.isPending}
            >
              {payInvoice.isPending ? "Registrando..." : "Confirmar pagamento"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
