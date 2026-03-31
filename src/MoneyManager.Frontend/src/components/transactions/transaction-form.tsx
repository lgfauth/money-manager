"use client";

import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { transactionSchema, type TransactionFormData } from "@/lib/validators";
import {
  TransactionType,
  type TransactionResponseDto,
} from "@/types/transaction";
import { AccountType } from "@/types/account";
import {
  useCreateTransaction,
  useUpdateTransaction,
  useCreateInstallment,
} from "@/hooks/use-transactions";
import { useAccounts } from "@/hooks/use-accounts";
import { useCategories } from "@/hooks/use-categories";
import { format } from "date-fns";

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
import { cn } from "@/lib/utils";

interface TransactionFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  editingTransaction?: TransactionResponseDto | null;
  defaultType?: TransactionType;
}

const typeOptions = [
  { value: TransactionType.Expense, label: "Despesa", color: "text-expense" },
  { value: TransactionType.Income, label: "Receita", color: "text-income" },
  {
    value: TransactionType.Investment,
    label: "Investimento",
    color: "text-investment",
  },
];

export function TransactionForm({
  open,
  onOpenChange,
  editingTransaction,
  defaultType = TransactionType.Expense,
}: TransactionFormProps) {
  const createTransaction = useCreateTransaction();
  const updateTransaction = useUpdateTransaction();
  const createInstallment = useCreateInstallment();
  const { data: accounts } = useAccounts();
  const { data: categories } = useCategories();

  const [isInstallment, setIsInstallment] = useState(false);
  const [installmentCount, setInstallmentCount] = useState(2);
  const [firstInCurrentInvoice, setFirstInCurrentInvoice] = useState(true);

  const isEditing = !!editingTransaction;
  const saveAndAddRef = useRef(false);

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors },
  } = useForm<TransactionFormData>({
    resolver: zodResolver(transactionSchema),
    defaultValues: {
      description: "",
      amount: 0,
      date: format(new Date(), "yyyy-MM-dd"),
      type: defaultType,
      accountId: "",
      categoryId: "",
      notes: "",
    },
  });

  const selectedType = watch("type");
  const selectedAccountId = watch("accountId");
  const amountValue = watch("amount");

  const selectedAccount = accounts?.find((a) => a.id === selectedAccountId);
  const isCreditCardAccount = selectedAccount?.type === AccountType.CreditCard;

  const filteredCategories = categories?.filter((cat) => {
    if (selectedType === TransactionType.Income) return cat.type === "Income";
    return cat.type === "Expense";
  });

  useEffect(() => {
    if (editingTransaction) {
      reset({
        description: editingTransaction.description,
        amount: editingTransaction.amount,
        date: editingTransaction.date.split("T")[0],
        type: editingTransaction.type,
        accountId: editingTransaction.accountId,
        categoryId: editingTransaction.categoryId,
        notes: editingTransaction.notes ?? "",
      });
      setIsInstallment(false);
    } else {
      reset({
        description: "",
        amount: 0,
        date: format(new Date(), "yyyy-MM-dd"),
        type: defaultType,
        accountId: "",
        categoryId: "",
        notes: "",
      });
      setIsInstallment(false);
      setInstallmentCount(2);
    }
  }, [editingTransaction, defaultType, reset]);

  const onSubmit = (data: TransactionFormData) => {
    const keepOpen = saveAndAddRef.current;
    saveAndAddRef.current = false;

    const onSuccess = () => {
      if (keepOpen) {
        reset({
          description: "",
          amount: 0,
          date: data.date,
          type: data.type,
          accountId: data.accountId,
          categoryId: data.categoryId,
          notes: "",
        });
      } else {
        onOpenChange(false);
      }
    };

    if (isInstallment && isCreditCardAccount && !isEditing) {
      createInstallment.mutate(
        {
          description: data.description,
          totalAmount: data.amount,
          installmentCount,
          firstInstallmentInCurrentInvoice: firstInCurrentInvoice,
          date: data.date,
          type: data.type,
          accountId: data.accountId,
          categoryId: data.categoryId,
          notes: data.notes,
          clientRequestId: crypto.randomUUID(),
        },
        { onSuccess }
      );
    } else if (isEditing) {
      updateTransaction.mutate(
        { id: editingTransaction!.id, data },
        { onSuccess }
      );
    } else {
      createTransaction.mutate(
        { ...data, clientRequestId: crypto.randomUUID() },
        { onSuccess }
      );
    }
  };

  const isPending =
    createTransaction.isPending ||
    updateTransaction.isPending ||
    createInstallment.isPending;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-md">
        <DialogHeader>
          <DialogTitle>
            {isEditing ? "Editar Transacao" : "Nova Transacao"}
          </DialogTitle>
          <DialogDescription>
            {isEditing
              ? "Altere os dados da transacao."
              : "Preencha os dados para registrar."}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          {/* Segmented type selector */}
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
            <Label htmlFor="description">Descricao</Label>
            <Input
              id="description"
              placeholder="Ex: Supermercado"
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
            <Label htmlFor="date">Data</Label>
            <Input id="date" type="date" {...register("date")} />
            {errors.date && (
              <p className="text-xs text-destructive">{errors.date.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label>Conta</Label>
            <Select
              value={selectedAccountId || null}
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
                    const cat = categories?.find((c) => c.id === value);
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

          <div className="space-y-2">
            <Label htmlFor="notes">Observacoes</Label>
            <Input
              id="notes"
              placeholder="Opcional"
              {...register("notes")}
            />
          </div>

          {/* Installment section — only for CC accounts on new transactions */}
          {isCreditCardAccount && !isEditing && (
            <div className="space-y-3 rounded-lg border p-4">
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="installment"
                  checked={isInstallment}
                  onChange={(e) => setIsInstallment(e.target.checked)}
                  className="h-4 w-4 rounded border-input"
                />
                <Label htmlFor="installment" className="text-sm">
                  Parcelamento
                </Label>
              </div>

              {isInstallment && (
                <>
                  <div className="space-y-2">
                    <Label htmlFor="installmentCount">
                      Numero de parcelas
                    </Label>
                    <Input
                      id="installmentCount"
                      type="number"
                      min={2}
                      max={48}
                      value={installmentCount}
                      onChange={(e) =>
                        setInstallmentCount(Number(e.target.value))
                      }
                    />
                    {amountValue > 0 && installmentCount >= 2 && (
                      <p className="text-xs text-muted-foreground">
                        {installmentCount}x de{" "}
                        {new Intl.NumberFormat("pt-BR", {
                          style: "currency",
                          currency: "BRL",
                        }).format(amountValue / installmentCount)}
                      </p>
                    )}
                  </div>

                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      id="firstInCurrentInvoice"
                      checked={firstInCurrentInvoice}
                      onChange={(e) =>
                        setFirstInCurrentInvoice(e.target.checked)
                      }
                      className="h-4 w-4 rounded border-input"
                    />
                    <Label htmlFor="firstInCurrentInvoice" className="text-sm">
                      1a parcela na fatura atual
                    </Label>
                  </div>
                </>
              )}
            </div>
          )}

          <DialogFooter>
            {!isEditing && (
              <Button
                type="button"
                variant="outline"
                className="w-full"
                disabled={isPending}
                onClick={() => {
                  saveAndAddRef.current = true;
                  handleSubmit(onSubmit)();
                }}
              >
                Salvar e Adicionar Outra
              </Button>
            )}
            <Button
              type="submit"
              className="w-full"
              disabled={isPending}
              onClick={() => { saveAndAddRef.current = false; }}
            >
              {isPending
                ? "Salvando..."
                : isEditing
                  ? "Salvar Alteracoes"
                  : isInstallment
                    ? `Parcelar em ${installmentCount}x`
                    : "Criar Transacao"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
