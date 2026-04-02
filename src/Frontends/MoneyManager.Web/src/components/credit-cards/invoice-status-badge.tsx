"use client";

import { Badge } from "@/components/ui/badge";
import { InvoiceStatus } from "@/types/invoice";

const statusConfig: Record<
  string,
  { label: string; variant: "default" | "secondary" | "destructive" | "outline" }
> = {
  [InvoiceStatus.Open]: { label: "Aberta", variant: "secondary" },
  [InvoiceStatus.Closed]: { label: "Fechada", variant: "default" },
  [InvoiceStatus.Paid]: { label: "Paga", variant: "outline" },
  [InvoiceStatus.PartiallyPaid]: { label: "Parcial", variant: "default" },
  [InvoiceStatus.Overdue]: { label: "Vencida", variant: "destructive" },
};

interface InvoiceStatusBadgeProps {
  status: InvoiceStatus;
  isOverdue?: boolean;
}

export function InvoiceStatusBadge({ status, isOverdue }: InvoiceStatusBadgeProps) {
  if (isOverdue && status !== InvoiceStatus.Paid) {
    return (
      <Badge variant="destructive" className="animate-pulse">
        Vencida
      </Badge>
    );
  }

  const config = statusConfig[status] ?? statusConfig[InvoiceStatus.Open];
  return <Badge variant={config.variant}>{config.label}</Badge>;
}
