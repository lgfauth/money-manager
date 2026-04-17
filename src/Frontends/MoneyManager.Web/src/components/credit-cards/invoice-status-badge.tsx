"use client";

import { Badge } from "@/components/ui/badge";
import type { InvoiceStatus } from "@/types/credit-card";

const statusConfig: Record<
  InvoiceStatus,
  { label: string; className: string }
> = {
  pending: {
    label: "Futura",
    className: "bg-muted text-muted-foreground border-transparent",
  },
  open: {
    label: "Em aberto",
    className: "bg-primary/15 text-primary border-primary/30",
  },
  closed: {
    label: "Fechada",
    className: "bg-secondary text-secondary-foreground border-transparent",
  },
  paid: {
    label: "Paga",
    className: "bg-income/15 text-income border-income/30",
  },
  overdue: {
    label: "Vencida",
    className: "bg-expense/15 text-expense border-expense/30",
  },
};

interface InvoiceStatusBadgeProps {
  status: InvoiceStatus;
}

export function InvoiceStatusBadge({ status }: InvoiceStatusBadgeProps) {
  const config = statusConfig[status] ?? statusConfig.pending;
  return (
    <Badge variant="outline" className={config.className}>
      {config.label}
    </Badge>
  );
}
