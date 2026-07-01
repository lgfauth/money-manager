"use client";

import { useState } from "react";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from "@/components/ui/sheet";
import { StepApiKey } from "./steps/step-api-key";
import { StepSelectConnection } from "./steps/step-select-connection";
import { StepMapAccounts } from "./steps/step-map-accounts";
import { StepStrategy } from "./steps/step-strategy";
import type {
  BankMcpConnectionDto,
  AccountMappingDto,
} from "@/types/bank-connection";

interface BankSetupSheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

type SetupStep = "api-key" | "select-connection" | "map-accounts" | "strategy";

interface SetupState {
  step: SetupStep;
  availableConnections: number;
  selectedConnection: BankMcpConnectionDto | null;
  registeredConnectionId: string | null;
  accountMappings: AccountMappingDto[];
}

const STEP_TITLES: Record<SetupStep, string> = {
  "api-key": "Conecte sua conta Banco MCP",
  "select-connection": "Selecione o banco",
  "map-accounts": "Associe suas contas",
  strategy: "Dados históricos",
};

const STEP_DESCRIPTIONS: Record<SetupStep, string> = {
  "api-key":
    "Insira sua API key do Banco MCP para importar transações automaticamente",
  "select-connection":
    "Escolha qual banco deseja sincronizar com o MoneyManager",
  "map-accounts":
    "Associe cada conta do banco com uma conta já cadastrada no MoneyManager",
  strategy: "Defina como tratar seus lançamentos já existentes",
};

const STEPS: SetupStep[] = [
  "api-key",
  "select-connection",
  "map-accounts",
  "strategy",
];

const initialState: SetupState = {
  step: "api-key",
  availableConnections: 0,
  selectedConnection: null,
  registeredConnectionId: null,
  accountMappings: [],
};

export function BankSetupSheet({ open, onOpenChange }: BankSetupSheetProps) {
  const [state, setState] = useState<SetupState>(initialState);

  function handleClose() {
    onOpenChange(false);
    setTimeout(() => setState(initialState), 300);
  }

  return (
    <Sheet open={open} onOpenChange={handleClose}>
      <SheetContent className="w-full sm:max-w-lg overflow-y-auto">
        <SheetHeader className="mb-6">
          <div className="flex gap-1.5 mb-4">
            {STEPS.map((s, i) => (
              <div
                key={s}
                className={`h-1 flex-1 rounded-full transition-colors ${
                  STEPS.indexOf(state.step) >= i ? "bg-primary" : "bg-muted"
                }`}
              />
            ))}
          </div>
          <SheetTitle>{STEP_TITLES[state.step]}</SheetTitle>
          <SheetDescription>{STEP_DESCRIPTIONS[state.step]}</SheetDescription>
        </SheetHeader>

        {state.step === "api-key" && (
          <StepApiKey
            onSuccess={(availableConnections) =>
              setState((s) => ({
                ...s,
                step: "select-connection",
                availableConnections,
              }))
            }
          />
        )}

        {state.step === "select-connection" && (
          <StepSelectConnection
            onBack={() => setState((s) => ({ ...s, step: "api-key" }))}
            onSuccess={(connection, registeredConnectionId) =>
              setState((s) => ({
                ...s,
                step: "map-accounts",
                selectedConnection: connection,
                registeredConnectionId,
              }))
            }
          />
        )}

        {state.step === "map-accounts" && state.registeredConnectionId && (
          <StepMapAccounts
            connectionId={state.registeredConnectionId}
            institutionName={
              state.selectedConnection?.institutionName ?? "Banco"
            }
            onBack={() =>
              setState((s) => ({ ...s, step: "select-connection" }))
            }
            onSuccess={(mappings) =>
              setState((s) => ({
                ...s,
                step: "strategy",
                accountMappings: mappings,
              }))
            }
          />
        )}

        {state.step === "strategy" && state.registeredConnectionId && (
          <StepStrategy
            connectionId={state.registeredConnectionId}
            accountMappings={state.accountMappings}
            onBack={() => setState((s) => ({ ...s, step: "map-accounts" }))}
            onSuccess={handleClose}
          />
        )}
      </SheetContent>
    </Sheet>
  );
}
