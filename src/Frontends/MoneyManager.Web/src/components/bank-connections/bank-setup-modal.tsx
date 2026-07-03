"use client";

import { useEffect, useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { StepApiKey } from "./steps/step-api-key";
import { StepSelectConnection } from "./steps/step-select-connection";
import { StepMapAccounts } from "./steps/step-map-accounts";
import { StepStrategy } from "./steps/step-strategy";
import { StepSuccess } from "./steps/step-success";
import { useAvailableConnections } from "@/hooks/use-bank-connections";
import { X } from "lucide-react";
import type {
  BankMcpConnectionDto,
  AccountMappingDto,
} from "@/types/bank-connection";

interface BankSetupModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

type SetupStep =
  | "api-key"
  | "select-connection"
  | "map-accounts"
  | "strategy"
  | "success";

interface SetupState {
  step: SetupStep;
  selectedConnection: BankMcpConnectionDto | null;
  registeredConnectionId: string | null;
  accountMappings: AccountMappingDto[];
}

const STEP_TITLES: Record<SetupStep, string> = {
  "api-key": "Informe sua API key",
  "select-connection": "Selecione o banco",
  "map-accounts": "Associe suas contas",
  strategy: "Dados históricos",
  success: "Banco conectado",
};

const STEP_DESCRIPTIONS: Record<SetupStep, string> = {
  "api-key":
    "Valide sua API key do Banco MCP para carregar seus bancos conectados",
  "select-connection":
    "Escolha qual banco deseja sincronizar com o MoneyManager",
  "map-accounts":
    "Associe cada conta do banco com uma conta já cadastrada no MoneyManager",
  strategy: "Defina como tratar seus lançamentos já existentes",
  success: "Conexao concluida com sucesso",
};

const STEPS: SetupStep[] = [
  "api-key",
  "select-connection",
  "map-accounts",
  "strategy",
  "success",
];

const initialState: SetupState = {
  step: "api-key",
  selectedConnection: null,
  registeredConnectionId: null,
  accountMappings: [],
};

export function BankSetupModal({ open, onOpenChange }: BankSetupModalProps) {
  const [state, setState] = useState<SetupState>(initialState);
  const [showCloseConfirm, setShowCloseConfirm] = useState(false);
  const [initializing, setInitializing] = useState(false);
  const { data: availableData, refetch: refetchAvailable } =
    useAvailableConnections();

  useEffect(() => {
    if (!open) {
      setShowCloseConfirm(false);
      setInitializing(false);
      return;
    }

    let cancelled = false;

    setInitializing(true);
    refetchAvailable().then(({ data }) => {
      if (cancelled) return;

      setState((current) => ({
        ...current,
        step:
          data?.apiKeyExpired || !data?.hasApiKey
            ? "api-key"
            : "select-connection",
      }));
      setInitializing(false);
    });

    return () => {
      cancelled = true;
    };
  }, [open, refetchAvailable]);

  function resetState() {
    setShowCloseConfirm(false);
    setState(initialState);
  }

  function handleClose() {
    onOpenChange(false);
    setTimeout(resetState, 300);
  }

  function handleOpenChange(
    nextOpen: boolean,
    eventDetails?: { reason?: string }
  ) {
    if (
      !nextOpen &&
      eventDetails?.reason === "escape-key" &&
      state.step !== "api-key"
    ) {
      return;
    }

    if (!nextOpen && state.step !== "api-key") {
      setShowCloseConfirm(true);
      return;
    }

    onOpenChange(nextOpen);

    if (!nextOpen) setTimeout(resetState, 300);
  }

  const currentIndex = STEPS.indexOf(state.step);
  const hasMoreBanks = (availableData?.connections ?? []).some(
    (connection) =>
      !connection.alreadyRegistered &&
      !connection.pendingSetup &&
      connection.status !== "LOGIN_ERROR"
  );

  return (
    <Dialog
      open={open}
      onOpenChange={handleOpenChange}
      disablePointerDismissal={state.step !== "api-key"}
    >
      <DialogContent
        showCloseButton={false}
        className="w-full max-w-[calc(100%-2rem)] sm:max-w-2xl lg:max-w-4xl max-h-[90vh] overflow-y-auto p-0"
      >
        <button
          type="button"
          onClick={() => {
            if (state.step === "api-key") {
              handleClose();
              return;
            }

            setShowCloseConfirm(true);
          }}
          className="absolute right-4 top-4 rounded-sm opacity-70 hover:opacity-100"
        >
          <X className="h-4 w-4" />
          <span className="sr-only">Fechar</span>
        </button>

        <div className="px-6 pt-6 pb-4 border-b">
          <div className="flex gap-1.5 mb-4">
            {STEPS.map((s, i) => (
              <div
                key={s}
                className={`h-1 flex-1 rounded-full transition-colors ${
                  currentIndex >= i ? "bg-primary" : "bg-muted"
                }`}
              />
            ))}
          </div>
          <DialogTitle>{STEP_TITLES[state.step]}</DialogTitle>
          <DialogDescription className="mt-1">
            {STEP_DESCRIPTIONS[state.step]}
          </DialogDescription>
        </div>

        <div className="px-6 py-5">
          {initializing ? (
            <div className="flex h-40 items-center justify-center">
              <div className="h-6 w-6 animate-spin rounded-full border-4 border-primary border-t-transparent" />
            </div>
          ) : (
            <>
          {state.step === "api-key" && (
            <StepApiKey
              isExpired={availableData?.apiKeyExpired}
              onSuccess={() =>
                setState((s) => ({ ...s, step: "select-connection" }))
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
              onResume={(connectionId, connection) =>
                setState((s) => ({
                  ...s,
                  step: "map-accounts",
                  selectedConnection: connection,
                  registeredConnectionId: connectionId,
                }))
              }
            />
          )}

          {state.step === "map-accounts" && state.registeredConnectionId && (
            <StepMapAccounts
              connectionId={state.registeredConnectionId}
              institutionName={state.selectedConnection?.connectorName ?? "Banco"}
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
              onSuccess={() => setState((s) => ({ ...s, step: "success" }))}
            />
          )}

          {state.step === "success" && (
            <StepSuccess
              institutionName={state.selectedConnection?.connectorName ?? "Banco"}
              hasMoreBanks={hasMoreBanks}
              onAddAnother={() =>
                setState((s) => ({
                  ...s,
                  step: "select-connection",
                  selectedConnection: null,
                  registeredConnectionId: null,
                  accountMappings: [],
                }))
              }
              onFinish={handleClose}
            />
          )}
            </>
          )}
        </div>
      </DialogContent>

      <AlertDialog open={showCloseConfirm} onOpenChange={setShowCloseConfirm}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Sair da configuracao?</AlertDialogTitle>
            <AlertDialogDescription>
              O banco selecionado ficara pendente de configuracao. Voce pode
              retomar depois na pagina de bancos conectados.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Continuar configurando</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                setShowCloseConfirm(false);
                handleClose();
              }}
            >
              Sair mesmo assim
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </Dialog>
  );
}
