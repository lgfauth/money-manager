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
    const parsed = parseFloat(displayValue.replace(/\./g, "").replace(",", "."));
    if (!isNaN(parsed)) {
      onChange(parsed);
    } else {
      onChange(0);
    }
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
