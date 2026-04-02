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
}

function parseMoneyInput(rawValue: string): number {
  const sanitizedValue = rawValue.replace(/[^0-9.,]/g, "").trim();

  if (!sanitizedValue) {
    return 0;
  }

  const lastCommaIndex = sanitizedValue.lastIndexOf(",");
  const lastDotIndex = sanitizedValue.lastIndexOf(".");
  const decimalIndex = Math.max(lastCommaIndex, lastDotIndex);

  if (decimalIndex === -1) {
    const integerValue = Number(sanitizedValue.replace(/[.,]/g, ""));
    return Number.isFinite(integerValue) ? integerValue : 0;
  }

  const integerPart = sanitizedValue.slice(0, decimalIndex).replace(/[.,]/g, "");
  const decimalPart = sanitizedValue.slice(decimalIndex + 1).replace(/[.,]/g, "");
  const normalizedValue = `${integerPart || "0"}.${decimalPart}`;
  const parsedValue = Number(normalizedValue);

  return Number.isFinite(parsedValue) ? parsedValue : 0;
}

export function MoneyInput({
  value,
  onChange,
  currencyCode = "BRL",
  locale = "pt-BR",
  className,
  placeholder,
  id,
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
    onChange(parseMoneyInput(displayValue));
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const raw = e.target.value.replace(/[^0-9.,]/g, "");
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
