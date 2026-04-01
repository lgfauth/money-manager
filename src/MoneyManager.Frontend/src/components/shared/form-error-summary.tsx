"use client";

import { AlertCircle } from "lucide-react";
import type { FieldErrors, FieldValues } from "react-hook-form";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";

interface FormErrorSummaryProps<TFieldValues extends FieldValues> {
  errors?: FieldErrors<TFieldValues>;
  submitCount?: number;
  messages?: string[];
  title?: string;
  description?: string;
  className?: string;
}

function collectErrorMessages(errorValue: unknown): string[] {
  if (!errorValue || typeof errorValue !== "object") {
    return [];
  }

  if (
    "message" in errorValue &&
    typeof errorValue.message === "string" &&
    errorValue.message.trim().length > 0
  ) {
    return [errorValue.message];
  }

  return Object.values(errorValue).flatMap((nestedValue) =>
    collectErrorMessages(nestedValue)
  );
}

export function FormErrorSummary<TFieldValues extends FieldValues>({
  errors,
  submitCount = 0,
  messages,
  title = "Nao foi possivel enviar o formulario",
  description = "Revise os campos destacados e corrija os itens abaixo.",
  className,
}: FormErrorSummaryProps<TFieldValues>) {
  const fieldMessages = errors ? collectErrorMessages(errors) : [];
  const allMessages = [...fieldMessages, ...(messages ?? [])];
  const uniqueMessages = Array.from(new Set(allMessages));

  if (submitCount < 1 || uniqueMessages.length === 0) {
    return null;
  }

  return (
    <Alert variant="destructive" className={className} aria-live="assertive">
      <AlertCircle className="h-4 w-4" />
      <AlertTitle>{title}</AlertTitle>
      <AlertDescription>
        <p>{description}</p>
        <ul className="mt-2 list-disc space-y-1 pl-5">
          {uniqueMessages.map((message) => (
            <li key={message}>{message}</li>
          ))}
        </ul>
      </AlertDescription>
    </Alert>
  );
}