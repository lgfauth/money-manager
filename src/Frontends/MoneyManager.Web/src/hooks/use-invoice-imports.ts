"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { apiClient } from "@/lib/api-client";
import { ApiClientError, createApiClientError, getApiErrorMessage } from "@/lib/api-errors";
import { useAuthStore } from "@/stores/auth-store";
import { useInvoiceImportDraftStore } from "@/stores/invoice-import-draft-store";
import { invalidateFinancialSummaries, queryKeys } from "@/lib/query-client";
import {
  INVOICE_IMPORT_DETAIL_POLL_MS,
  INVOICE_IMPORT_LIST_POLL_MS,
  INVOICE_IMPORT_NETWORK_ERROR,
  INVOICE_IMPORT_UPLOAD_ERROR_FALLBACKS,
  isActiveInvoiceImport,
  withInferredContentType,
} from "@/lib/invoice-import";
import type {
  ConfirmInvoiceImportRequest,
  CreditCardInvoiceImportResponseDto,
  InvoiceImportCreatedResponseDto,
  InvoiceImportEligibleCardResponseDto,
} from "@/types/invoice-import";

const BASE_PATH = "/api/credit-card-invoice-imports";
const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "";

/**
 * Importação de fatura por id. Enquanto o job está ativo (na fila, lendo ou cadastrando),
 * consulta a API periodicamente; em estado final para sozinho. A consulta periódica pausa
 * com a aba oculta (padrão do TanStack Query) e volta ao focar a janela.
 */
export function useInvoiceImport(id: string | undefined) {
  return useQuery({
    queryKey: id ? queryKeys.invoiceImport(id) : ["invoice-imports", "none"],
    queryFn: () => apiClient.get<CreditCardInvoiceImportResponseDto>(`${BASE_PATH}/${id}`),
    enabled: !!id,
    staleTime: 0,
    refetchOnWindowFocus: true,
    refetchInterval: (query) =>
      isActiveInvoiceImport(query.state.data?.status) ? INVOICE_IMPORT_DETAIL_POLL_MS : false,
  });
}

/**
 * As importações mais recentes do usuário (até 20, definidas pela API). Alimenta a lista, o
 * indicador do header e os avisos de transição: consulta periodicamente só enquanto houver job
 * ativo; sem nenhum, só ao carregar e ao focar a janela.
 */
export function useRecentInvoiceImports() {
  return useQuery({
    queryKey: queryKeys.invoiceImports,
    queryFn: () => apiClient.get<CreditCardInvoiceImportResponseDto[]>(BASE_PATH),
    staleTime: 0,
    refetchOnWindowFocus: true,
    refetchInterval: (query) =>
      query.state.data?.some((job) => isActiveInvoiceImport(job.status))
        ? INVOICE_IMPORT_LIST_POLL_MS
        : false,
  });
}

/** Cartões do usuário que podem receber importação (os não sincronizados pelo Open Finance). */
export function useInvoiceImportEligibleCards() {
  return useQuery({
    queryKey: queryKeys.invoiceImportEligibleCards,
    queryFn: () =>
      apiClient.get<InvoiceImportEligibleCardResponseDto[]>(`${BASE_PATH}/eligible-cards`),
  });
}

/**
 * Confirma a importação: cadastra os lançamentos escolhidos (já editados). Erros (400 com a
 * lista de problemas, 409 de estado/cartão) ficam com quem chama, que os exibe junto aos itens.
 */
export function useConfirmInvoiceImport() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: ConfirmInvoiceImportRequest }) =>
      apiClient.post<CreditCardInvoiceImportResponseDto>(`${BASE_PATH}/${id}/confirm`, data),
    onSuccess: (result) => {
      qc.setQueryData(queryKeys.invoiceImport(result.id), result);
      useInvoiceImportDraftStore.getState().clearDraft(result.id);
      qc.invalidateQueries({ queryKey: queryKeys.invoiceImports, exact: true });
      // Faturas e transações do cartão mudaram (inclusive parcelas em faturas futuras)
      qc.invalidateQueries({ queryKey: queryKeys.creditCards });
      qc.invalidateQueries({ queryKey: queryKeys.creditCardTransactions() });
      invalidateFinancialSummaries(qc);
      const created = result.result?.createdCount ?? 0;
      toast.success(
        created === 1 ? "1 lançamento importado" : `${created} lançamentos importados`
      );
    },
    onError: (error, { id }) => {
      // 409: confirmada em outra aba, cartão sincronizado ou fechamento divergente —
      // recarrega para a tela refletir o estado atual
      if (error instanceof ApiClientError && error.statusCode === 409) {
        qc.invalidateQueries({ queryKey: queryKeys.invoiceImport(id) });
        // Se o estado mudou, a revisão sai da tela junto com o alerta: o toast explica o motivo
        toast.error(error.message);
      }
    },
  });
}

