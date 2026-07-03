"use client";

import { FormEvent, useState } from "react";

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
      const formData = new FormData();
      formData.set("username", username);
      formData.set("password", password);

      const response = await fetch("/api/login", {
        method: "POST",
        body: formData,
      });

      const data = (await response.json()) as { ok: boolean; error?: string };

      if (data.ok) {
        window.location.href = "/";
      } else {
        setError(data.error ?? "Credenciais inválidas");
      }
    } catch {
      setError("Erro ao conectar com o servidor");
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
