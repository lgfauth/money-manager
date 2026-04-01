"use client";

const TOKEN_KEY = "mm_admin_token";
const COOKIE_KEY = "mm_admin_token";

function readCookie(name: string): string | null {
  const encodedName = `${name}=`;
  const parts = document.cookie.split(";");
  for (const part of parts) {
    const trimmed = part.trim();
    if (trimmed.startsWith(encodedName)) {
      return decodeURIComponent(trimmed.substring(encodedName.length));
    }
  }

  return null;
}

export function saveAdminToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
  document.cookie = `${COOKIE_KEY}=${encodeURIComponent(token)}; Path=/; Max-Age=3600; SameSite=Lax`;
}

export function getAdminToken(): string | null {
  const fromStorage = localStorage.getItem(TOKEN_KEY);
  if (fromStorage) {
    return fromStorage;
  }

  return readCookie(COOKIE_KEY);
}

export function clearAdminToken(): void {
  localStorage.removeItem(TOKEN_KEY);
  document.cookie = `${COOKIE_KEY}=; Path=/; Max-Age=0; SameSite=Lax`;
}
