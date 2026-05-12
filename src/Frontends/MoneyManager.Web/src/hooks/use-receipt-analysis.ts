"use client";

import { useRef, useState } from "react";
import { useAuthStore } from "@/stores/auth-store";
import type { ReceiptAnalysisResult } from "@/types/receipt";

type ReceiptAnalysisStatus = "idle" | "analyzing" | "success" | "error";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "";

// Anthropic limita base64 a 5 MB (~3,75 MB antes da codificaÃ§Ã£o).
// Limite conservador: 3,5 MB para o arquivo original.
const MAX_BYTES_BEFORE_ENCODE = 3.5 * 1024 * 1024;
const MAX_DIMENSION = 1920;
const JPEG_QUALITY = 0.82;

async function compressImageIfNeeded(file: File): Promise<File> {
  if (file.size <= MAX_BYTES_BEFORE_ENCODE) return file;

  return new Promise((resolve, reject) => {
    const img = new Image();
    const objectUrl = URL.createObjectURL(file);

    img.onload = () => {
      URL.revokeObjectURL(objectUrl);

      let { width, height } = img;

      // Redimensionar mantendo proporÃ§Ã£o
      if (width > MAX_DIMENSION || height > MAX_DIMENSION) {
        if (width >= height) {
          height = Math.round((height * MAX_DIMENSION) / width);
          width = MAX_DIMENSION;
        } else {
          width = Math.round((width * MAX_DIMENSION) / height);
          height = MAX_DIMENSION;
        }
      }

      const canvas = document.createElement("canvas");
      canvas.width = width;
      canvas.height = height;

      const ctx = canvas.getContext("2d");
      if (!ctx) {
        reject(new Error("Canvas nÃ£o disponÃ­vel"));
        return;
      }

      ctx.drawImage(img, 0, 0, width, height);

      canvas.toBlob(
        (blob) => {
          if (!blob) {
            reject(new Error("Falha ao comprimir imagem"));
            return;
          }
          resolve(new File([blob], file.name.replace(/\.[^.]+$/, ".jpg"), { type: "image/jpeg" }));
        },
        "image/jpeg",
        JPEG_QUALITY,
      );
    };

    img.onerror = () => {
      URL.revokeObjectURL(objectUrl);
      reject(new Error("Falha ao carregar imagem para compressÃ£o"));
    };

    img.src = objectUrl;
  });
}

export function useReceiptAnalysis() {
  const [status, setStatus] = useState<ReceiptAnalysisStatus>("idle");
  const [result, setResult] = useState<ReceiptAnalysisResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  const analyze = async (file: File): Promise<void> => {
    // Cancelar requisiÃ§Ã£o anterior se ainda estiver em andamento
    abortControllerRef.current?.abort();
    const abortController = new AbortController();
    abortControllerRef.current = abortController;

    setStatus("analyzing");
    setResult(null);
    setError(null);

    try {
      const compressed = await compressImageIfNeeded(file);

      const formData = new FormData();
      formData.append("file", compressed);

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
        throw new Error("SessÃ£o expirada.");
      }

      if (!response.ok) {
        const body = await response.text().catch(() => "");
        let message = "NÃ£o foi possÃ­vel ler o comprovante. Tente uma foto mais nÃ­tida.";
        try {
          const parsed = JSON.parse(body);
          if (parsed?.message) message = parsed.message;
        } catch {
          // manter mensagem padrÃ£o
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
          : "NÃ£o foi possÃ­vel ler o comprovante. Tente uma foto mais nÃ­tida.";
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


type ReceiptAnalysisStatus = "idle" | "analyzing" | "success" | "error";

