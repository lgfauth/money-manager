"use client";

import { useEffect, useRef } from "react";
import { Camera, Loader2 } from "lucide-react";
import { toast } from "sonner";
import { useReceiptAnalysis } from "@/hooks/use-receipt-analysis";
import type { ReceiptAnalysisResult } from "@/types/receipt";

interface ReceiptFabProps {
  onResult: (result: ReceiptAnalysisResult) => void;
}

export function ReceiptFab({ onResult }: ReceiptFabProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const { status, result, error, analyze } = useReceiptAnalysis();

  useEffect(() => {
    if (status === "success" && result) {
      onResult(result);
    }
  }, [status, result, onResult]);

  useEffect(() => {
    if (status === "error" && error) {
      toast.error(error);
    }
  }, [status, error]);

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    // Limpar o input para permitir selecionar o mesmo arquivo novamente
    e.target.value = "";
    await analyze(file);
  };

  return (
    <div className="md:hidden">
      <button
        type="button"
        aria-label="Ler comprovante"
        disabled={status === "analyzing"}
        onClick={() => status !== "analyzing" && inputRef.current?.click()}
        style={{ backgroundColor: "#00C896" }}
        className="
          fixed bottom-40 right-4 z-50
          h-14 w-14 rounded-full shadow-lg
          flex items-center justify-center
          text-white
          active:scale-95 transition-transform
          disabled:opacity-70 disabled:cursor-not-allowed
        "
      >
        {status === "analyzing" ? (
          <Loader2 className="h-6 w-6 animate-spin" />
        ) : (
          <Camera className="h-6 w-6" />
        )}
      </button>

      <input
        ref={inputRef}
        type="file"
        className="hidden"
        accept="image/jpeg,image/png,image/webp"
        capture="environment"
        onChange={handleFileChange}
      />
    </div>
  );
}
