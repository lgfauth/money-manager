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
import { Separator } from "@/components/ui/separator";
import { ExternalLink, Key, CreditCard, Building2 } from "lucide-react";

interface StepApiKeyProps {
  onSuccess: (availableConnections: number) => void;
  isExpired?: boolean;
}

const STEPS_GUIDE = [
  {
    icon: CreditCard,
    title: "Crie sua conta no Banco MCP",
    description: (
      <>
        Acesse{" "}
        <a
          href="https://banco.mcp.ai/#pricing"
          target="_blank"
          rel="noopener noreferrer"
          className="text-primary underline underline-offset-2 hover:opacity-80 inline-flex items-center gap-1"
        >
          banco.mcp.ai <ExternalLink className="h-3 w-3" />
        </a>{" "}
        e crie uma conta gratuita. Escolha o <strong>plano Plus</strong>
        (R$&nbsp;29,90/mês) para ter acesso à API - sem ele, a chave de
        integração não é gerada.
      </>
    ),
  },
  {
    icon: Building2,
    title: "Conecte seus bancos",
    description:
      "Dentro do Banco MCP, adicione os bancos que deseja sincronizar com o MoneyManager " +
      "(Nubank, Itaú, Bradesco etc.). O processo usa Open Finance Brasil - você autoriza " +
      "pelo app do seu banco, sem compartilhar senha.",
  },
  {
    icon: Key,
    title: "Gere sua API key",
    description: (
      <>
        No painel do Banco MCP, vá em{" "}
        <strong>Configurações -&gt; API -&gt; Chaves</strong> e clique em{" "}
        <strong>Criar chave</strong>. Copie a chave gerada (começa com{" "}
        <code className="bg-muted px-1 py-0.5 rounded text-xs">sk_live_</code>)
        e cole no campo abaixo. Guarde-a em local seguro - ela não é exibida
        novamente.
      </>
    ),
  },
];

export function StepApiKey({ onSuccess, isExpired = false }: StepApiKeyProps) {
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
      {isExpired && (
        <div className="rounded-lg border border-destructive/20 bg-destructive/10 px-3 py-2">
          <p className="text-sm font-medium text-destructive">
            Sua API key expirou
          </p>
          <p className="mt-0.5 text-xs text-destructive/80">
            Gere uma nova chave no Banco MCP e cole abaixo para reativar a sincronização.
          </p>
        </div>
      )}

      <a
        href="https://banco.mcp.ai/#pricing"
        target="_blank"
        rel="noopener noreferrer"
        className="flex items-center justify-between rounded-lg border border-primary/30 bg-primary/5 px-4 py-3 hover:bg-primary/10 transition-colors group"
      >
        <div>
          <p className="text-sm font-medium">Acessar Banco MCP</p>
          <p className="text-xs text-muted-foreground">banco.mcp.ai</p>
        </div>
        <ExternalLink className="h-4 w-4 text-primary group-hover:translate-x-0.5 transition-transform" />
      </a>

      <Separator />

      <div className="space-y-5">
        <p className="text-sm font-medium text-muted-foreground uppercase tracking-wide">
          Como configurar
        </p>
        {STEPS_GUIDE.map((step, index) => {
          const Icon = step.icon;
          return (
            <div key={index} className="flex gap-4">
              <div className="flex flex-col items-center">
                <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
                  <Icon className="h-4 w-4" />
                </div>
                {index < STEPS_GUIDE.length - 1 && (
                  <div className="mt-2 w-px flex-1 bg-border" />
                )}
              </div>
              <div className="pb-5">
                <p className="text-sm font-medium mb-1">
                  <span className="text-muted-foreground mr-2">{index + 1}.</span>
                  {step.title}
                </p>
                <p className="text-sm text-muted-foreground leading-relaxed">
                  {step.description}
                </p>
              </div>
            </div>
          );
        })}
      </div>

      <Separator />

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="apiKey" className="flex items-center gap-2">
            <Key className="h-3.5 w-3.5" />
            Cole sua API key aqui
          </Label>
          <Input
            id="apiKey"
            type="password"
            placeholder="sk_live_..."
            autoComplete="off"
            className="font-mono"
            {...register("apiKey")}
          />
          {errors.apiKey && (
            <p className="text-xs text-destructive">{errors.apiKey.message}</p>
          )}
          <p className="text-xs text-muted-foreground">
            A chave é salva de forma criptografada e nunca é exibida novamente.
          </p>
        </div>

        {saveApiKey.isError && (
          <div className="rounded-lg bg-destructive/10 border border-destructive/20 px-3 py-2">
            <p className="text-xs text-destructive">
              API key inválida ou sem permissão. Verifique se copiou a chave
              correta e se o plano Plus está ativo no Banco MCP.
            </p>
          </div>
        )}

        <Button
          type="submit"
          className="w-full"
          size="lg"
          disabled={saveApiKey.isPending}
        >
          {saveApiKey.isPending
            ? "Validando chave..."
            : "Validar e continuar ->"}
        </Button>
      </form>
    </div>
  );
}
