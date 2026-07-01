"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  bankMcpApiKeySchema,
  type BankMcpApiKeyFormData,
} from "@/lib/validators";
import { useSaveApiKey } from "@/hooks/use-bank-connections";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ExternalLink } from "lucide-react";

interface StepApiKeyProps {
  onSuccess: (availableConnections: number) => void;
}

export function StepApiKey({ onSuccess }: StepApiKeyProps) {
  const saveApiKey = useSaveApiKey();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<BankMcpApiKeyFormData>({
    resolver: zodResolver(bankMcpApiKeySchema),
  });

  const onSubmit = (data: BankMcpApiKeyFormData) => {
    saveApiKey.mutate(data.apiKey, {
      onSuccess: (result) => onSuccess(result.availableConnections),
    });
  };

  return (
    <div className="space-y-6">
      <div className="rounded-lg bg-muted/50 p-4 space-y-2 text-sm text-muted-foreground">
        <p className="font-medium text-foreground">Como obter sua API key:</p>
        <ol className="list-decimal list-inside space-y-1">
          <li>
            Acesse <strong>banco.mcp.ai</strong> e crie sua conta (plano Plus ou
            superior)
          </li>
          <li>Conecte seus bancos no dashboard do Banco MCP</li>
          <li>
            Vá em <strong>Configurações → API</strong> e copie sua API key
          </li>
          <li>Cole abaixo para começar a importar suas transações</li>
        </ol>
        <a
          href="https://banco.mcp.ai"
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex items-center gap-1 text-primary hover:underline mt-1"
        >
          Acessar Banco MCP <ExternalLink className="h-3 w-3" />
        </a>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="apiKey">API Key</Label>
          <Input
            id="apiKey"
            type="password"
            placeholder="sk_live_..."
            autoComplete="off"
            {...register("apiKey")}
          />
          {errors.apiKey && (
            <p className="text-xs text-destructive">{errors.apiKey.message}</p>
          )}
        </div>

        <Button type="submit" className="w-full" disabled={saveApiKey.isPending}>
          {saveApiKey.isPending ? "Validando..." : "Continuar"}
        </Button>
      </form>
    </div>
  );
}
