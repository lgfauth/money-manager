"use client";

import { useEffect, useRef } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { format } from "date-fns";

import {
  creditCardTransactionSchema,
  type CreditCardTransactionFormData,
} from "@/lib/validators";
import {
  useCreateCreditCardTransaction,
  useCreditCards,
} from "@/hooks/use-credit-cards";
import { useCategories } from "@/hooks/use-categories";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { DialogFooter } from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { MoneyInput } from "@/components/shared/money-input";
import { FormErrorSummary } from "@/components/shared/form-error-summary";

interface CreditCardTransactionFormBodyProps {
  open: boolean;
  defaultCardId?: string;
  onClose: () => void;
}

export function CreditCardTransactionFormBody({
  open,
  defaultCardId,
  onClose,
}: CreditCardTransactionFormBodyProps) {
  const createTx = useCreateCreditCardTransaction();
  const { data: cards } = useCreditCards();
  const { data: categories } = useCategories();

  const expenseCategories = categories?.filter((c) => c.type === "Expense");

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors, submitCount },
  } = useForm<CreditCardTransactionFormData>({
    resolver: zodResolver(creditCardTransactionSchema),
    defaultValues: {
      creditCardId: defaultCardId ?? "",
      description: "",
      categoryId: "",
      purchaseDate: format(new Date(), "yyyy-MM-dd"),
      totalAmount: 0,
      totalInstallments: 1,
      firstInstallmentOnCurrentInvoice: true,
    },
  });

  const cardId = watch("creditCardId");
  const categoryId = watch("categoryId");
  const totalAmount = watch("totalAmount");
  const installments = watch("totalInstallments");
  const firstOnCurrent = watch("firstInstallmentOnCurrentInvoice");
  const selectedCard = cards?.find((c) => c.id === cardId);

  const resetMutationsRef = useRef(() => {});
  resetMutationsRef.current = () => createTx.reset();

  useEffect(() => {
    if (!open) return;
    resetMutationsRef.current();
    reset({
      creditCardId: defaultCardId ?? "",
      description: "",
      categoryId: "",
      purchaseDate: format(new Date(), "yyyy-MM-dd"),
      totalAmount: 0,
      totalInstallments: 1,
      firstInstallmentOnCurrentInvoice: true,
    });
  }, [open, defaultCardId, reset]);

  const onSubmit = (data: CreditCardTransactionFormData) => {
    const payload = {
      ...data,
      categoryId: data.categoryId || undefined,
      clientRequestId: crypto.randomUUID(),
    };
    createTx.mutate(payload, {
      onSuccess: () => onClose(),
    });
  };

  const installmentValue =
    installments > 0 ? totalAmount / installments : totalAmount;

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <FormErrorSummary
        errors={errors}
        submitCount={submitCount}
        apiError={createTx.error}
      />

      <div className="space-y-2">
        <Label>Cartão</Label>
        <Select
          value={cardId || null}
          onValueChange={(v) => v && setValue("creditCardId", v)}
        >
          <SelectTrigger className="w-full">
            <SelectValue placeholder="Selecione o cartão">
              {(value: string) => cards?.find((c) => c.id === value)?.name}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            {cards?.map((card) => (
              <SelectItem key={card.id} value={card.id}>
                <span className="flex items-center gap-2">
                  <span
                    className="h-2 w-2 rounded-full"
                    style={{ backgroundColor: card.color }}
                  />
                  {card.name}
                </span>
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {errors.creditCardId && (
          <p className="text-xs text-destructive">
            {errors.creditCardId.message}
          </p>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="description">Descrição</Label>
        <Input
          id="description"
          placeholder="Ex: Mercado, eletrônico..."
          {...register("description")}
        />
        {errors.description && (
          <p className="text-xs text-destructive">
            {errors.description.message}
          </p>
        )}
      </div>

      <div className="space-y-2">
        <Label>Categoria</Label>
        <Select
          value={categoryId || null}
          onValueChange={(v) => v && setValue("categoryId", v)}
        >
          <SelectTrigger className="w-full">
            <SelectValue placeholder="Selecione (opcional)">
              {(value: string) => {
                const cat = expenseCategories?.find((c) => c.id === value);
                if (!cat) return null;
                return (
                  <span className="flex items-center gap-2">
                    <span
                      className="h-2 w-2 rounded-full"
                      style={{ backgroundColor: cat.color }}
                    />
                    {cat.name}
                  </span>
                );
              }}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            {expenseCategories?.map((cat) => (
              <SelectItem key={cat.id} value={cat.id}>
                <span className="flex items-center gap-2">
                  <span
                    className="h-2 w-2 rounded-full"
                    style={{ backgroundColor: cat.color }}
                  />
                  {cat.name}
                </span>
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="space-y-2">
        <Label htmlFor="purchaseDate">Data da compra</Label>
        <Input id="purchaseDate" type="date" {...register("purchaseDate")} />
        {errors.purchaseDate && (
          <p className="text-xs text-destructive">
            {errors.purchaseDate.message}
          </p>
        )}
      </div>

      <div className="space-y-2">
        <Label>Valor total</Label>
        <MoneyInput
          value={totalAmount}
          onChange={(v) => setValue("totalAmount", v)}
          currencyCode={selectedCard?.currency ?? "BRL"}
        />
        {errors.totalAmount && (
          <p className="text-xs text-destructive">
            {errors.totalAmount.message}
          </p>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="totalInstallments">Parcelas</Label>
        <Input
          id="totalInstallments"
          type="number"
          min={1}
          max={18}
          {...register("totalInstallments", { valueAsNumber: true })}
        />
        {errors.totalInstallments && (
          <p className="text-xs text-destructive">
            {errors.totalInstallments.message}
          </p>
        )}
        {installments > 1 && totalAmount > 0 && (
          <p className="text-[11px] text-muted-foreground">
            {installments}x de aproximadamente{" "}
            {new Intl.NumberFormat("pt-BR", {
              style: "currency",
              currency: selectedCard?.currency ?? "BRL",
            }).format(installmentValue)}
          </p>
        )}
      </div>

      {installments > 1 && (
        <div className="flex items-start justify-between gap-3 rounded-lg border bg-muted/30 p-3">
          <div className="space-y-1">
            <Label
              htmlFor="firstInstallmentOnCurrentInvoice"
              className="cursor-pointer text-sm"
            >
              Primeira parcela na fatura corrente
            </Label>
            <p className="text-[11px] text-muted-foreground">
              Desmarque para começar pela próxima fatura.
            </p>
          </div>
          <Switch
            id="firstInstallmentOnCurrentInvoice"
            checked={firstOnCurrent}
            onCheckedChange={(v) =>
              setValue("firstInstallmentOnCurrentInvoice", v)
            }
          />
        </div>
      )}

      <DialogFooter>
        <Button
          type="submit"
          className="w-full"
          disabled={createTx.isPending}
        >
          {createTx.isPending ? "Salvando..." : "Registrar compra"}
        </Button>
      </DialogFooter>
    </form>
  );
}
