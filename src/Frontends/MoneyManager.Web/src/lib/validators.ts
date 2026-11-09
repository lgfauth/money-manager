import { z } from "zod";
import { CategoryType } from "@/types/category";
import { AccountType } from "@/types/account";
import { TransactionType } from "@/types/transaction";
import { RecurrenceFrequency } from "@/types/recurring";

// Espelha a política de senha dos validators do backend
const newPasswordRules = z
  .string()
  .min(12, "A senha deve ter pelo menos 12 caracteres")
  .regex(/[A-Z]/, "A senha deve ter pelo menos uma letra maiúscula")
  .regex(/[0-9]/, "A senha deve ter pelo menos um número")
  .regex(/[^a-zA-Z0-9]/, "A senha deve ter pelo menos um caractere especial");

export const loginSchema = z.object({
  email: z.string().email("E-mail inválido"),
  password: z.string().min(1, "Senha obrigatória"),
});

export const registerSchema = z
  .object({
    name: z.string().min(2, "Nome deve ter pelo menos 2 caracteres"),
    email: z.string().email("E-mail inválido"),
    password: newPasswordRules,
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

function localDateString(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

export function todayString(): string {
  return localDateString(new Date());
}

export function minAllowedDateString(): string {
  const d = new Date();
  d.setDate(d.getDate() - 30);
  return localDateString(d);
}

export const transactionSchema = z.object({
  description: z.string().min(1, "Descrição obrigatória"),
  amount: z.number().positive("Valor deve ser positivo"),
  date: z
    .string()
    .min(1, "Data obrigatória")
    .refine((val) => val <= todayString(), "A data não pode ser futura")
    .refine(
      (val) => val >= minAllowedDateString(),
      "A data não pode ser anterior a 30 dias"
    ),
  type: z.nativeEnum(TransactionType),
  accountId: z.string().min(1, "Conta obrigatória"),
  categoryId: z.string().min(1, "Categoria obrigatória"),
  notes: z.string().optional(),
});

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, "Senha atual obrigatória"),
    newPassword: newPasswordRules,
    confirmPassword: z.string(),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: "As senhas não conferem",
    path: ["confirmPassword"],
  })
  .refine((data) => data.newPassword !== data.currentPassword, {
    message: "A nova senha deve ser diferente da senha atual",
    path: ["newPassword"],
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
  purchaseDate: z
    .string()
    .min(1, "Data obrigatória")
    .refine((val) => val <= todayString(), "A data não pode ser futura")
    .refine(
      (val) => val >= minAllowedDateString(),
      "A data não pode ser anterior a 30 dias"
    ),
  totalAmount: z.number().positive("Valor deve ser maior que zero"),
  totalInstallments: z
    .number()
    .int()
    .min(1, "Mínimo 1 parcela")
    .max(18, "Máximo 18 parcelas"),
  firstInstallmentOnCurrentInvoice: z.boolean(),
  isRefund: z.boolean().optional(),
});

export const payCreditCardInvoiceSchema = z.object({
  paidWithAccountId: z.string().min(1, "Conta pagadora obrigatória"),
  paidAmount: z.number().positive("Valor deve ser maior que zero"),
  paidAt: z.string().min(1, "Data obrigatória"),
});

export const updateCreditCardTransactionSchema = z.object({
  description: z.string().min(1, "Descrição obrigatória"),
  categoryId: z.string().optional(),
  purchaseDate: z
    .string()
    .min(1, "Data obrigatória")
    .refine((val) => val <= todayString(), "A data não pode ser futura")
    .refine(
      (val) => val >= minAllowedDateString(),
      "A data não pode ser anterior a 30 dias"
    ),
  totalAmount: z.number().positive("Valor deve ser maior que zero"),
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

export const bankMcpApiKeySchema = z.object({
  apiKey: z
    .string()
    .trim() // keys coladas costumam vir com espaço/quebra de linha nas pontas
    .min(1, "API key obrigatória")
    .regex(/^sk_live_/, "A API key deve começar com sk_live_"),
});

// Edição de um lançamento na revisão da importação de fatura. Parcelas em texto para aceitar
// campo vazio (compra à vista); limites espelham ConfirmInvoiceImportValidator.
export const invoiceImportItemSchema = z
  .object({
    date: z.string().min(1, "Informe a data da compra"),
    description: z
      .string()
      .trim()
      .min(1, "Informe a descrição")
      .max(200, "A descrição pode ter no máximo 200 caracteres"),
    amount: z.number().positive("O valor deve ser maior que zero"),
    categoryId: z.string().nullable(),
    installmentNumber: z.string().regex(/^\d*$/, "Use apenas números"),
    totalInstallments: z.string().regex(/^\d*$/, "Use apenas números"),
  })
  .superRefine((data, ctx) => {
    const hasNumber = data.installmentNumber !== "";
    const hasTotal = data.totalInstallments !== "";
    if (hasNumber !== hasTotal) {
      ctx.addIssue({
        code: "custom",
        path: [hasNumber ? "totalInstallments" : "installmentNumber"],
        message: "Informe a parcela atual e o total de parcelas juntos",
      });
      return;
    }
    if (!hasNumber) return;
    const number = Number(data.installmentNumber);
    const total = Number(data.totalInstallments);
    if (total < 1 || total > 48) {
      ctx.addIssue({
        code: "custom",
        path: ["totalInstallments"],
        message: "O total de parcelas deve estar entre 1 e 48",
      });
    }
    if (number < 1 || number > total) {
      ctx.addIssue({
        code: "custom",
        path: ["installmentNumber"],
        message: "A parcela atual deve estar entre 1 e o total de parcelas",
      });
    }
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
export type UpdateCreditCardTransactionFormData = z.infer<typeof updateCreditCardTransactionSchema>;
export type BankMcpApiKeyFormData = z.infer<typeof bankMcpApiKeySchema>;
export type InvoiceImportItemFormData = z.infer<typeof invoiceImportItemSchema>;
