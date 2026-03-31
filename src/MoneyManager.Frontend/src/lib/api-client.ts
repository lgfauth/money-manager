import { useAuthStore } from "@/stores/auth-store";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "";

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
    throw new Error("Unauthorized");
  }

  if (!res.ok) {
    const error = await res.text();
    throw new Error(error || `HTTP ${res.status}`);
  }

  const text = await res.text();
  if (!text) return undefined as T;
  return JSON.parse(text) as T;
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
        throw new Error("Unauthorized");
      }
      if (!res.ok) {
        const error = await res.text();
        throw new Error(error || `HTTP ${res.status}`);
      }
      const text = await res.text();
      if (!text) return undefined as T;
      return JSON.parse(text) as T;
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
