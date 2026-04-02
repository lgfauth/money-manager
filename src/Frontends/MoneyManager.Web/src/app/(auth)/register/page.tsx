"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { Loader2 } from "lucide-react";
import { FormErrorSummary } from "@/components/shared/form-error-summary";
import { registerSchema, type RegisterFormData } from "@/lib/validators";
import { useRegister } from "@/hooks/use-auth";

export default function RegisterPage() {
  const {
    register,
    handleSubmit,
    formState: { errors, submitCount },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  });

  const registerMutation = useRegister();

  const onSubmit = (data: RegisterFormData) => {
    registerMutation.mutate({
      name: data.name,
      email: data.email,
      password: data.password,
      confirmPassword: data.confirmPassword,
    });
  };

  return (
    <div className="login-page">
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

      <div className="login-panel-right">
        <h2 className="login-title">Criar conta</h2>
        <p className="login-subtitle">Comece a controlar suas finanças</p>

        <form onSubmit={handleSubmit(onSubmit)}>
          <FormErrorSummary
            errors={errors}
            submitCount={submitCount}
            apiError={registerMutation.error}
            className="mb-4"
          />

          <div className="login-field">
            <label htmlFor="name">NOME</label>
            <input
              id="name"
              type="text"
              placeholder="Seu nome"
              autoComplete="name"
              {...register("name")}
            />
            {errors.name && (
              <p className="mt-1 text-sm text-destructive">{errors.name.message}</p>
            )}
          </div>

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
              placeholder="Mínimo 6 caracteres"
              autoComplete="new-password"
              {...register("password")}
            />
            {errors.password && (
              <p className="mt-1 text-sm text-destructive">{errors.password.message}</p>
            )}
          </div>

          <div className="login-field">
            <label htmlFor="confirmPassword">CONFIRMAR SENHA</label>
            <input
              id="confirmPassword"
              type="password"
              placeholder="Repita a senha"
              autoComplete="new-password"
              {...register("confirmPassword")}
            />
            {errors.confirmPassword && (
              <p className="mt-1 text-sm text-destructive">
                {errors.confirmPassword.message}
              </p>
            )}
          </div>

          <button
            type="submit"
            className="btn-login"
            disabled={registerMutation.isPending}
          >
            {registerMutation.isPending && (
              <Loader2 className="mr-2 inline h-4 w-4 animate-spin" />
            )}
            Criar conta
          </button>
        </form>

        <p className="login-register">
          Já tem uma conta? <Link href="/login">Entrar</Link>
        </p>
      </div>
    </div>
  );
}
