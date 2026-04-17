import { z } from "zod";
import { CategoryType } from "@/types/category";
import { AccountType } from "@/types/account";
import { TransactionType } from "@/types/transaction";
import { RecurrenceFrequency } from "@/types/recurring";

export const loginSchema = z.object({
  email: z.string().email("E-mail inválido"),
  password: z.string().min(1, "Senha obrigatória"),
});

export const registerSchema = z
  .object({
    name: z.string().min(2, "Nome deve ter pelo menos 2 caracteres"),
    email: z.string().email("E-mail inválido"),
    password: z.string().min(6, "Senha deve ter pelo menos 6 caracteres"),
    confirmPassword: z.string(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "As senhas não conferem",
    path: ["confirmPassword"],
  });

export const categorySchema = z.object({
  name: z.string().min(1, "Nome obrigatório"),
  type: z.nativeEnum(CategoryType),
  color: z.string().regex(/^#[0-9a-fA-F]{6}$/, "Cor inválida"),
});

export const accountSchema = z.object({
  name: z.string().min(1, "Nome obrigatório"),
  type: z.nativeEnum(AccountType),
  initialBalance: z.number(),
  currency: z.string().min(1),
  color: z.string().regex(/^#[0-9a-fA-F]{6}$/),
});

export const transactionSchema = z.object({
  description: z.string().min(1, "Descrição obrigatória"),
  amount: z.number().positive("Valor deve ser positivo"),
  date: z.string().min(1, "Data obrigatória"),
  type: z.nativeEnum(TransactionType),
  accountId: z.string().min(1, "Conta obrigatória"),
  categoryId: z.string().min(1, "Categoria obrigatória"),
  notes: z.string().optional(),
});

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, "Senha atual obrigatória"),
    newPassword: z.string().min(6, "Nova senha deve ter pelo menos 6 caracteres"),
    confirmNewPassword: z.string(),
  })
  .refine((data) => data.newPassword === data.confirmNewPassword, {
    message: "As senhas não conferem",
    path: ["confirmNewPassword"],
  });

export const updateEmailSchema = z.object({
  newEmail: z.string().email("E-mail inválido"),
  password: z.string().min(1, "Senha obrigatória"),
});

export const creditCardSchema = z.object({
  name: z.string().min(2, "Nome deve ter ao menos 2 caracteres"),
  limit: z.number().positive("Limite deve ser maior que zero"),
  closingDay: z
    .number()
    .int()
    .min(1, "Dia de fechamento entre 1 e 28")
    .max(28, "Dia de fechamento entre 1 e 28"),
  billingDueDay: z
    .number()
    .int()
    .min(1, "Dia de vencimento entre 1 e 28")
    .max(28, "Dia de vencimento entre 1 e 28"),
  bestPurchaseDay: z
    .number()
    .int()
    .min(1, "Melhor dia entre 1 e 28")
    .max(28, "Melhor dia entre 1 e 28")
    .optional(),
  color: z.string().regex(/^#[0-9a-fA-F]{6}$/, "Cor inválida"),
  currency: z.string().length(3),
});

export const creditCardTransactionSchema = z.object({
  creditCardId: z.string().min(1, "Cartão obrigatório"),
  description: z.string().min(1, "Descrição obrigatória"),
  categoryId: z.string().optional(),
  purchaseDate: z.string().min(1, "Data obrigatória"),
  totalAmount: z.number().positive("Valor deve ser maior que zero"),
  totalInstallments: z
    .number()
    .int()
    .min(1, "Mínimo 1 parcela")
    .max(18, "Máximo 18 parcelas"),
  firstInstallmentOnCurrentInvoice: z.boolean(),
});

export const payCreditCardInvoiceSchema = z.object({
  paidWithAccountId: z.string().min(1, "Conta pagadora obrigatória"),
  paidAmount: z.number().positive("Valor deve ser maior que zero"),
  paidAt: z.string().min(1, "Data obrigatória"),
});

export const recurringSchema = z.object({
  description: z.string().min(1, "Descrição obrigatória"),
  amount: z.number().positive("Valor deve ser positivo"),
  type: z.nativeEnum(TransactionType),
  accountId: z.string().min(1, "Conta obrigatória"),
  categoryId: z.string().min(1, "Categoria obrigatória"),
  frequency: z.nativeEnum(RecurrenceFrequency),
  startDate: z.string().min(1, "Data de início obrigatória"),
  endDate: z.string().optional(),
  isActive: z.boolean(),
  notes: z.string().optional(),
});

export type LoginFormData = z.infer<typeof loginSchema>;
export type RegisterFormData = z.infer<typeof registerSchema>;
export type CategoryFormData = z.infer<typeof categorySchema>;
export type AccountFormData = z.infer<typeof accountSchema>;
export type TransactionFormData = z.infer<typeof transactionSchema>;
export type ChangePasswordFormData = z.infer<typeof changePasswordSchema>;
export type UpdateEmailFormData = z.infer<typeof updateEmailSchema>;
export type RecurringFormData = z.infer<typeof recurringSchema>;
export type CreditCardFormData = z.infer<typeof creditCardSchema>;
export type CreditCardTransactionFormData = z.infer<typeof creditCardTransactionSchema>;
export type PayCreditCardInvoiceFormData = z.infer<typeof payCreditCardInvoiceSchema>;