/** Descarta uma importação que falhou ou aguarda revisão. */
export function useDiscardInvoiceImport() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete<void>(`${BASE_PATH}/${id}`),
    onSuccess: (_data, id) => {
      useInvoiceImportDraftStore.getState().clearDraft(id);
      // A API não devolve importações descartadas (404): marca no cache para a tela mostrar
      // "Descartada" em vez de "não encontrada"
      qc.setQueryData<CreditCardInvoiceImportResponseDto>(queryKeys.invoiceImport(id), (old) =>
        old ? { ...old, status: "Discarded" } : old
      );
      qc.invalidateQueries({ queryKey: queryKeys.invoiceImports, exact: true });
      toast.success("Importação descartada");
    },
    onError: (error, id) => {
      // 409: o estado mudou (ex.: confirmada em outra aba) — recarrega para mostrar o atual
      qc.invalidateQueries({ queryKey: queryKeys.invoiceImport(id) });
      toast.error(getApiErrorMessage(error, "Não foi possível descartar a importação"));
    },
  });
}

/** Erro de rede no envio (sem resposta da API): a tela mantém os arquivos para nova tentativa. */
export class InvoiceImportNetworkError extends Error {
  constructor() {
    super(INVOICE_IMPORT_NETWORK_ERROR);
    this.name = "InvoiceImportNetworkError";
  }
}

function parseXhrBody(xhr: XMLHttpRequest): unknown {
  const text = xhr.responseText;
  if (!text) return null;
  const contentType = xhr.getResponseHeader("content-type") ?? "";
  // Páginas de erro HTML (ex.: 413 do proxy) não trazem mensagem útil
  if (contentType.includes("text/html") || text.trimStart().startsWith("<")) return null;
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

/**
 * Envia os arquivos em multipart (`creditCardId` + `files`). Usa XMLHttpRequest porque `fetch`
 * não informa o progresso do upload, que importa em rede móvel (até ~40 MB).
 */
function uploadInvoiceImport(
  creditCardId: string,
  files: File[],
  onProgress: (percent: number) => void
): Promise<InvoiceImportCreatedResponseDto> {
  return new Promise((resolve, reject) => {
    const formData = new FormData();
    formData.append("creditCardId", creditCardId);
    for (const file of files.map(withInferredContentType))
      formData.append("files", file, file.name);

    const xhr = new XMLHttpRequest();
    xhr.open("POST", `${API_URL}${BASE_PATH}`);
    xhr.withCredentials = true;
    // Mesmo critério do apiClient: Bearer para navegadores que bloqueiam cookie cross-origin
    const token = useAuthStore.getState().token;
    if (token) xhr.setRequestHeader("Authorization", `Bearer ${token}`);

    xhr.upload.onprogress = (event) => {
      if (event.lengthComputable) onProgress(Math.round((event.loaded / event.total) * 100));
    };
    xhr.onerror = () => reject(new InvoiceImportNetworkError());
    xhr.ontimeout = () => reject(new InvoiceImportNetworkError());
    xhr.onload = () => {
      const body = parseXhrBody(xhr);
      if (xhr.status === 401) {
        useAuthStore.getState().logout();
        // Fora de componente (sem router), mesmo tratamento de sessão expirada do apiClient
        // eslint-disable-next-line @next/next/no-location-assign-relative-destination
        window.location.href = "/login";
        reject(createApiClientError(401, "Sessão expirada. Faça login novamente."));
        return;
      }
      if (xhr.status < 200 || xhr.status >= 300) {
        reject(
          createApiClientError(
            xhr.status,
            body,
            INVOICE_IMPORT_UPLOAD_ERROR_FALLBACKS[xhr.status] ??
              "Não foi possível enviar a fatura. Tente novamente em instantes."
          )
        );
        return;
      }
      resolve(body as InvoiceImportCreatedResponseDto);
    };

    xhr.send(formData);
  });
}

/**
 * Cria a importação (o Worker processa em segundo plano). Expõe o progresso do envio em
 * porcentagem; os erros ficam com quem chama, que os mostra mantendo os arquivos escolhidos.
 */
export function useCreateInvoiceImport() {
  const qc = useQueryClient();
  const [progress, setProgress] = useState(0);

  const mutation = useMutation({
    mutationFn: ({ creditCardId, files }: { creditCardId: string; files: File[] }) => {
      setProgress(0);
      return uploadInvoiceImport(creditCardId, files, setProgress);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.invoiceImports, exact: true });
    },
    onError: (error) => {
      // 404 (cartão removido) ou 409 (sincronizado/importação ativa): atualiza as listas
      if (error instanceof ApiClientError && (error.statusCode === 404 || error.statusCode === 409)) {
        qc.invalidateQueries({ queryKey: queryKeys.invoiceImportEligibleCards });
        qc.invalidateQueries({ queryKey: queryKeys.invoiceImports, exact: true });
      }
    },
  });

  return { ...mutation, progress };
}
