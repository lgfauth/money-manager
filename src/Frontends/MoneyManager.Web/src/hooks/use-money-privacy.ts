"use client";

import { useCallback } from "react";
import {
  formatCurrencyValue,
  obfuscateNumericChars,
} from "@/lib/money-privacy";
import { useMoneyPrivacyStore } from "@/stores/money-privacy-store";

export function useMoneyPrivacy() {
  const hideMoneyValues = useMoneyPrivacyStore((state) => state.hideMoneyValues);

  const formatMonetaryValue = useCallback(
    (value: number, currency = "BRL", locale = "pt-BR") => {
      const formatted = formatCurrencyValue(value, currency, locale);
      return hideMoneyValues ? obfuscateNumericChars(formatted) : formatted;
    },
    [hideMoneyValues]
  );

  const maskMonetaryText = useCallback(
    (text: string) => (hideMoneyValues ? obfuscateNumericChars(text) : text),
    [hideMoneyValues]
  );

  return {
    hideMoneyValues,
    formatMonetaryValue,
    maskMonetaryText,
  };
}