"use client";

import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { MoneyInput } from "@/components/shared/money-input";
import { FormErrorSummary } from "@/components/shared/form-error-summary";
import { cn } from "@/lib/utils";
import { useCategories } from "@/hooks/use-categories";
import { useAccounts } from "@/hooks/use-accounts";
import { useCreditCards } from "@/hooks/use-credit-cards";
import { useCreateTransaction } from "@/hooks/use-transactions";
import { useCreateCreditCardTransaction } from "@/hooks/use-credit-cards";
import { TransactionType } from "@/types/transaction";
import type { ReceiptAnalysisResult } from "@/types/receipt";

const CREDIT_KEYWORDS = ["crÃ©dito", "credito", "credit", "cartÃ£o", "cartao"];

function isCreditPayment(paymentMethod: string | null): boolean {
  if (!paymentMethod) return false;
  const lower = paymentMethod.toLowerCase();
  return CREDIT_KEYWORDS.some((kw) => lower.includes(kw));
}

const schema = z.object({
  description: z.string().min(1, "DescriÃ§Ã£o obrigatÃ³ria"),
  amount: z.number().positive("Valor deve ser positivo"),
  date: z.string().min(1, "Data obrigatÃ³ria"),
  transactionType: z.enum(["expense", "income"]),
  categoryId: z.string().optional(),
  accountId: z.string().optional(),
  creditCardId: z.string().optional(),
  installments: z.number().int().min(1).max(48).optional(),
  notes: z.string().optional(),
});

type FormData = z.infer<typeof schema>;

interface ReceiptConfirmationModalProps {
  open: boolean;
  result: ReceiptAnalysisResult | null;
  onClose: () => void;
}

const typeOptions = [
  { value: "expense", label: "Despesa" },
  { value: "income", label: "Receita" },
] as const;

const paymentModeOptions = [
  { value: "account", label: "Conta" },
  { value: "card", label: "CartÃ£o de crÃ©dito" },
] as const;

