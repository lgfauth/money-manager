"use client";

import { useState } from "react";
import { Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { AccountFormContent } from "@/components/accounts/account-form";
import { CreditCardFormContent } from "@/components/credit-cards/credit-card-form";

export function AddFinancialEntityButton() {
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button onClick={() => setOpen(true)}>
        <Plus className="h-4 w-4 mr-1" /> Adicionar
      </Button>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Adicionar</DialogTitle>
          </DialogHeader>

          <Tabs defaultValue="conta">
            <TabsList className="w-full">
              <TabsTrigger value="conta" className="flex-1">
                Conta
              </TabsTrigger>
              <TabsTrigger value="cartao" className="flex-1">
                Cartão de Crédito
              </TabsTrigger>
            </TabsList>

            <TabsContent value="conta" className="mt-4 space-y-4">
              <div className="rounded-lg bg-muted/50 px-3 py-2 text-xs text-muted-foreground">
                Crie uma conta offline para organizar seus lançamentos
                manualmente. Para sincronizar automaticamente com seu banco
                real, vincule a conta ao Banco MCP depois de criá-la.
              </div>
              <AccountFormContent onSuccess={() => setOpen(false)} />
            </TabsContent>

            <TabsContent value="cartao" className="mt-4 space-y-4">
              <div className="rounded-lg bg-muted/50 px-3 py-2 text-xs text-muted-foreground">
                Crie um cartão offline para controlar faturas manualmente.
                Para sincronizar automaticamente com seu cartão real,
                vincule-o ao Banco MCP depois de criá-lo — é necessário ter
                uma conta offline criada para fazer o vínculo.
              </div>
              <CreditCardFormContent onSuccess={() => setOpen(false)} />
            </TabsContent>
          </Tabs>
        </DialogContent>
      </Dialog>
    </>
  );
}
