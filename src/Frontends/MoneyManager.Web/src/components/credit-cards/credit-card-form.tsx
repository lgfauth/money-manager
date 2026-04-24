"use client";

import { useEffect, useRef } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { creditCardSchema, type CreditCardFormData } from "@/lib/validators";
import {
  useCreateCreditCard,
  useUpdateCreditCard,
} from "@/hooks/use-credit-cards";
import { currencies } from "@/config/currencies";
import { DEFAULT_CURRENCY } from "@/config/constants";
import type { CreditCardResponseDto } from "@/types/credit-card";

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
import { FormErrorSummary } from "@/components/shared/form-error-summary";

interface CreditCardFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  editingCard?: CreditCardResponseDto | null;
}

export function CreditCardForm({
  open,
  onOpenChange,
  editingCard,
}: CreditCardFormProps) {
  const createCard = useCreateCreditCard();
  const updateCard = useUpdateCreditCard();

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors, submitCount },
  } = useForm<CreditCardFormData>({
    resolver: zodResolver(creditCardSchema),
    defaultValues: {
      name: "",
      limit: 0,
      closingDay: 1,
      billingDueDay: 10,
      bestPurchaseDay: undefined,
      color: "#8B5CF6",
      currency: DEFAULT_CURRENCY,
    },
  });

  const selectedColor = watch("color");
  const selectedCurrency = watch("currency");
  const limitValue = watch("limit");
  const closingDay = watch("closingDay");
  const billingDueDay = watch("billingDueDay");
  const bestPurchaseDay = watch("bestPurchaseDay");

  const isEditing = !!editingCard;
  const mutationError = isEditing ? updateCard.error : createCard.error;
  const resetMutationsRef = useRef(() => {});

  resetMutationsRef.current = () => {
    createCard.reset();
    updateCard.reset();
  };

  useEffect(() => {
    if (!open) return;

    resetMutationsRef.current();

    if (editingCard) {
      reset({
        name: editingCard.name,
        limit: editingCard.limit,
        closingDay: editingCard.closingDay,
        billingDueDay: editingCard.billingDueDay,
        bestPurchaseDay: editingCard.bestPurchaseDay,
        color: editingCard.color,
        currency: editingCard.currency,
      });
    } else {
      reset({
        name: "",
        limit: 0,
        closingDay: 1,
        billingDueDay: 10,
        bestPurchaseDay: undefined,
        color: "#8B5CF6",
        currency: DEFAULT_CURRENCY,
      });
    }
  }, [open, editingCard, reset]);

  const onSubmit = (data: CreditCardFormData) => {
    if (isEditing) {
      updateCard.mutate(
        { id: editingCard!.id, data },
        { onSuccess: () => onOpenChange(false) }
      );
    } else {
      createCard.mutate(data, {
        onSuccess: () => onOpenChange(false),
      });
    }
  };

  const isPending = createCard.isPending || updateCard.isPending;

  const dayOptions = Array.from({ length: 28 }, (_, i) => i + 1);

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="overflow-y-auto">
        <SheetHeader>
          <SheetTitle>
            {isEditing ? "Editar Cartão" : "Novo Cartão"}
          </SheetTitle>
          <SheetDescription>
            {isEditing
              ? "Altere os dados do cartão de crédito."
              : "Preencha os dados para cadastrar um novo cartão."}
          </SheetDescription>
        </SheetHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 px-4">
          <FormErrorSummary
            errors={errors}
            submitCount={submitCount}
            apiError={mutationError}
          />

          <div className="space-y-2">
            <Label htmlFor="name">Nome</Label>
            <Input
              id="name"
              placeholder="Ex: Nubank Platinum"
              {...register("name")}
            />
            {errors.name && (
              <p className="text-xs text-destructive">{errors.name.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label>Limite</Label>
            <MoneyInput
              value={limitValue}
              onChange={(v) => setValue("limit", v)}
              currencyCode={selectedCurrency}
            />
            {errors.limit && (
              <p className="text-xs text-destructive">{errors.limit.message}</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label>Dia de fechamento</Label>
              <Select
                value={String(closingDay)}
                onValueChange={(v) =>
                  v && setValue("closingDay", Number(v), { shouldValidate: true })
                }
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Selecione" />
                </SelectTrigger>
                <SelectContent>
                  {dayOptions.map((d) => (
                    <SelectItem key={d} value={String(d)}>
                      {d}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.closingDay && (
                <p className="text-xs text-destructive">
                  {errors.closingDay.message}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label>Dia de vencimento</Label>
              <Select
                value={String(billingDueDay)}
                onValueChange={(v) =>
                  v &&
                  setValue("billingDueDay", Number(v), { shouldValidate: true })
                }
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Selecione" />
                </SelectTrigger>
                <SelectContent>
                  {dayOptions.map((d) => (
                    <SelectItem key={d} value={String(d)}>
                      {d}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.billingDueDay && (
                <p className="text-xs text-destructive">
                  {errors.billingDueDay.message}
                </p>
              )}
            </div>
          </div>

          <div className="space-y-2">
            <Label>Melhor dia de compra (opcional)</Label>
            <Select
              value={bestPurchaseDay ? String(bestPurchaseDay) : ""}
              onValueChange={(v) =>
                setValue("bestPurchaseDay", v ? Number(v) : undefined, {
                  shouldValidate: true,
                })
              }
            >
              <SelectTrigger className="w-full">
                <SelectValue
                  placeholder={`Padrão: dia ${closingDay} (fechamento)`}
                />
              </SelectTrigger>
              <SelectContent>
                {dayOptions.map((d) => (
                  <SelectItem key={d} value={String(d)}>
                    {d}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <p className="text-[11px] text-muted-foreground">
              Se não informado, usa o dia de fechamento.
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="currency">Moeda</Label>
            <Select
              value={selectedCurrency}
              onValueChange={(v) => v && setValue("currency", v)}
            >
              <SelectTrigger className="w-full">
                <SelectValue placeholder="Selecione">
                  {(value: string) => {
                    const c = currencies.find((x) => x.code === value);
                    return c ? `${c.symbol} — ${c.name}` : null;
                  }}
                </SelectValue>
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

          <SheetFooter className="px-0">
            <Button type="submit" className="w-full" disabled={isPending}>
              {isPending
                ? "Salvando..."
                : isEditing
                  ? "Salvar Alterações"
                  : "Criar Cartão"}
            </Button>
          </SheetFooter>
        </form>
      </SheetContent>
    </Sheet>
  );
}
