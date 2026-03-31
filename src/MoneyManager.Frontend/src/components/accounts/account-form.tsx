"use client";

import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { accountSchema, type AccountFormData } from "@/lib/validators";
import { AccountType, type AccountResponseDto } from "@/types/account";
import { useCreateAccount, useUpdateAccount } from "@/hooks/use-accounts";
import { currencies } from "@/config/currencies";
import { DEFAULT_CURRENCY } from "@/config/constants";

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
import { ColorPicker } from "@/components/shared/color-picker";

interface AccountFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  editingAccount?: AccountResponseDto | null;
}

const accountTypeLabels: Record<string, string> = {
  Checking: "Conta Corrente",
  Savings: "Poupanca",
  Cash: "Dinheiro",
  CreditCard: "Cartao de Credito",
  Investment: "Investimento",
};

export function AccountForm({
  open,
  onOpenChange,
  editingAccount,
}: AccountFormProps) {
  const createAccount = useCreateAccount();
  const updateAccount = useUpdateAccount();

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors },
  } = useForm<AccountFormData>({
    resolver: zodResolver(accountSchema),
    defaultValues: {
      name: "",
      type: AccountType.Checking,
      initialBalance: 0,
      currency: DEFAULT_CURRENCY,
      color: "#6366f1",
    },
  });

  const selectedType = watch("type");
  const selectedColor = watch("color");
  const selectedCurrency = watch("currency");
  const balanceValue = watch("initialBalance");
  const isCreditCard = selectedType === AccountType.CreditCard;
  const isEditing = !!editingAccount;

  useEffect(() => {
    if (!open) return;
    if (editingAccount) {
      reset({
        name: editingAccount.name,
        type: editingAccount.type,
        initialBalance: editingAccount.balance,
        currency: editingAccount.currency,
        color: editingAccount.color,
        invoiceClosingDay: editingAccount.invoiceClosingDay,
        invoiceDueDayOffset: editingAccount.invoiceDueDayOffset,
        creditLimit: editingAccount.creditLimit,
      });
    } else {
      reset({
        name: "",
        type: AccountType.Checking,
        initialBalance: 0,
        currency: DEFAULT_CURRENCY,
        color: "#6366f1",
      });
    }
  }, [open, editingAccount, reset]);

  const onSubmit = (data: AccountFormData) => {
    if (!isCreditCard) {
      delete data.invoiceClosingDay;
      delete data.invoiceDueDayOffset;
      delete data.creditLimit;
    }

    if (isEditing) {
      updateAccount.mutate(
        { id: editingAccount!.id, data },
        { onSuccess: () => onOpenChange(false) }
      );
    } else {
      createAccount.mutate(data, {
        onSuccess: () => onOpenChange(false),
      });
    }
  };

  const isPending = createAccount.isPending || updateAccount.isPending;

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="overflow-y-auto">
        <SheetHeader>
          <SheetTitle>{isEditing ? "Editar Conta" : "Nova Conta"}</SheetTitle>
          <SheetDescription>
            {isEditing
              ? "Altere os dados da conta."
              : "Preencha os dados para criar uma nova conta."}
          </SheetDescription>
        </SheetHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 px-4">
          <div className="space-y-2">
            <Label htmlFor="name">Nome</Label>
            <Input
              id="name"
              placeholder="Ex: Nubank"
              {...register("name")}
            />
            {errors.name && (
              <p className="text-xs text-destructive">{errors.name.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="type">Tipo</Label>
            <Select
              value={selectedType}
              onValueChange={(v) => v && setValue("type", v as AccountType)}
            >
              <SelectTrigger className="w-full">
                <SelectValue placeholder={accountTypeLabels[selectedType] ?? "Selecione"} />
              </SelectTrigger>
              <SelectContent>
                {Object.values(AccountType).map((t) => (
                  <SelectItem key={t} value={t}>
                    {accountTypeLabels[t]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label>Saldo Inicial</Label>
            <MoneyInput
              value={balanceValue}
              onChange={(v) => setValue("initialBalance", v)}
              currencyCode={selectedCurrency}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="currency">Moeda</Label>
            <Select
              value={selectedCurrency}
              onValueChange={(v) => v && setValue("currency", v)}
            >
              <SelectTrigger className="w-full">
                <SelectValue placeholder="Selecione" />
              </SelectTrigger>
              <SelectContent>
                {currencies.map((c) => (
                  <SelectItem key={c.code} value={c.code}>
                    {c.symbol} — {c.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label>Cor</Label>
            <ColorPicker
              value={selectedColor}
              onChange={(c) => setValue("color", c)}
            />
          </div>

          {isCreditCard && (
            <div className="space-y-4 rounded-lg border p-4">
              <p className="text-sm font-medium">Dados do Cartao de Credito</p>

              <div className="space-y-2">
                <Label htmlFor="creditLimit">Limite</Label>
                <MoneyInput
                  value={watch("creditLimit") ?? 0}
                  onChange={(v) => setValue("creditLimit", v)}
                  currencyCode={selectedCurrency}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="invoiceClosingDay">
                  Dia de fechamento (1–28)
                </Label>
                <Input
                  id="invoiceClosingDay"
                  type="number"
                  min={1}
                  max={28}
                  {...register("invoiceClosingDay", { valueAsNumber: true })}
                />
                {errors.invoiceClosingDay && (
                  <p className="text-xs text-destructive">
                    {errors.invoiceClosingDay.message}
                  </p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="invoiceDueDayOffset">
                  Dias ate vencimento (1–30)
                </Label>
                <Input
                  id="invoiceDueDayOffset"
                  type="number"
                  min={1}
                  max={30}
                  {...register("invoiceDueDayOffset", { valueAsNumber: true })}
                />
                {errors.invoiceDueDayOffset && (
                  <p className="text-xs text-destructive">
                    {errors.invoiceDueDayOffset.message}
                  </p>
                )}
              </div>
            </div>
          )}

          <SheetFooter className="px-0">
            <Button type="submit" className="w-full" disabled={isPending}>
              {isPending
                ? "Salvando..."
                : isEditing
                  ? "Salvar Alteracoes"
                  : "Criar Conta"}
            </Button>
          </SheetFooter>
        </form>
      </SheetContent>
    </Sheet>
  );
}
