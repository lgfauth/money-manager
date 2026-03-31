"use client";

import { useMutation } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { apiClient } from "@/lib/api-client";
import { useAuthStore } from "@/stores/auth-store";
import { queryClient } from "@/lib/query-client";
import type { LoginRequestDto, LoginResponseDto, RegisterRequestDto } from "@/types/api";
import { toast } from "sonner";

export function useLogin() {
  const { login } = useAuthStore();
  const router = useRouter();

  return useMutation({
    mutationFn: (data: LoginRequestDto) =>
      apiClient.post<LoginResponseDto>("/api/auth/login", data),
    onSuccess: (response) => {
      login(response.token);
      router.push("/");
    },
    onError: () => {
      toast.error("E-mail ou senha inválidos");
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
      login(response.token);
      router.push("/onboarding");
    },
    onError: (error: Error) => {
      toast.error(error.message || "Erro ao criar conta");
    },
  });
}

export function useLogout() {
  const { logout } = useAuthStore();
  const router = useRouter();

  return () => {
    logout();
    queryClient.clear();
    router.push("/login");
  };
}
