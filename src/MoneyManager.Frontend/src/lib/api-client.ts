import { useAuthStore } from "@/stores/auth-store";
import { createApiClientError } from "@/lib/api-errors";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "";

async function parseResponseBody(response: Response): Promise<unknown> {
  const text = await response.text();

  if (!text) {
    return undefined;
  }

  const contentType = response.headers.get("content-type") ?? "";
  if (contentType.includes("application/json")) {
    try {
      return JSON.parse(text);
    } catch {
      return text;
    }
  }

  return text;
}

async function fetchWithAuth<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const token = useAuthStore.getState().token;

  const res = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(token && { Authorization: `Bearer ${token}` }),
      ...options.headers,
    },
  });

  if (res.status === 401) {
    useAuthStore.getState().logout();
    if (typeof window !== "undefined") {
      window.location.href = "/login";
    }
    throw createApiClientError(401, "Sessao expirada. Faca login novamente.");
  }

  const body = await parseResponseBody(res);

  if (!res.ok) {
    throw createApiClientError(res.status, body, `HTTP ${res.status}`);
  }

  return body as T;
}

export const apiClient = {
  get: <T>(path: string) => fetchWithAuth<T>(path),

  post: <T>(path: string, body?: unknown) =>
    fetchWithAuth<T>(path, {
      method: "POST",
      body: body ? JSON.stringify(body) : undefined,
    }),

  postForm: <T>(path: string, formData: FormData) => {
    const token = useAuthStore.getState().token;
    return fetch(`${API_URL}${path}`, {
      method: "POST",
      headers: {
        ...(token && { Authorization: `Bearer ${token}` }),
      },
      body: formData,
    }).then(async (res) => {
      if (res.status === 401) {
        useAuthStore.getState().logout();
        if (typeof window !== "undefined") window.location.href = "/login";
        throw createApiClientError(401, "Sessao expirada. Faca login novamente.");
      }

      const body = await parseResponseBody(res);

      if (!res.ok) {
        throw createApiClientError(res.status, body, `HTTP ${res.status}`);
      }

      return body as T;
    });
  },

  put: <T>(path: string, body?: unknown) =>
    fetchWithAuth<T>(path, {
      method: "PUT",
      body: body ? JSON.stringify(body) : undefined,
    }),

  delete: <T>(path: string) =>
    fetchWithAuth<T>(path, { method: "DELETE" }),
};
