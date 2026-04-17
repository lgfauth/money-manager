"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { getApiErrorMessage } from "@/lib/api-errors";
import { queryKeys } from "@/lib/query-client";
import { toast } from "sonner";
import type {
  CreateCreditCardRequest,
  CreateCreditCardTransactionRequest,
  CreditCardInvoiceDetailResponseDto,
  CreditCardInvoiceResponseDto,
  CreditCardResponseDto,
  CreditCardTransactionResponseDto,
  PayCreditCardInvoiceRequest,
} from "@/types/credit-card";

export function useCreditCards() {
  return useQuery({
    queryKey: queryKeys.creditCards,
    queryFn: () => apiClient.get<CreditCardResponseDto[]>("/api/credit-cards"),
  });
}

export function useCreditCard(id: string | undefined) {
  return useQuery({
    queryKey: id ? queryKeys.creditCard(id) : ["credit-cards", "none"],
    queryFn: () => apiClient.get<CreditCardResponseDto>(`/api/credit-cards/${id}`),
    enabled: !!id,
  });
}

export function useCreateCreditCard() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateCreditCardRequest) =>
      apiClient.post<CreditCardResponseDto>("/api/credit-cards", data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.creditCards });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Cartão criado com sucesso");
    },
    onError: (error) => toast.error(getApiErrorMessage(error, "Erro ao criar cartão")),
  });
}

export function useUpdateCreditCard() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: CreateCreditCardRequest }) =>
      apiClient.put<CreditCardResponseDto>(`/api/credit-cards/${id}`, data),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.creditCards });
      qc.invalidateQueries({ queryKey: queryKeys.creditCard(variables.id) });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Cartão atualizado");
    },
    onError: (error) => toast.error(getApiErrorMessage(error, "Erro ao atualizar cartão")),
  });
}

export function useDeleteCreditCard() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete<void>(`/api/credit-cards/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.creditCards });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Cartão excluído");
    },
    onError: (error) => toast.error(getApiErrorMessage(error, "Erro ao excluir cartão")),
  });
}

export function useCreditCardInvoices(cardId: string | undefined) {
  return useQuery({
    queryKey: cardId ? queryKeys.creditCardInvoices(cardId) : ["credit-cards", "none", "invoices"],
    queryFn: () =>
      apiClient.get<CreditCardInvoiceResponseDto[]>(
        `/api/credit-cards/${cardId}/invoices`
      ),
    enabled: !!cardId,
  });
}

export function useCreditCardInvoice(cardId: string | undefined, invoiceId: string | undefined) {
  return useQuery({
    queryKey:
      cardId && invoiceId
        ? queryKeys.creditCardInvoice(cardId, invoiceId)
        : ["credit-cards", "none", "invoices", "none"],
    queryFn: () =>
      apiClient.get<CreditCardInvoiceDetailResponseDto>(
        `/api/credit-cards/${cardId}/invoices/${invoiceId}`
      ),
    enabled: !!cardId && !!invoiceId,
  });
}

export function usePayCreditCardInvoice() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      cardId,
      invoiceId,
      data,
    }: {
      cardId: string;
      invoiceId: string;
      data: PayCreditCardInvoiceRequest;
    }) =>
      apiClient.post<CreditCardInvoiceResponseDto>(
        `/api/credit-cards/${cardId}/invoices/${invoiceId}/pay`,
        data
      ),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.creditCards });
      qc.invalidateQueries({ queryKey: queryKeys.creditCard(variables.cardId) });
      qc.invalidateQueries({ queryKey: queryKeys.creditCardInvoices(variables.cardId) });
      qc.invalidateQueries({
        queryKey: queryKeys.creditCardInvoice(variables.cardId, variables.invoiceId),
      });
      qc.invalidateQueries({ queryKey: queryKeys.accounts });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      qc.invalidateQueries({ queryKey: ["transactions"] });
      toast.success("Pagamento registrado");
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao registrar pagamento")),
  });
}

export function useCreditCardTransactions(cardId?: string) {
  return useQuery({
    queryKey: queryKeys.creditCardTransactions(cardId),
    queryFn: () => {
      const url = cardId
        ? `/api/credit-card-transactions?creditCardId=${cardId}`
        : "/api/credit-card-transactions";
      return apiClient.get<CreditCardTransactionResponseDto[]>(url);
    },
  });
}

export function useCreateCreditCardTransaction() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateCreditCardTransactionRequest) =>
      apiClient.post<CreditCardTransactionResponseDto[]>(
        "/api/credit-card-transactions",
        data
      ),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.creditCards });
      qc.invalidateQueries({ queryKey: queryKeys.creditCard(variables.creditCardId) });
      qc.invalidateQueries({ queryKey: queryKeys.creditCardInvoices(variables.creditCardId) });
      qc.invalidateQueries({ queryKey: ["credit-card-transactions"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Compra registrada no cartão");
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao registrar compra")),
  });
}

export function useDeleteCreditCardTransaction() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiClient.delete<void>(`/api/credit-card-transactions/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.creditCards });
      qc.invalidateQueries({ queryKey: ["credit-card-transactions"] });
      qc.invalidateQueries({ queryKey: ["credit-cards"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Compra excluída");
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao excluir compra")),
  });
}
