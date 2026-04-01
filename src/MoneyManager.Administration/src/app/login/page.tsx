"use client";

import { KeyboardEvent, useState } from "react";
import { login } from "@/lib/admin-api";
import { saveAdminToken } from "@/lib/admin-auth";

export default function LoginPage() {
  const [username, setUsername] = useState("admin");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleLogin() {
    if (isSubmitting) {
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      const result = await login(username, password);
      saveAdminToken(result.accessToken);
      window.location.href = "/";
    } catch (err) {
      setError(err instanceof Error ? err.message : "Login failed");
    } finally {
      setIsSubmitting(false);
    }
  }

  function handlePasswordKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "Enter") {
      event.preventDefault();
      void handleLogin();
    }
  }

  return (
    <div className="login-wrap">
      <div className="login-card">
        <h1>Admin Login</h1>
        <p className="muted">Use credenciais configuradas por variavel de ambiente.</p>
        <div>
          <input
            placeholder="Username"
            value={username}
            onChange={(event) => setUsername(event.target.value)}
          />
          <input
            type="password"
            placeholder="Password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            onKeyDown={handlePasswordKeyDown}
          />
          {error && <p className="error">{error}</p>}
          <button className="btn btn-primary" type="button" disabled={isSubmitting} onClick={() => void handleLogin()}>
            {isSubmitting ? "Entrando..." : "Entrar"}
          </button>
        </div>
      </div>
    </div>
  );
}
