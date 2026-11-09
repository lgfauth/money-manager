"use client";

import { Badge } from "@/components/ui/badge";
import { invoiceImportStatusLabels } from "@/lib/invoice-import";
import type { InvoiceImportStatus } from "@/types/invoice-import";

const statusClassNames: Record<InvoiceImportStatus, string> = {
  Queued: "bg-muted text-muted-foreground border-transparent",
  Processing: "bg-primary/15 text-primary border-primary/30",
  AwaitingReview: "bg-amber-500/15 text-amber-700 dark:text-amber-400 border-amber-500/30",
  Failed: "bg-expense/15 text-expense border-expense/30",
  Confirming: "bg-primary/15 text-primary border-primary/30",
  Confirmed: "bg-income/15 text-income border-income/30",
  Discarded: "bg-muted text-muted-foreground border-transparent",
};

export function InvoiceImportStatusBadge({ status }: { status: InvoiceImportStatus }) {
  return (
    <Badge variant="outline" className={statusClassNames[status] ?? statusClassNames.Queued}>
      {invoiceImportStatusLabels[status] ?? status}
    </Badge>
  );
}
