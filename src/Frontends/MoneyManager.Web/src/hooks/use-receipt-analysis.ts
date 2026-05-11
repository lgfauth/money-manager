"use client";

import { useRef, useState } from "react";
import { useAuthStore } from "@/stores/auth-store";
import { getApiErrorMessage } from "@/lib/api-errors";
import type { ReceiptAnalysisResult } from "@/types/receipt";

type ReceiptAnalysisStatus = "idle" | "analyzing" | "success" | "error";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "";

export function useReceiptAnalysis() {
  const [status, setStatus] = useState<ReceiptAnalysisStatus>("idle");
  const [result, setResult] = useState<ReceiptAnalysisResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  const analyze = async (file: File): Promise<void> => {
    // Cancelar requisição anterior se ainda estiver em andamento
    abortControllerRef.current?.abort();
    const abortController = new AbortController();
    abortControllerRef.current = abortController;

    setStatus("analyzing");
    setResult(null);
    setError(null);

    try {
      const formData = new FormData();
      formData.append("file", file);

      const token = useAuthStore.getState().token;

      const response = await fetch(`${API_URL}/api/receipts/analyze`, {
        method: "POST",
        headers: {
          ...(token && { Authorization: `Bearer ${token}` }),
        },
        body: formData,
        signal: abortController.signal,
      });

      if (response.status === 401) {
        useAuthStore.getState().logout();
        if (typeof window !== "undefined") window.location.href = "/login";
        throw new Error("Sessão expirada.");
      }

      if (!response.ok) {
        const body = await response.text().catch(() => "");
        let message = "Não foi possível ler o comprovante. Tente uma foto mais nítida.";
        try {
          const parsed = JSON.parse(body);
          if (parsed?.message) message = parsed.message;
        } catch {
          // manter mensagem padrão
        }
        throw new Error(message);
      }

      const data: ReceiptAnalysisResult = await response.json();
      setResult(data);
      setStatus("success");
    } catch (err) {
      if ((err as Error)?.name === "AbortError") return;
      const message =
        err instanceof Error
          ? err.message
          : "Não foi possível ler o comprovante. Tente uma foto mais nítida.";
      setError(message);
      setStatus("error");
    }
  };

  const reset = () => {
    abortControllerRef.current?.abort();
    setStatus("idle");
    setResult(null);
    setError(null);
  };

  return { status, result, error, analyze, reset };
}
