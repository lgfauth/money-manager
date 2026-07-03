"use client";

interface CardBrandIconProps {
  brand: string | null | undefined;
  className?: string;
}

const BRAND_STYLES: Record<string, { label: string; className: string }> = {
  MASTERCARD: {
    label: "Mastercard",
    className: "bg-red-500/10 text-red-500 border-red-500/20",
  },
  VISA: {
    label: "Visa",
    className: "bg-blue-500/10 text-blue-500 border-blue-500/20",
  },
  ELO: {
    label: "Elo",
    className: "bg-yellow-500/10 text-yellow-600 border-yellow-500/20",
  },
  AMEX: {
    label: "Amex",
    className: "bg-green-500/10 text-green-600 border-green-500/20",
  },
  HIPERCARD: {
    label: "Hipercard",
    className: "bg-rose-500/10 text-rose-500 border-rose-500/20",
  },
};

export function CardBrandIcon({ brand, className }: CardBrandIconProps) {
  if (!brand) return null;

  const style = BRAND_STYLES[brand.toUpperCase()] ?? {
    label: brand,
    className: "bg-muted text-muted-foreground border-border",
  };

  return (
    <span
      className={`inline-flex items-center px-1.5 py-0.5 rounded border text-[10px] font-bold tracking-wider uppercase ${style.className} ${className ?? ""}`}
    >
      {style.label}
    </span>
  );
}
