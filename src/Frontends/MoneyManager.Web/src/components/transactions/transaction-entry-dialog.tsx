"use client";

import { useEffect, useState } from "react";

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from "@/components/ui/tabs";
import { BankTransactionFormBody } from "@/components/transactions/bank-transaction-form-body";
import { CreditCardTransactionFormBody } from "@/components/transactions/credit-card-transaction-form-body";

export type TransactionEntryTab = "bank" | "card";

interface TransactionEntryDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  defaultTab?: TransactionEntryTab;
}

export function TransactionEntryDialog({
  open,
  onOpenChange,
  defaultTab = "bank",
}: TransactionEntryDialogProps) {
  const [tab, setTab] = useState<TransactionEntryTab>(defaultTab);

  useEffect(() => {
    if (open) setTab(defaultTab);
  }, [open, defaultTab]);

  const close = () => onOpenChange(false);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-md max-md:mt-[env(safe-area-inset-top,16px)] max-md:mb-[env(safe-area-inset-bottom,16px)] max-md:pt-10 max-md:pb-6">
        <DialogHeader>
          <DialogTitle>Nova transação</DialogTitle>
          <DialogDescription>
            Selecione o tipo de lançamento e preencha os dados.
          </DialogDescription>
        </DialogHeader>

        <Tabs
          value={tab}
          onValueChange={(v) => setTab(v as TransactionEntryTab)}
        >
          <TabsList className="grid w-full grid-cols-2">
            <TabsTrigger value="bank">Bancária</TabsTrigger>
            <TabsTrigger value="card">Cartão</TabsTrigger>
          </TabsList>

          <TabsContent value="bank" className="mt-4">
            <BankTransactionFormBody open={open} onClose={close} />
          </TabsContent>

          <TabsContent value="card" className="mt-4">
            <CreditCardTransactionFormBody open={open} onClose={close} />
          </TabsContent>
        </Tabs>
      </DialogContent>
    </Dialog>
  );
}
