"use client";

// O token de admin é armazenado exclusivamente em cookie httpOnly definido server-side.
// JavaScript não consegue lê-lo diretamente — o proxy lida com a injeção do Authorization header.

export function saveAdminToken(_token: string): void {
  // No-op: o cookie httpOnly é definido server-side pela route de login.
}

export function getAdminToken(): string | null {
  // No-op: cookie httpOnly não é acessível por JavaScript.
  // A autenticação é injetada pelo proxy server-side.
  return null;
}

export function clearAdminToken(): void {
  // Redireciona para a route de logout que limpa o cookie server-side.
  if (typeof window !== "undefined") {
    window.location.href = "/api/logout";
  }
}
