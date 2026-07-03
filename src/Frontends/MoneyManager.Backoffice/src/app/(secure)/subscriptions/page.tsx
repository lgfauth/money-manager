"use client";

import { useEffect, useState } from "react";
import { getSubscriptions, activatePremium, revokePremium } from "@/lib/admin-api";
import type { AdminUserSubscriptionDto } from "@/types/subscription";

interface ActivateModalState {
  isOpen: boolean;
  userId: string;
  userName: string;
  durationDays: number;
}

const STATUS_LABELS: Record<string, string> = {
  Trial: "Trial",
  Active: "Ativa",
  PastDue: "Em Atraso",
  Expired: "Expirada",
  Cancelled: "Cancelada",
};

export default function SubscriptionsPage() {
  const [users, setUsers] = useState<AdminUserSubscriptionDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [activateModal, setActivateModal] = useState<ActivateModalState>({
    isOpen: false,
    userId: "",
    userName: "",
    durationDays: 30,
  });

  async function fetchUsers() {
    setIsLoading(true);
    setError(null);

    try {
      const data = await getSubscriptions();
      setUsers(data);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Erro ao carregar usuários");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    fetchUsers();
  }, []);

  async function handleActivate() {
    setActionLoading(activateModal.userId);

    try {
      await activatePremium(activateModal.userId, activateModal.durationDays);
      setActivateModal({ isOpen: false, userId: "", userName: "", durationDays: 30 });
      await fetchUsers();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Erro ao ativar premium");
    } finally {
      setActionLoading(null);
    }
  }

  async function handleRevoke(userId: string) {
    if (!confirm("Revogar acesso premium imediatamente?")) return;

    setActionLoading(userId);

    try {
      await revokePremium(userId);
      await fetchUsers();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Erro ao revogar premium");
    } finally {
      setActionLoading(null);
    }
  }

  return (
    <section className="stack">
      <article className="card">
        <h2>Controle de Premium</h2>
        <p className="muted">{users.length} usuários carregados.</p>
        {error && <p className="error">{error}</p>}
      </article>

      <article className="card">
        {isLoading && <p className="muted">Carregando usuários...</p>}
        {!isLoading && !error && users.length === 0 && <p className="muted">Nenhum usuário encontrado.</p>}
        {!isLoading && users.length > 0 && (
          <div className="table-wrap">
            <table className="audit-table">
              <thead>
                <tr>
                  <th>Usuário</th>
                  <th>Plano</th>
                  <th>Status</th>
                  <th>Acesso</th>
                  <th>Expira em</th>
                  <th>Cadastro</th>
                  <th>Ações</th>
                </tr>
              </thead>
              <tbody>
                {users.map((user) => (
                  <tr key={user.userId}>
                    <td>
                      <div>{user.name}</div>
                      <div className="muted code-inline">{user.email}</div>
                    </td>
                    <td>
                      <span className={`status-pill plan-${user.plan.toLowerCase()}`}>{user.plan}</span>
                    </td>
                    <td>
                      <span className={`status-pill status-${user.status.toLowerCase()}`}>
                        {STATUS_LABELS[user.status] ?? user.status}
                      </span>
                    </td>
                    <td>
                      <span className={`status-pill ${user.isPremiumActive ? "success" : "failure"}`}>
                        {user.isPremiumActive ? "Ativo" : "Inativo"}
                      </span>
                    </td>
                    <td>
                      {user.currentPeriodEnd
                        ? new Date(user.currentPeriodEnd).toLocaleDateString("pt-BR")
                        : user.trialEndsAt
                        ? new Date(user.trialEndsAt).toLocaleDateString("pt-BR")
                        : "—"}
                    </td>
                    <td>{new Date(user.userCreatedAt).toLocaleDateString("pt-BR")}</td>
                    <td>
                      <div className="actions">
                        <button
                          className="btn btn-primary"
                          type="button"
                          disabled={actionLoading === user.userId}
                          onClick={() =>
                            setActivateModal({
                              isOpen: true,
                              userId: user.userId,
                              userName: user.name,
                              durationDays: 30,
                            })
                          }
                        >
                          {user.isPremiumActive ? "Renovar" : "Ativar"}
                        </button>
                        {user.isPremiumActive && (
                          <button
                            className="btn btn-danger"
                            type="button"
                            disabled={actionLoading === user.userId}
                            onClick={() => handleRevoke(user.userId)}
                          >
                            Revogar
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </article>

      {activateModal.isOpen && (
        <div className="modal-overlay" onClick={() => setActivateModal((s) => ({ ...s, isOpen: false }))}>
          <div className="modal card" onClick={(e) => e.stopPropagation()}>
            <h2>Ativar Premium</h2>
            <p>
              Usuário: <strong>{activateModal.userName}</strong>
            </p>
            <label className="field">
              <span>Duração (dias)</span>
              <input
                type="number"
                min={1}
                max={365}
                value={activateModal.durationDays}
                onChange={(e) =>
                  setActivateModal((s) => ({ ...s, durationDays: Number(e.target.value) }))
                }
              />
            </label>
            <div className="actions modal-actions">
              <button
                className="btn"
                type="button"
                onClick={() => setActivateModal((s) => ({ ...s, isOpen: false }))}
              >
                Cancelar
              </button>
              <button
                className="btn btn-primary"
                type="button"
                disabled={actionLoading === activateModal.userId}
                onClick={handleActivate}
              >
                {actionLoading === activateModal.userId ? "Ativando..." : "Confirmar"}
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  );
}
