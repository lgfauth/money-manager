"use client";

import { useEffect, useRef } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { recurringSchema, type RecurringFormData } from "@/lib/validators";
import { TransactionType } from "@/types/transaction";
import { RecurrenceFrequency } from "@/types/recurring";
import type { RecurringTransactionResponseDto } from "@/types/recurring";
import { useCreateRecurring, useUpdateRecurring } from "@/hooks/use-recurring";
import { useAccounts } from "@/hooks/use-accounts";
import { useCategories } from "@/hooks/use-categories";
import { format } from "date-fns";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
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

interface RecurringFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  editingRecurring?: RecurringTransactionResponseDto | null;
}

const typeOptions = [
  { value: TransactionType.Expense, label: "Despesa" },
  { value: TransactionType.Income, label: "Receita" },
];

const frequencyOptions = [
  { value: RecurrenceFrequency.Daily, label: "Diária" },
  { value: RecurrenceFrequency.Weekly, label: "Semanal" },
  { value: RecurrenceFrequency.Biweekly, label: "Quinzenal" },
  { value: RecurrenceFrequency.Monthly, label: "Mensal" },
  { value: RecurrenceFrequency.Quarterly, label: "Trimestral" },
  { value: RecurrenceFrequency.Semiannual, label: "Semestral" },
  { value: RecurrenceFrequency.Annual, label: "Anual" },
];

export function RecurringForm({
  open,
  onOpenChange,
  editingRecurring,
}: RecurringFormProps) {
  const createRecurring = useCreateRecurring();
  const updateRecurring = useUpdateRecurring();
  const { data: accounts } = useAccounts();
  const { data: categories } = useCategories();

  const isEditing = !!editingRecurring;

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors, submitCount },
  } = useForm<RecurringFormData>({
    resolver: zodResolver(recurringSchema),
    defaultValues: {
      description: "",
      amount: 0,
      type: TransactionType.Expense,
      accountId: "",
      categoryId: "",
      frequency: RecurrenceFrequency.Monthly,
      startDate: format(new Date(), "yyyy-MM-dd"),
      isActive: true,
      notes: "",
    },
  });

  const selectedType = watch("type");
  const amountValue = watch("amount");
  const mutationError = isEditing ? updateRecurring.error : createRecurring.error;
  const resetMutationsRef = useRef(() => {});

  resetMutationsRef.current = () => {
    createRecurring.reset();
    updateRecurring.reset();
  };

  const filteredCategories = categories?.filter((cat) => {
    if (selectedType === TransactionType.Income) return cat.type === "Income";
    return cat.type === "Expense";
  });

  useEffect(() => {
    if (!open) return;

    resetMutationsRef.current();

    if (editingRecurring) {
      reset({
        description: editingRecurring.description,
        amount: editingRecurring.amount,
        type: editingRecurring.type as TransactionType,
        accountId: editingRecurring.accountId,
        categoryId: editingRecurring.categoryId,
        frequency: editingRecurring.frequency,
        startDate: editingRecurring.startDate.split("T")[0],
        endDate: editingRecurring.endDate?.split("T")[0],
        isActive: editingRecurring.isActive,
        notes: editingRecurring.notes ?? "",
      });
    } else {
      reset({
        description: "",
        amount: 0,
        type: TransactionType.Expense,
        accountId: "",
        categoryId: "",
        frequency: RecurrenceFrequency.Monthly,
        startDate: format(new Date(), "yyyy-MM-dd"),
        isActive: true,
        notes: "",
      });
    }
  }, [open, editingRecurring, reset]);

  const onSubmit = (data: RecurringFormData) => {
    if (isEditing) {
      updateRecurring.mutate(
        { id: editingRecurring!.id, data },
        { onSuccess: () => onOpenChange(false) }
      );
    } else {
      createRecurring.mutate(data, {
        onSuccess: () => onOpenChange(false),
      });
    }
  };

  const isPending = createRecurring.isPending || updateRecurring.isPending;

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="overflow-y-auto">
        <SheetHeader>
          <SheetTitle>
            {isEditing ? "Editar Recorrente" : "Nova Recorrente"}
          </SheetTitle>
          <SheetDescription>
            {isEditing
              ? "Altere os dados da transação recorrente."
              : "Configure uma nova transação recorrente."}
          </SheetDescription>
        </SheetHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 px-4">
          <FormErrorSummary
            errors={errors}
            submitCount={submitCount}
            apiError={mutationError}
          />

          {/* Type selector */}
          <div className="space-y-2">
            <Label>Tipo</Label>
            <div className="flex rounded-lg border p-1 gap-1">
              {typeOptions.map((opt) => (
                <button
                  key={opt.value}
                  type="button"
                  onClick={() => setValue("type", opt.value)}
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

          <div className="space-y-2">
            <Label htmlFor="description">Descrição</Label>
            <Input
              id="description"
              placeholder="Ex: Aluguel"
              {...register("description")}
            />
            {errors.description && (
              <p className="text-xs text-destructive">
                {errors.description.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label>Valor</Label>
            <MoneyInput value={amountValue} onChange={(v) => setValue("amount", v)} />
            {errors.amount && (
              <p className="text-xs text-destructive">
                {errors.amount.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label>Frequência</Label>
            <Select
              defaultValue={watch("frequency")}
              onValueChange={(v) =>
                v && setValue("frequency", v as RecurrenceFrequency)
              }
            >
              <SelectTrigger className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {frequencyOptions.map((f) => (
                  <SelectItem key={f.value} value={f.value}>
                    {f.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label>Conta</Label>
            <Select
              value={watch("accountId") || null}
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
              <p className="text-xs text-destructive">
                {errors.accountId.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label>Categoria</Label>
            <Select
              value={watch("categoryId") || null}
              onValueChange={(v) => v && setValue("categoryId", v)}
            >
              <SelectTrigger className="w-full">
                <SelectValue placeholder="Selecione a categoria">
                  {(value: string) => {
                    const cat = filteredCategories?.find((c) => c.id === value);
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
                {filteredCategories?.map((cat) => (
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
            {errors.categoryId && (
              <p className="text-xs text-destructive">
                {errors.categoryId.message}
              </p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label htmlFor="startDate">Início</Label>
              <Input id="startDate" type="date" {...register("startDate")} />
              {errors.startDate && (
                <p className="text-xs text-destructive">
                  {errors.startDate.message}
                </p>
              )}
            </div>
            <div className="space-y-2">
              <Label htmlFor="endDate">Fim (opcional)</Label>
              <Input id="endDate" type="date" {...register("endDate")} />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="notes">Observações</Label>
            <Input
              id="notes"
              placeholder="Opcional"
              {...register("notes")}
            />
          </div>

          <SheetFooter className="px-0">
            <Button type="submit" className="w-full" disabled={isPending}>
              {isPending
                ? "Salvando..."
                : isEditing
                  ? "Salvar Alterações"
                  : "Criar Recorrente"}
            </Button>
          </SheetFooter>
        </form>
      </SheetContent>
    </Sheet>
  );
}
