"use client";

import { useEffect, useMemo, useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { useCategories } from "@/hooks/use-categories";
import { useCreateBudget, useUpdateBudget } from "@/hooks/use-budgets";
import type { BudgetResponseDto, BudgetItemDto } from "@/types/budget";
import { DEFAULT_BUDGET_AMOUNT } from "@/config/constants";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { MoneyInput } from "@/components/shared/money-input";
import { Check, ChevronLeft, ChevronRight } from "lucide-react";

interface BudgetWizardProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  month: string;
  existingBudget?: BudgetResponseDto | null;
}

export function BudgetWizard({
  open,
  onOpenChange,
  month,
  existingBudget,
}: BudgetWizardProps) {
  const { data: categories } = useCategories();
  const createBudget = useCreateBudget();
  const updateBudget = useUpdateBudget();

  const expenseCategories = useMemo(
    () => categories?.filter((c) => c.type === "Expense") ?? [],
    [categories]
  );

  const [step, setStep] = useState(0);
  const [selectedCategoryIds, setSelectedCategoryIds] = useState<string[]>(
    () =>
      existingBudget?.items.map((i) => i.categoryId) ??
      expenseCategories.map((c) => c.id)
  );
  const [amounts, setAmounts] = useState<Record<string, number>>(() => {
    const map: Record<string, number> = {};
    if (existingBudget) {
      existingBudget.items.forEach(
        (i) => (map[i.categoryId] = i.limitAmount)
      );
    } else {
      expenseCategories.forEach(
        (c) => (map[c.id] = DEFAULT_BUDGET_AMOUNT)
      );
    }
    return map;
  });

  useEffect(() => {
    if (!open) return;
    setStep(0);
    setDirection(0);
    if (existingBudget) {
      setSelectedCategoryIds(existingBudget.items.map((i) => i.categoryId));
      const map: Record<string, number> = {};
      existingBudget.items.forEach((i) => (map[i.categoryId] = i.limitAmount));
      setAmounts(map);
    } else {
      setSelectedCategoryIds(expenseCategories.map((c) => c.id));
      const map: Record<string, number> = {};
      expenseCategories.forEach((c) => (map[c.id] = DEFAULT_BUDGET_AMOUNT));
      setAmounts(map);
    }
  }, [open, existingBudget, expenseCategories]);

  const toggleCategory = (id: string) => {
    setSelectedCategoryIds((prev) =>
      prev.includes(id) ? prev.filter((c) => c !== id) : [...prev, id]
    );
  };

  const handleSave = () => {
    const items: BudgetItemDto[] = selectedCategoryIds.map((categoryId) => ({
      categoryId,
      limitAmount: amounts[categoryId] ?? DEFAULT_BUDGET_AMOUNT,
    }));
    const payload = { month, items };

    if (existingBudget) {
      updateBudget.mutate(
        { id: month, data: payload },
        { onSuccess: () => onOpenChange(false) }
      );
    } else {
      createBudget.mutate(payload, {
        onSuccess: () => onOpenChange(false),
      });
    }
  };

  const isPending = createBudget.isPending || updateBudget.isPending;

  const totalBudget = selectedCategoryIds.reduce(
    (sum, id) => sum + (amounts[id] ?? 0),
    0
  );

  const steps = [
    {
      title: "Selecione as categorias",
      description: "Escolha quais categorias de despesa terao orcamento.",
    },
    {
      title: "Defina os limites",
      description: "Informe o valor maximo para cada categoria.",
    },
    {
      title: "Confirmar",
      description: "Revise o orcamento antes de salvar.",
    },
  ];

  const slideVariants = {
    enter: (direction: number) => ({
      x: direction > 0 ? 100 : -100,
      opacity: 0,
    }),
    center: { x: 0, opacity: 1 },
    exit: (direction: number) => ({
      x: direction > 0 ? -100 : 100,
      opacity: 0,
    }),
  };

  const [direction, setDirection] = useState(0);

  const goNext = () => {
    setDirection(1);
    setStep((s) => Math.min(s + 1, 2));
  };

  const goBack = () => {
    setDirection(-1);
    setStep((s) => Math.max(s - 1, 0));
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{steps[step].title}</DialogTitle>
          <DialogDescription>{steps[step].description}</DialogDescription>
        </DialogHeader>

        {/* Step indicator */}
        <div className="flex items-center gap-2 px-1">
          {steps.map((_, i) => (
            <div
              key={i}
              className={`h-1 flex-1 rounded-full transition-colors ${
                i <= step ? "bg-primary" : "bg-muted"
              }`}
            />
          ))}
        </div>

        <AnimatePresence mode="wait" custom={direction}>
          <motion.div
            key={step}
            custom={direction}
            variants={slideVariants}
            initial="enter"
            animate="center"
            exit="exit"
            transition={{ duration: 0.2 }}
            className="min-h-[200px]"
          >
            {step === 0 && (
              <div className="grid grid-cols-2 gap-2">
                {expenseCategories.map((cat) => {
                  const isSelected = selectedCategoryIds.includes(cat.id);
                  return (
                    <button
                      key={cat.id}
                      type="button"
                      onClick={() => toggleCategory(cat.id)}
                      className={`flex items-center gap-2 rounded-lg border p-3 text-left text-sm transition-colors ${
                        isSelected
                          ? "border-primary bg-primary/5"
                          : "border-border hover:bg-muted"
                      }`}
                    >
                      <div
                        className="h-3 w-3 rounded-full"
                        style={{ backgroundColor: cat.color }}
                      />
                      <span className="flex-1 truncate">{cat.name}</span>
                      {isSelected && (
                        <Check className="h-4 w-4 text-primary" />
                      )}
                    </button>
                  );
                })}
              </div>
            )}

            {step === 1 && (
              <div className="space-y-3">
                {selectedCategoryIds.map((catId) => {
                  const cat = expenseCategories.find((c) => c.id === catId);
                  if (!cat) return null;
                  return (
                    <div
                      key={catId}
                      className="flex items-center gap-3"
                    >
                      <div
                        className="h-3 w-3 rounded-full shrink-0"
                        style={{ backgroundColor: cat.color }}
                      />
                      <span className="text-sm w-28 truncate">{cat.name}</span>
                      <MoneyInput
                        value={amounts[catId] ?? 0}
                        onChange={(v) =>
                          setAmounts((prev) => ({ ...prev, [catId]: v }))
                        }
                        className="flex-1"
                      />
                    </div>
                  );
                })}
              </div>
            )}

            {step === 2 && (
              <div className="space-y-3">
                <div className="rounded-lg border p-4 space-y-2">
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Mes</span>
                    <span className="font-medium">{month}</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Categorias</span>
                    <span className="font-medium">
                      {selectedCategoryIds.length}
                    </span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">
                      Orcamento total
                    </span>
                    <span className="font-bold">
                      {new Intl.NumberFormat("pt-BR", {
                        style: "currency",
                        currency: "BRL",
                      }).format(totalBudget)}
                    </span>
                  </div>
                </div>

                <div className="space-y-1">
                  {selectedCategoryIds.map((catId) => {
                    const cat = expenseCategories.find((c) => c.id === catId);
                    if (!cat) return null;
                    return (
                      <div
                        key={catId}
                        className="flex justify-between text-sm"
                      >
                        <span className="flex items-center gap-2">
                          <span
                            className="h-2 w-2 rounded-full"
                            style={{ backgroundColor: cat.color }}
                          />
                          {cat.name}
                        </span>
                        <span>
                          {new Intl.NumberFormat("pt-BR", {
                            style: "currency",
                            currency: "BRL",
                          }).format(amounts[catId] ?? 0)}
                        </span>
                      </div>
                    );
                  })}
                </div>
              </div>
            )}
          </motion.div>
        </AnimatePresence>

        <DialogFooter className="flex-row justify-between">
          <Button
            variant="outline"
            onClick={goBack}
            disabled={step === 0}
          >
            <ChevronLeft className="mr-1 h-4 w-4" />
            Voltar
          </Button>

          {step < 2 ? (
            <Button onClick={goNext} disabled={selectedCategoryIds.length === 0}>
              Proximo
              <ChevronRight className="ml-1 h-4 w-4" />
            </Button>
          ) : (
            <Button onClick={handleSave} disabled={isPending}>
              {isPending ? "Salvando..." : "Salvar Orcamento"}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
