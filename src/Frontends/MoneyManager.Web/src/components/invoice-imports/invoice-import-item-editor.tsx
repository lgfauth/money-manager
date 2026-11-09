"use client";

import { useEffect, useSyncExternalStore } from "react";
import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { MoneyInput } from "@/components/shared/money-input";
import { useCategories } from "@/hooks/use-categories";
import { invoiceImportItemSchema, type InvoiceImportItemFormData } from "@/lib/validators";
import { itemWarningTexts, type ReviewItemState } from "@/lib/invoice-import-review";
import { CategoryType } from "@/types/category";
import type {
  InvoiceImportItemResponseDto,
  InvoiceImportWarningCode,
} from "@/types/invoice-import";

// No celular a edição abre de baixo para cima; no desktop, pela lateral
const MOBILE_QUERY = "(max-width: 767px)";
function useIsMobile() {
  return useSyncExternalStore(
    (onChange) => {
      const media = window.matchMedia(MOBILE_QUERY);
      media.addEventListener("change", onChange);
      return () => media.removeEventListener("change", onChange);
    },
    () => window.matchMedia(MOBILE_QUERY).matches,
    () => false
  );
}

const NO_CATEGORY = "__none__";

interface InvoiceImportItemEditorProps {
  item: InvoiceImportItemResponseDto | null;
  state: ReviewItemState | null;
  warnings: InvoiceImportWarningCode[];
  onOpenChange: (open: boolean) => void;
  onSave: (itemId: string, changes: Partial<ReviewItemState>) => void;
}

function toFormValues(state: ReviewItemState): InvoiceImportItemFormData {
  return {
    date: state.date,
    description: state.description,
    amount: state.amount,
    categoryId: state.categoryId,
    installmentNumber: state.installmentNumber?.toString() ?? "",
    totalInstallments: state.totalInstallments?.toString() ?? "",
  };
}

/** Edição de um lançamento extraído: data, descrição, valor, parcelas e categoria. */
export function InvoiceImportItemEditor({
  item,
  state,
  warnings,
  onOpenChange,
  onSave,
}: InvoiceImportItemEditorProps) {
  const isMobile = useIsMobile();
  const { data: categories } = useCategories();
  const expenseCategories = categories?.filter((c) => c.type === CategoryType.Expense);

  const {
    register,
    handleSubmit,
    setValue,
    control,
    reset,
    formState: { errors },
  } = useForm<InvoiceImportItemFormData>({
    resolver: zodResolver(invoiceImportItemSchema),
    defaultValues: state ? toFormValues(state) : undefined,
  });

  useEffect(() => {
    if (state) reset(toFormValues(state));
  }, [state, reset]);

  const amount = useWatch({ control, name: "amount" });
  const categoryId = useWatch({ control, name: "categoryId" });
  const isRefund = item?.type === "Refund";

  const submit = (data: InvoiceImportItemFormData) => {
    if (!item) return;
    const total = data.totalInstallments ? Number(data.totalInstallments) : null;
    const hasInstallments = total !== null && total > 1;
    onSave(item.id, {
      date: data.date,
      description: data.description.trim(),
      amount: data.amount,
      categoryId: data.categoryId,
      installmentNumber: hasInstallments ? Number(data.installmentNumber) : null,
      totalInstallments: hasInstallments ? total : null,
    });
    onOpenChange(false);
  };

  return (
    <Sheet open={!!item} onOpenChange={onOpenChange}>
      <SheetContent
        side={isMobile ? "bottom" : "right"}
        className="max-h-[90dvh] overflow-y-auto"
      >
        <SheetHeader>
          <SheetTitle>Editar lançamento</SheetTitle>
          <SheetDescription>
            {isRefund
              ? "Estorno: o valor abate do total da fatura."
              : "As alterações valem só para esta importação."}
          </SheetDescription>
        </SheetHeader>

        {item && (
          <form
            id="invoice-import-item-form"
            onSubmit={handleSubmit(submit)}
            className="space-y-4 px-4"
          >
            {warnings.length > 0 && (
              <ul className="space-y-1 rounded-lg bg-muted/60 p-3 text-xs">
                {warnings.map((code) => (
                  <li key={code}>
                    <span className="font-medium">{itemWarningTexts[code]?.label ?? code}:</span>{" "}
                    {itemWarningTexts[code]?.help}
                  </li>
                ))}
              </ul>
            )}

            <div className="space-y-2">
              <Label htmlFor="item-date">Data da compra</Label>
              <Input id="item-date" type="date" {...register("date")} />
              {errors.date && <p className="text-xs text-destructive">{errors.date.message}</p>}
            </div>

            <div className="space-y-2">
              <Label htmlFor="item-description">Descrição</Label>
              <Input id="item-description" maxLength={200} {...register("description")} />
              {errors.description && (
                <p className="text-xs text-destructive">{errors.description.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="item-amount">
                {isRefund ? "Valor do estorno" : "Valor da parcela"}
              </Label>
              <MoneyInput
                id="item-amount"
                value={amount ?? 0}
                onChange={(v) => setValue("amount", v, { shouldValidate: true })}
              />
              {errors.amount && <p className="text-xs text-destructive">{errors.amount.message}</p>}
            </div>

            {!isRefund && (
              <div className="space-y-2">
                <Label>Parcela</Label>
                <div className="flex items-center gap-2">
                  <Input
                    aria-label="Parcela atual"
                    inputMode="numeric"
                    placeholder="—"
                    className="w-20"
                    {...register("installmentNumber")}
                  />
                  <span className="text-sm text-muted-foreground">de</span>
                  <Input
                    aria-label="Total de parcelas"
                    inputMode="numeric"
                    placeholder="—"
                    className="w-20"
                    {...register("totalInstallments")}
                  />
                </div>
                <p className="text-xs text-muted-foreground">
                  Deixe em branco para compra à vista. Parcelamentos também criam as parcelas
                  seguintes nas próximas faturas.
                </p>
                {(errors.installmentNumber || errors.totalInstallments) && (
                  <p className="text-xs text-destructive">
                    {errors.installmentNumber?.message ?? errors.totalInstallments?.message}
                  </p>
                )}
              </div>
            )}

            <div className="space-y-2">
              <Label>Categoria</Label>
              <Select
                value={categoryId ?? NO_CATEGORY}
                onValueChange={(v) =>
                  setValue("categoryId", !v || v === NO_CATEGORY ? null : (v as string))
                }
              >
                <SelectTrigger className="w-full">
                  <SelectValue>
                    {(value: string) => {
                      const cat = expenseCategories?.find((c) => c.id === value);
                      if (!cat) return "Sem categoria";
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
                  <SelectItem value={NO_CATEGORY}>Sem categoria</SelectItem>
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
              {item.categoryHint && !categoryId && (
                <p className="text-xs text-muted-foreground">
                  Categoria na fatura: {item.categoryHint}
                </p>
              )}
            </div>
          </form>
        )}

        <SheetFooter className="flex-row justify-end gap-2">
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            Cancelar
          </Button>
          <Button type="submit" form="invoice-import-item-form">
            Salvar
          </Button>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  );
}
