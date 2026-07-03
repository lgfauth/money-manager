"use client";

import { cn } from "@/lib/utils";

interface FinancialEntityCardProps {
  // Header
  indicator: React.ReactNode; // dot colorido (conta) ou ícone (cartão)
  name: string;
  nameSuffix?: React.ReactNode;
  menuTrigger?: React.ReactNode; // botão "..." com dropdown

  // Corpo — conteúdo específico de cada entidade
  children: React.ReactNode;

  // Footer — opcional
  syncBadge?: React.ReactNode; // <BankSyncBadge /> quando vinculado
  action?: React.ReactNode; // "Ver faturas" (cartão) ou nada (conta)

  // Estilo
  accentColor?: string; // cor da borda top (cartão) — undefined = sem borda
  accentPosition?: "top" | "bottom";
  className?: string;
}

export function FinancialEntityCard({
  indicator,
  name,
  nameSuffix,
  menuTrigger,
  children,
  syncBadge,
  action,
  accentColor,
  accentPosition = "top",
  className,
}: FinancialEntityCardProps) {
  const accentStyle =
    accentColor && accentPosition === "bottom"
      ? { borderBottom: `3px solid ${accentColor}` }
      : accentColor
        ? { borderTop: `3px solid ${accentColor}` }
        : undefined;

  return (
    <div
      className={cn(
        "rounded-xl border bg-card p-5 flex flex-col gap-4",
        "hover:border-border/80 transition-colors",
        className
      )}
      style={accentStyle}
    >
      {/* Header — igual nos dois */}
      <div className="flex items-center justify-between gap-2">
        <div className="flex items-center gap-2 min-w-0">
          {indicator}
          <span className="font-semibold text-sm truncate">{name}</span>
          {nameSuffix}
        </div>
        {menuTrigger}
      </div>

      {/* Corpo — flexível */}
      <div className="flex-1">{children}</div>

      {/* Footer — sync badge + ação */}
      {(syncBadge || action) && (
        <div className="flex items-center justify-between gap-2 pt-1 border-t border-border/40">
          <div className="flex-1 min-w-0">{syncBadge}</div>
          {action && <div>{action}</div>}
        </div>
      )}
    </div>
  );
}
