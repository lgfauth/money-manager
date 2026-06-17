"use client";

import { useState } from "react";
import { useConfirmSnapshot } from "@/hooks/use-financial-health";
import type { PendingBucketStatus } from "@/types/financial-health";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { MoneyInput } from "@/components/shared/money-input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { DEFAULT_CURRENCY, DEFAULT_LOCALE } from "@/config/constants";

interface CheckinModalProps {
  open: boolean;
  onClose: () => void;
  referenceMonth: string;
  pendingBuckets: PendingBucketStatus[];
}

const bucketLabel: Record<string, string> = {
  emergency_reserve: "Reserva de Emergência",
  fire_investment: "Investimentos FIRE",
};

function formatCurrency(value: number) {
  return new Intl.NumberFormat(DEFAULT_LOCALE, {
    style: "currency",
    currency: DEFAULT_CURRENCY,
  }).format(value);
}

export function CheckinModal({
  open,
  onClose,
  referenceMonth,
  pendingBuckets,
}: CheckinModalProps) {
  const confirmSnapshot = useConfirmSnapshot();

  const [balances, setBalances] = useState<Record<string, number>>(() =>
    Object.fromEntries(
      pendingBuckets.map((b) => [b.bucketId, b.estimatedBalance])
    )
  );

  const [year, month] = referenceMonth.split("-").map(Number);

  const handleConfirm = () => {
    confirmSnapshot.mutate(
      {
        year,
        month,
        data: {
          buckets: pendingBuckets.map((b) => ({
            bucketId: b.bucketId,
            confirmedBalance: balances[b.bucketId] ?? b.estimatedBalance,
          })),
        },
      },
      { onSuccess: onClose }
    );
  };

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Check-in mensal</DialogTitle>
          <DialogDescription>
            Informe os saldos reais nas corretoras para o mês de{" "}
            <strong>{referenceMonth}</strong>. Se preferir, use o valor estimado.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 py-2">
          {pendingBuckets.map((bucket) => (
            <div key={bucket.bucketId} className="space-y-3">
              <p className="font-medium text-sm">
                {bucketLabel[bucket.bucketType] ?? bucket.bucketType}
              </p>
              <div className="grid grid-cols-3 gap-2 text-xs text-muted-foreground">
                <div>
                  <p>Estimado</p>
                  <p className="font-semibold text-foreground text-sm">
                    {formatCurrency(bucket.estimatedBalance)}
                  </p>
                </div>
                <div>
                  <p>Aportes rastreados</p>
                  <p className="font-semibold text-foreground text-sm">
                    {formatCurrency(bucket.trackedContributions)}
                  </p>
                </div>
                <div>
                  <p>Rendimento estimado</p>
                  <p className="font-semibold text-foreground text-sm">
                    {formatCurrency(bucket.estimatedYield)}
                  </p>
                </div>
              </div>
              <div className="space-y-1">
                <Label className="text-xs">Saldo real na corretora</Label>
                <MoneyInput
                  value={balances[bucket.bucketId] ?? bucket.estimatedBalance}
                  onChange={(v) =>
                    setBalances((prev) => ({ ...prev, [bucket.bucketId]: v }))
                  }
                />
              </div>
            </div>
          ))}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={confirmSnapshot.isPending}>
            Cancelar
          </Button>
          <Button onClick={handleConfirm} disabled={confirmSnapshot.isPending}>
            {confirmSnapshot.isPending ? "Confirmando..." : "Confirmar check-in"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
