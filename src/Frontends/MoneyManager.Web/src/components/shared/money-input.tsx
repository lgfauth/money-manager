"use client";

import { useState, useRef, useEffect } from "react";
import { Input } from "@/components/ui/input";
import currency from "currency.js";

interface MoneyInputProps {
  value: number;
  onChange: (value: number) => void;
  currencyCode?: string;
  locale?: string;
  className?: string;
  placeholder?: string;
  id?: string;
  allowNegative?: boolean;
}

function parseMoneyInput(rawValue: string, allowNegative = false): number {
  const trimmedValue = rawValue.trim();
  const isNegative = allowNegative && trimmedValue.startsWith("-");
  const sanitizedValue = trimmedValue.replace(/[^0-9.,]/g, "").trim();

  if (!sanitizedValue) {
    return 0;
  }

  const lastCommaIndex = sanitizedValue.lastIndexOf(",");
  const lastDotIndex = sanitizedValue.lastIndexOf(".");
  const decimalIndex = Math.max(lastCommaIndex, lastDotIndex);

  if (decimalIndex === -1) {
    const integerValue = Number(sanitizedValue.replace(/[.,]/g, ""));
    if (!Number.isFinite(integerValue)) {
      return 0;
    }

    return isNegative ? -integerValue : integerValue;
  }

  const integerPart = sanitizedValue.slice(0, decimalIndex).replace(/[.,]/g, "");
  const decimalPart = sanitizedValue.slice(decimalIndex + 1).replace(/[.,]/g, "");
  const normalizedValue = `${integerPart || "0"}.${decimalPart}`;
  const parsedValue = Number(normalizedValue);

  if (!Number.isFinite(parsedValue)) {
    return 0;
  }

  return isNegative ? -parsedValue : parsedValue;
}

export function MoneyInput({
  value,
  onChange,
  currencyCode = "BRL",
  locale = "pt-BR",
  className,
  placeholder,
  id,
  allowNegative = false,
}: MoneyInputProps) {
  const [displayValue, setDisplayValue] = useState("");
  const [isFocused, setIsFocused] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const symbol =
    new Intl.NumberFormat(locale, {
      style: "currency",
      currency: currencyCode,
    })
      .formatToParts(0)
      .find((p) => p.type === "currency")?.value ?? "R$";

  useEffect(() => {
    if (!isFocused) {
      setDisplayValue(
        value
          ? currency(value, { symbol: "", separator: ".", decimal: "," }).format()
          : ""
      );
    }
  }, [value, isFocused]);

  const handleFocus = () => {
    setIsFocused(true);
    setDisplayValue(value ? String(value) : "");
  };

  const handleBlur = () => {
    setIsFocused(false);
    onChange(parseMoneyInput(displayValue, allowNegative));
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const inputValue = e.target.value;
    const hasLeadingMinus = allowNegative && inputValue.trimStart().startsWith("-");
    const numericPart = inputValue.replace(/[^0-9.,]/g, "");
    const raw = hasLeadingMinus ? `-${numericPart}` : numericPart;
    setDisplayValue(raw);
  };

  return (
    <div className="relative">
      <span className="absolute left-3 top-1/2 -translate-y-1/2 text-sm text-muted-foreground">
        {symbol}
      </span>
      <Input
        ref={inputRef}
        id={id}
        type="text"
        inputMode="decimal"
        className={`pl-10 ${className ?? ""}`}
        value={displayValue}
        placeholder={placeholder ?? "0,00"}
        onChange={handleChange}
        onFocus={handleFocus}
        onBlur={handleBlur}
      />
    </div>
  );
}