export function ReceiptConfirmationModal({
  open,
  result,
  onClose,
}: ReceiptConfirmationModalProps) {
  const { data: categories } = useCategories();
  const { data: accounts } = useAccounts();
  const { data: creditCards } = useCreditCards();

  const createTransaction = useCreateTransaction();
  const createCreditCardTransaction = useCreateCreditCardTransaction();

  const [useCard, setUseCard] = useState(false);

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors, submitCount },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      description: "",
      amount: 0,
      date: new Date().toISOString().slice(0, 10),
      transactionType: "expense",
      categoryId: undefined,
      accountId: undefined,
      creditCardId: undefined,
      installments: 1,
      notes: undefined,
    },
  });

  const selectedType = watch("transactionType");
  const amountValue = watch("amount");
  const categoryId = watch("categoryId");
  const accountId = watch("accountId");
  const creditCardId = watch("creditCardId");

  const filteredCategories = categories?.filter((cat) => {
    if (selectedType === "income") return cat.type === "Income";
    return cat.type === "Expense";
  });

  // Preencher o formulÃ¡rio com os dados extraÃ­dos do comprovante
  useEffect(() => {
    if (!result) return;

    const matchCategory = categories?.find((c) =>
      c.name.toLowerCase().includes((result.categoryHint ?? "").toLowerCase())
    );

    setUseCard(isCreditPayment(result.paymentMethod));

    reset({
      description: result.description,
      amount: result.amount,
      date: result.date,
      transactionType: result.transactionType,
      categoryId: matchCategory?.id ?? undefined,
      accountId: undefined,
      creditCardId: undefined,
      installments: result.installments ?? 1,
      notes: result.notes ?? undefined,
    });
  }, [result, categories, reset]);

  const isSubmitting =
    createTransaction.isPending || createCreditCardTransaction.isPending;

  const mutationError =
    createTransaction.error ?? createCreditCardTransaction.error;

  const onSubmit = async (values: FormData) => {
    try {
      if (useCard) {
        if (!values.creditCardId) {
          return;
        }
        await createCreditCardTransaction.mutateAsync({
          creditCardId: values.creditCardId,
          description: values.description,
          categoryId: values.categoryId || undefined,
          purchaseDate: values.date,
          totalAmount: values.amount,
          totalInstallments: values.installments ?? 1,
          firstInstallmentOnCurrentInvoice: true,
          clientRequestId: crypto.randomUUID(),
        });
      } else {
        if (!values.accountId) {
          return;
        }
        await createTransaction.mutateAsync({
          description: values.description,
          amount: values.amount,
          date: values.date,
          type:
            values.transactionType === "income"
              ? TransactionType.Income
              : TransactionType.Expense,
          accountId: values.accountId,
          categoryId: values.categoryId ?? "",
          notes: values.notes,
          clientRequestId: crypto.randomUUID(),
        });
      }
      onClose();
    } catch {
      // erros tratados pelo hook via toast
    }
  };

  const lowConfidence = (result?.confidence ?? 1) < 0.7;

  return (
    <Sheet open={open} onOpenChange={(v) => !v && onClose()}>
      <SheetContent side="bottom" className="max-h-[90dvh] overflow-y-auto rounded-t-xl p-6">
        <SheetHeader className="mb-4">
          <SheetTitle>Confirmar comprovante</SheetTitle>
        </SheetHeader>

        {lowConfidence && (
          <div className="mb-4 flex items-start gap-2 rounded-md border border-yellow-300 bg-yellow-50 p-3 text-sm text-yellow-800">
            <span>
              A leitura do comprovante pode ter sido imprecisa. Verifique os
              dados antes de confirmar.
            </span>
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <FormErrorSummary
            errors={errors}
            submitCount={submitCount}
            apiError={mutationError}
          />

          {/* Tipo */}
          <div className="space-y-2">
            <Label>Tipo</Label>
            <div className="flex rounded-lg border p-1 gap-1">
              {typeOptions.map((opt) => (
                <button
                  key={opt.value}
                  type="button"
                  onClick={() => setValue("transactionType", opt.value)}
                  className={cn(
                    "flex-1 rounded-md px-3 py-1.5 text-sm font-medium transition-colors",
                    selectedType === opt.value
                      ? "bg-primary text-primary-foreground"
                      : "hover:bg-muted"
                  )}
                >
                  {opt.label}
                </button>
              ))}
            </div>
          </div>

          {/* DescriÃ§Ã£o */}
          <div className="space-y-2">
            <Label htmlFor="description">DescriÃ§Ã£o</Label>
            <Input
              id="description"
              placeholder="Ex: Supermercado"
              {...register("description")}
            />
            {errors.description && (
              <p className="text-xs text-destructive">{errors.description.message}</p>
            )}
          </div>

          {/* Valor + Data */}
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label>Valor</Label>
              <MoneyInput value={amountValue} onChange={(v) => setValue("amount", v)} />
              {errors.amount && (
                <p className="text-xs text-destructive">{errors.amount.message}</p>
              )}
            </div>
            <div className="space-y-2">
              <Label htmlFor="date">Data</Label>
              <Input id="date" type="date" {...register("date")} />
              {errors.date && (
                <p className="text-xs text-destructive">{errors.date.message}</p>
              )}
            </div>
          </div>

          {/* Categoria */}
          <div className="space-y-2">
            <Label>Categoria</Label>
            <Select
              value={categoryId || null}
              onValueChange={(v) => setValue("categoryId", v || undefined)}
            >
              <SelectTrigger className="w-full">
                <SelectValue placeholder="Selecione a categoria">
                  {(value: string) => {
                    const cat = filteredCategories?.find((c) => c.id === value);
                    if (!cat) return null;
                    return (
                      <span className="flex items-center gap-2">
                        <span
                          className="h-2 w-2 rounded-full shrink-0"
                          style={{ backgroundColor: cat.color }}
                        />
                        {cat.name}
                      </span>
                    );
                  }}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                {filteredCategories?.map((cat) => (
                  <SelectItem key={cat.id} value={cat.id}>
                    <span className="flex items-center gap-2">
                      <span
                        className="h-2 w-2 rounded-full shrink-0"
                        style={{ backgroundColor: cat.color }}
                      />
                      {cat.name}
                    </span>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {errors.categoryId && (
              <p className="text-xs text-destructive">{errors.categoryId.message}</p>
            )}
          </div>

          {/* Modo de pagamento */}
          <div className="space-y-2">
            <Label>Pagar com</Label>
            <div className="flex rounded-lg border p-1 gap-1">
              {paymentModeOptions.map((opt) => (
                <button
                  key={opt.value}
                  type="button"
                  onClick={() => setUseCard(opt.value === "card")}
                  className={cn(
                    "flex-1 rounded-md px-3 py-1.5 text-sm font-medium transition-colors",
                    (opt.value === "card") === useCard
                      ? "bg-primary text-primary-foreground"
                      : "hover:bg-muted"
                  )}
                >
                  {opt.label}
                </button>
              ))}
            </div>
          </div>

          {/* Conta bancÃ¡ria */}
          {!useCard && (
            <div className="space-y-2">
              <Label>Conta</Label>
              <Select
                value={accountId || null}
                onValueChange={(v) => v && setValue("accountId", v)}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Selecione a conta">
                    {(value: string) => accounts?.find((a) => a.id === value)?.name}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  {accounts?.map((acc) => (
                    <SelectItem key={acc.id} value={acc.id}>
                      {acc.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.accountId && (
                <p className="text-xs text-destructive">{errors.accountId.message}</p>
              )}
            </div>
          )}

          {/* CartÃ£o de crÃ©dito */}
          {useCard && (
            <>
              <div className="space-y-2">
                <Label>CartÃ£o</Label>
                <Select
                  value={creditCardId || null}
                  onValueChange={(v) => v && setValue("creditCardId", v)}
                >
                  <SelectTrigger className="w-full">
                    <SelectValue placeholder="Selecione o cartÃ£o">
                      {(value: string) => creditCards?.find((c) => c.id === value)?.name}
                    </SelectValue>
                  </SelectTrigger>
                  <SelectContent>
                    {creditCards?.map((c) => (
                      <SelectItem key={c.id} value={c.id}>
                        {c.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {errors.creditCardId && (
                  <p className="text-xs text-destructive">{errors.creditCardId.message}</p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="installments">Parcelas</Label>
                <Input
                  id="installments"
                  type="number"
                  min="1"
                  max="48"
                  {...register("installments", { valueAsNumber: true })}
                />
                {errors.installments && (
                  <p className="text-xs text-destructive">{errors.installments.message}</p>
                )}
              </div>
            </>
          )}

          {/* ObservaÃ§Ãµes */}
          <div className="space-y-2">
            <Label htmlFor="notes">ObservaÃ§Ãµes</Label>
            <Input id="notes" placeholder="Opcional" {...register("notes")} />
          </div>

          {/* AÃ§Ãµes */}
          <div className="flex gap-3 pt-2">
            <Button
              type="button"
              variant="outline"
              className="flex-1"
              onClick={onClose}
              disabled={isSubmitting}
            >
              Cancelar
            </Button>
            <Button type="submit" className="flex-1" disabled={isSubmitting}>
              {isSubmitting ? "Salvando..." : "Confirmar"}
            </Button>
          </div>
        </form>
      </SheetContent>
    </Sheet>
  );
}
