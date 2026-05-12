"use client";

import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { AlertCircle } from "lucide-react";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useCategories } from "@/hooks/use-categories";
import { useAccounts } from "@/hooks/use-accounts";
import { useCreditCards } from "@/hooks/use-credit-cards";
import { useCreateTransaction } from "@/hooks/use-transactions";
import { useCreateCreditCardTransaction } from "@/hooks/use-credit-cards";
import { TransactionType } from "@/types/transaction";
import type { ReceiptAnalysisResult } from "@/types/receipt";

const CREDIT_KEYWORDS = ["crédito", "credito", "credit", "cartão", "cartao"];

function isCreditPayment(paymentMethod: string | null): boolean {
  if (!paymentMethod) return false;
  const lower = paymentMethod.toLowerCase();
  return CREDIT_KEYWORDS.some((kw) => lower.includes(kw));
}

const schema = z.object({
  description: z.string().min(1, "Descrição obrigatória"),
  amount: z.number().positive("Valor deve ser positivo"),
  date: z.string().min(1, "Data obrigatória"),
  transactionType: z.enum(["expense", "income"]),
  categoryId: z.string().optional(),
  // Para conta bancária
  accountId: z.string().optional(),
  // Para cartão de crédito
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

  const form = useForm<FormData>({
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

  // Preencher o formulário com os dados extraídos do comprovante
  useEffect(() => {
    if (!result) return;

    const matchCategory = categories?.find((c) =>
      c.name.toLowerCase().includes((result.categoryHint ?? "").toLowerCase())
    );

    setUseCard(isCreditPayment(result.paymentMethod));

    form.reset({
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
  }, [result, categories, form]);

  const isSubmitting =
    createTransaction.isPending || createCreditCardTransaction.isPending;

  const handleSubmit = async (values: FormData) => {
    try {
      if (useCard) {
        if (!values.creditCardId) {
          form.setError("creditCardId", { message: "Selecione um cartão" });
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
        });
      } else {
        if (!values.accountId) {
          form.setError("accountId", { message: "Selecione uma conta" });
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
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
            <span>
              A leitura do comprovante pode ter sido imprecisa. Verifique os
              dados antes de confirmar.
            </span>
          </div>
        )}

        <Form {...form}>
          <form
            onSubmit={form.handleSubmit(handleSubmit)}
            className="flex flex-col gap-4"
          >
            {/* Descrição */}
            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Descrição</FormLabel>
                  <FormControl>
                    <Input {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {/* Valor + Data */}
            <div className="grid grid-cols-2 gap-3">
              <FormField
                control={form.control}
                name="amount"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Valor (R$)</FormLabel>
                    <FormControl>
                      <Input type="number" step="0.01" min="0" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="date"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Data</FormLabel>
                    <FormControl>
                      <Input type="date" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            {/* Tipo */}
            <FormField
              control={form.control}
              name="transactionType"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Tipo</FormLabel>
                  <Select value={field.value} onValueChange={field.onChange}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      <SelectItem value="expense">Despesa</SelectItem>
                      <SelectItem value="income">Receita</SelectItem>
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            {/* Categoria */}
            <FormField
              control={form.control}
              name="categoryId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Categoria</FormLabel>
                  <Select
                    value={field.value ?? ""}
                    onValueChange={(v) => field.onChange(v || undefined)}
                  >
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder="Selecionar categoria" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {categories?.map((c) => (
                        <SelectItem key={c.id} value={c.id}>
                          {c.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            {/* Usar cartão ou conta */}
            <div className="flex items-center gap-3">
              <Button
                type="button"
                variant={!useCard ? "default" : "outline"}
                size="sm"
                onClick={() => setUseCard(false)}
              >
                Conta
              </Button>
              <Button
                type="button"
                variant={useCard ? "default" : "outline"}
                size="sm"
                onClick={() => setUseCard(true)}
              >
                Cartão de crédito
              </Button>
            </div>

            {/* Conta bancária */}
            {!useCard && (
              <FormField
                control={form.control}
                name="accountId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Conta</FormLabel>
                    <Select value={field.value ?? ""} onValueChange={field.onChange}>
                      <FormControl>
                        <SelectTrigger>
                          <SelectValue placeholder="Selecionar conta" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {accounts?.map((a) => (
                          <SelectItem key={a.id} value={a.id}>
                            {a.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />
            )}

            {/* Cartão de crédito */}
            {useCard && (
              <>
                <FormField
                  control={form.control}
                  name="creditCardId"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Cartão</FormLabel>
                      <Select value={field.value ?? ""} onValueChange={field.onChange}>
                        <FormControl>
                          <SelectTrigger>
                            <SelectValue placeholder="Selecionar cartão" />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          {creditCards?.map((c) => (
                            <SelectItem key={c.id} value={c.id}>
                              {c.name}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="installments"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Parcelas</FormLabel>
                      <FormControl>
                        <Input type="number" min="1" max="48" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </>
            )}

            {/* Observações */}
            <FormField
              control={form.control}
              name="notes"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Observações</FormLabel>
                  <FormControl>
                    <Input {...field} value={field.value ?? ""} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {/* Ações */}
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
        </Form>
      </SheetContent>
    </Sheet>
  );
}
