"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { Loader2 } from "lucide-react";
import { loginSchema, type LoginFormData } from "@/lib/validators";
import { useLogin } from "@/hooks/use-auth";

export default function LoginPage() {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  const loginMutation = useLogin();

  const onSubmit = (data: LoginFormData) => {
    loginMutation.mutate(data);
  };

  return (
    <div className="login-page">
      {/* Painel esquerdo — escuro */}
      <div className="login-panel-left">
        <div className="login-logo">
          <svg width="24" height="24" viewBox="0 0 64 64" fill="none">
            <polyline
              points="10,44 22,28 32,36 44,18 54,26"
              stroke="#0A2B1E"
              strokeWidth="4.5"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
            <circle cx="44" cy="18" r="5" fill="#0A2B1E" />
          </svg>
        </div>
        <h1 className="login-brand">MoneyManager</h1>
        <p className="login-tagline">Controle financeiro inteligente</p>
      </div>

      {/* Painel direito — formulário */}
      <div className="login-panel-right">
        <h2 className="login-title">Bem-vindo</h2>
        <p className="login-subtitle">Entre com suas credenciais</p>

        <form onSubmit={handleSubmit(onSubmit)}>
          <div className="login-field">
            <label htmlFor="email">E-MAIL</label>
            <input
              id="email"
              type="email"
              placeholder="seu@email.com"
              autoComplete="email"
              {...register("email")}
            />
            {errors.email && (
              <p className="mt-1 text-sm text-destructive">{errors.email.message}</p>
            )}
          </div>

          <div className="login-field">
            <label htmlFor="password">SENHA</label>
            <input
              id="password"
              type="password"
              placeholder="••••••"
              autoComplete="current-password"
              {...register("password")}
            />
            {errors.password && (
              <p className="mt-1 text-sm text-destructive">
                {errors.password.message}
              </p>
            )}
          </div>

          <button
            type="submit"
            className="btn-login"
            disabled={loginMutation.isPending}
          >
            {loginMutation.isPending && (
              <Loader2 className="mr-2 inline h-4 w-4 animate-spin" />
            )}
            Entrar
          </button>
        </form>

        <p className="login-register">
          Não tem uma conta?{" "}
          <Link href="/register">Cadastre-se</Link>
        </p>
      </div>
    </div>
  );
}
