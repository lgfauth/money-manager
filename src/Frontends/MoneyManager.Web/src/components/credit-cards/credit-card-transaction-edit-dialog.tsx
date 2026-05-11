"use client";

import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { format } from "date-fns";

import {
  updateCreditCardTransactionSchema,
  type UpdateCreditCardTransactionFormData,
} from "@/lib/validators";
import { useUpdateCreditCardTransaction } from "@/hooks/use-credit-cards";
import { useCategories } from "@/hooks/use-categories";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
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
import type { CreditCardTransactionResponseDto } from "@/types/credit-card";

interface CreditCardTransactionEditDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  transaction: CreditCardTransactionResponseDto | null;
  currency?: string;
}

export function CreditCardTransactionEditDialog({
  open,
  onOpenChange,
  transaction,
  currency = "BRL",
}: CreditCardTransactionEditDialogProps) {
  const updateTx = useUpdateCreditCardTransaction();
  const { data: categories } = useCategories();

  const expenseCategories = categories?.filter((c) => c.type === "Expense");

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors, submitCount },
  } = useForm<UpdateCreditCardTransactionFormData>({
    resolver: zodResolver(updateCreditCardTransactionSchema),
    defaultValues: {
      description: "",
      categoryId: "",
      purchaseDate: format(new Date(), "yyyy-MM-dd"),
      totalAmount: 0,
    },
  });

  const categoryId = watch("categoryId");
  const totalAmount = watch("totalAmount");

  useEffect(() => {
    if (!open || !transaction) return;
    updateTx.reset();
    reset({
      description: transaction.description,
      categoryId: transaction.categoryId ?? "",
      purchaseDate: format(new Date(transaction.purchaseDate), "yyyy-MM-dd"),
      totalAmount: transaction.totalAmount,
    });
  }, [open, transaction, reset]);

  const onSubmit = (data: UpdateCreditCardTransactionFormData) => {
    if (!transaction) return;
    updateTx.mutate(
      {
        id: transaction.id,
        data: {
          ...data,
          categoryId: data.categoryId || undefined,
        },
      },
      { onSuccess: () => onOpenChange(false) }
    );
  };

  const isInstallment = (transaction?.totalInstallments ?? 1) > 1;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Editar transação</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <FormErrorSummary
            errors={errors}
            submitCount={submitCount}
            apiError={updateTx.error}
          />

          {isInstallment && (
            <p className="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-[12px] text-amber-700 dark:border-amber-800 dark:bg-amber-950 dark:text-amber-300">
              Esta compra possui {transaction?.totalInstallments} parcelas. Todas serão atualizadas.
            </p>
          )}

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
              currencyCode={currency}
            />
            {errors.totalAmount && (
              <p className="text-xs text-destructive">
                {errors.totalAmount.message}
              </p>
            )}
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
            >
              Cancelar
            </Button>
            <Button type="submit" disabled={updateTx.isPending}>
              {updateTx.isPending ? "Salvando..." : "Salvar alterações"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
