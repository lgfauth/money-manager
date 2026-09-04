"use client";

import { useState } from "react";
import { X, CalendarCheck } from "lucide-react";
import { useDismissSnapshot } from "@/hooks/use-financial-health";
import { CheckinModal } from "@/components/financial-health/checkin-modal";
import { Button } from "@/components/ui/button";
import type { SnapshotStatus } from "@/types/financial-health";

interface CheckinBannerProps {
  status: SnapshotStatus;
}

export function CheckinBanner({ status }: CheckinBannerProps) {
  const [localDismissed, setLocalDismissed] = useState(false);
  const [showModal, setShowModal] = useState(false);
  const dismissSnapshot = useDismissSnapshot();

  if (!status.showBanner || localDismissed) return null;

  const [year, month] = (status.referenceMonth ?? "").split("-").map(Number);

  const handleDismiss = () => {
    dismissSnapshot.mutate(
      { year, month },
      { onSuccess: () => setLocalDismissed(true) }
    );
  };

  return (
    <>
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 sm:gap-4 rounded-xl bg-primary/10 border border-primary/20 px-4 py-3">
        <div className="flex items-start gap-3">
          <CalendarCheck className="h-5 w-5 text-primary shrink-0 mt-0.5 sm:mt-0" />
          <div>
            <p className="text-sm font-medium">
              Check-in disponível para{" "}
              <span className="font-bold">{status.referenceMonth}</span>
            </p>
            <p className="text-xs text-muted-foreground">
              Informe os saldos reais nas corretoras para manter o score preciso.
            </p>
          </div>
        </div>
        <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:shrink-0">
          <div className="flex items-center justify-between sm:justify-end gap-2">
            <Button
              variant="ghost"
              size="sm"
              className="text-xs text-muted-foreground"
              onClick={handleDismiss}
              disabled={dismissSnapshot.isPending}
            >
              Ignorar este mês
            </Button>
            <button
              onClick={() => setLocalDismissed(true)}
              className="rounded p-1 text-muted-foreground hover:text-foreground transition-colors"
              aria-label="Fechar banner"
            >
              <X className="h-4 w-4" />
            </button>
          </div>
          <Button
            size="sm"
            className="w-full sm:w-auto"
            onClick={() => setShowModal(true)}
          >
            Fazer check-in
          </Button>
        </div>
      </div>

      {showModal && (
        <CheckinModal
          open={showModal}
          onClose={() => setShowModal(false)}
          referenceMonth={status.referenceMonth ?? ""}
          pendingBuckets={status.pendingBuckets}
        />
      )}
    </>
  );
}
