import Link from "next/link";
import { LogoutButton } from "@/components/logout-button";

export default function SecureLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="admin-shell">
      <aside className="admin-sidebar">
        <h1>MoneyManager Admin</h1>
        <nav>
          <Link href="/">Visao Geral</Link>
          <Link href="/jobs">Jobs</Link>
          <Link href="/errors-latency">Erros e Latencia</Link>
          <Link href="/financial-maintenance">Manutencao Financeira</Link>
          <Link href="/audit">Auditoria</Link>
        </nav>
      </aside>
      <main className="admin-main">
        <div className="admin-topbar">
          <h2>Portal Administrativo</h2>
          <LogoutButton />
        </div>
        {children}
      </main>
    </div>
  );
}
