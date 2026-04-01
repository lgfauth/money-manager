"use client";

import { FormEvent, useState } from "react";
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

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void handleLogin();
  }

  return (
    <div className="login-wrap">
      <div className="login-card">
        <h1>Painel Administrativo</h1>
        <p className="muted">Entre com as credenciais configuradas para o backoffice.</p>
        <form className="login-form" method="post" action="/api/login" onSubmit={handleSubmit}>
          <label className="login-field">
            <span>Usuário</span>
            <input
              name="username"
              autoComplete="username"
              placeholder="admin"
              value={username}
              onChange={(event) => setUsername(event.target.value)}
            />
          </label>
          <label className="login-field">
            <span>Senha</span>
            <input
              name="password"
              type="password"
              autoComplete="current-password"
              placeholder="********"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />
          </label>
          {error && <p className="error">{error}</p>}
          <button className="btn btn-primary login-btn" type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Entrando..." : "Entrar"}
          </button>
        </form>
      </div>
    </div>
  );
}
