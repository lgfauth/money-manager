"use client";

import { useMutation } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { apiClient } from "@/lib/api-client";
import { getApiErrorMessage } from "@/lib/api-errors";
import { useAuthStore } from "@/stores/auth-store";
import { queryClient } from "@/lib/query-client";
import type { LoginRequestDto, LoginResponseDto, RegisterRequestDto } from "@/types/api";
import { toast } from "sonner";

function isLoginResponseDto(value: unknown): value is LoginResponseDto {
  if (!value || typeof value !== "object") {
    return false;
  }

  const candidate = value as Partial<LoginResponseDto>;
  return (
    typeof candidate.id === "string" &&
    typeof candidate.name === "string" &&
    typeof candidate.email === "string"
  );
}

export function useLogin() {
  const { login, hydrate } = useAuthStore();
  const router = useRouter();

  return useMutation({
    mutationFn: (data: LoginRequestDto) =>
      apiClient.post<LoginResponseDto | undefined>("/api/auth/login", data),
    onSuccess: async (response) => {
      if (isLoginResponseDto(response)) {
        login({ id: response.id, name: response.name, email: response.email }, response.token);
      } else {
        // Fallback para backend que autentica por cookie sem retornar o usuário no body.
        await hydrate();
      }

      if (!useAuthStore.getState().isAuthenticated) {
        throw new Error("Sessão não foi estabelecida após o login");
      }

      router.push("/");
    },
    onError: (error) => {
      toast.error(getApiErrorMessage(error, "E-mail ou senha inválidos"));
    },
  });
}

export function useRegister() {
  const { login } = useAuthStore();
  const router = useRouter();

  return useMutation({
    mutationFn: (data: RegisterRequestDto) =>
      apiClient.post<LoginResponseDto>("/api/auth/register", data),
    onSuccess: (response) => {
      login({ id: response.id, name: response.name, email: response.email }, response.token);
      router.push("/onboarding");
    },
    onError: (error) => {
      toast.error(getApiErrorMessage(error, "Erro ao criar conta"));
    },
  });
}

export function useLogout() {
  const { logout } = useAuthStore();
  const router = useRouter();

  return async () => {
    await fetch(`${process.env.NEXT_PUBLIC_API_URL ?? ""}/api/auth/logout`, {
      method: "POST",
      credentials: "include",
    }).catch(() => {});
    logout();
    queryClient.clear();
    router.push("/login");
  };
}
