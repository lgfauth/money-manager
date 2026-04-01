# MoneyManager Next.js — Plano de Execucao por Fases

> Documento de rastreamento para a reconstrucao do frontend em React + Next.js.
> Baseado em `NEXTJS_FRONTEND_SPEC.md` e `FRONTEND_DOCUMENTATION.md`.
>
> **Convencao:** [ ] = pendente, [~] = em progresso, [x] = concluido

---

## Fase 0 — Scaffolding do Projeto ✅
**Esforco:** Baixo | **Dependencias:** Nenhuma

- [x] Criar projeto Next.js 15 (App Router) com TypeScript
- [x] Instalar dependencias core (Tailwind CSS 4, shadcn/ui)
- [x] Instalar libs de estado (TanStack Query v5, Zustand, nuqs)
- [x] Instalar libs de formulario (React Hook Form, Zod)
- [x] Instalar libs de UI (Framer Motion, Recharts, Lucide React, Sonner, Vaul)
- [x] Instalar libs de infra (date-fns, currency.js, next-themes, next-intl)
- [x] Configurar estrutura de pastas (src/app, components, hooks, lib, stores, types, config)
- [x] Criar `.env.example` e `.env.local`
- [x] Configurar `next.config.ts` (standalone output, images)
- [x] Configurar `tsconfig.json` (paths aliases @/)

---

## Fase 1 — Foundation Layer (Design System + Tipos + Config) ✅
**Esforco:** Baixo-Medio | **Dependencias:** Fase 0

- [x] Definir CSS tokens em `globals.css` (light + dark, cores semanticas financeiras)
- [x] Configurar `tailwind.config.ts` (darkMode, cores custom income/expense/investment)
- [x] Instalar componentes shadcn/ui essenciais (button, card, dialog, input, select, badge, skeleton, table, tabs, tooltip, progress, sheet, dropdown-menu, command, popover, calendar, switch, label, separator, avatar, accordion)
- [x] Criar `src/lib/utils.ts` (cn(), formatCurrency(), formatDate(), helpers)
- [x] Criar tipos TypeScript (`src/types/`): account, transaction, category, budget, invoice, recurring, profile, settings, api
- [x] Criar configs (`src/config/`): navigation.ts, currencies.ts, constants.ts
- [x] Criar `src/lib/api-client.ts` (fetch wrapper com JWT, interceptor 401)
- [x] Criar `src/stores/auth-store.ts` (Zustand: token, user, login, logout)
- [x] Criar `src/stores/ui-store.ts` (Zustand: sidebar, command palette)
- [x] Criar `src/stores/settings-store.ts` (Zustand: tema, idioma, moeda client)
- [x] Criar `src/lib/query-client.ts` (config TanStack Query: staleTime, gcTime, retry)
- [x] Criar `src/lib/validators.ts` (schemas Zod compartilhados)

---

## Fase 2 — API Client + Autenticacao ✅
**Esforco:** Medio | **Dependencias:** Fase 1

- [x] Criar `src/lib/api-client.ts` (movido para Fase 1)
- [x] Criar `src/stores/auth-store.ts` (movido para Fase 1)
- [x] Criar `src/lib/query-client.ts` (movido para Fase 1)
- [x] Criar `src/hooks/use-auth.ts` (useLogin, useRegister mutations)
- [x] Criar Auth layout (`src/app/(auth)/layout.tsx`) — fundo gradiente, sem sidebar
- [x] Criar pagina Login (`src/app/(auth)/login/page.tsx`) — form com Zod
- [x] Criar pagina Register (`src/app/(auth)/register/page.tsx`) — form com Zod + auto-login
- [x] Criar Root layout (`src/app/layout.tsx`) — providers (ThemeProvider, QueryClientProvider)
- [x] Criar `src/components/providers.tsx` (QueryClientProvider, ThemeProvider, Toaster, AuthHydration)

---

## Fase 3 — Layout Principal e Navegacao ✅
**Esforco:** Medio | **Dependencias:** Fase 2

- [x] Criar `src/stores/ui-store.ts` (movido para Fase 1)
- [x] Criar Dashboard layout (`src/app/(dashboard)/layout.tsx`) — protecao de rota + estrutura
- [x] Criar `src/components/layout/sidebar.tsx` — colapsavel, animada, itens de navegacao
- [x] Criar `src/components/layout/header.tsx` — breadcrumb, busca (Ctrl+K), user menu
- [x] Criar `src/components/layout/mobile-nav.tsx` — bottom navigation (5 itens)
- [x] Criar `src/components/layout/breadcrumb.tsx` — automatico baseado na rota
- [x] Criar `src/app/(dashboard)/template.tsx` — animacao de transicao de pagina (Framer Motion)
- [x] Criar `src/app/(dashboard)/page.tsx` — placeholder Dashboard

---

## Fase 4 — Componentes Compartilhados ✅
**Esforco:** Medio | **Dependencias:** Fase 1

- [x] Criar `src/components/shared/money-input.tsx` (formatacao BR, focus/blur, currency.js)
- [x] Criar `src/components/shared/color-picker.tsx` (16 cores + hex custom, Popover)
- [x] Criar `src/components/shared/period-selector.tsx` (presets + date range picker)
- [x] Criar `src/components/shared/confirm-dialog.tsx` (substitui confirm() nativo)
- [x] Criar `src/components/shared/empty-state.tsx` (icone, titulo, descricao, action)
- [x] Criar `src/components/shared/stat-card.tsx` (metrica com icone, tendencia, variante)
- [x] Criar `src/components/shared/page-header.tsx` (titulo, descricao, slot de acoes)
- [x] Criar `src/components/shared/loading-skeleton.tsx` (skeletons por tipo de pagina)
- [x] Criar `src/components/shared/date-range-picker.tsx` (calendario duplo)

---

## Fase 5 — Componentes de Graficos ✅
**Esforco:** Medio | **Dependencias:** Fase 1

- [x] Criar `src/components/charts/pie-chart.tsx` (Donut com centro customizavel)
- [x] Criar `src/components/charts/bar-chart.tsx` (horizontal/vertical, cores custom)
- [x] Criar `src/components/charts/line-chart.tsx` (multiplas series)
- [x] Criar `src/components/charts/area-chart.tsx` (gradiente de preenchimento)
- [x] Criar `src/components/charts/radial-chart.tsx` (gauge circular 0-100%)
- [x] Criar `src/components/charts/chart-tooltip.tsx` (tooltip custom para Recharts)

---

## Fase 6 — Paginas Simples (Onboarding, Categorias, Account Deleted) ✅
**Esforco:** Baixo | **Dependencias:** Fases 3, 4

- [x] Criar `src/hooks/use-categories.ts` (useCategories, useCreateCategory, useUpdateCategory, useDeleteCategory)
- [x] Criar `src/components/shared/` category card e form
- [x] Criar pagina Categorias (`src/app/(dashboard)/categories/page.tsx`) — tabs + grid + form dialog
- [x] Criar pagina Onboarding (`src/app/(dashboard)/onboarding/page.tsx`) — cards animados
- [x] Criar pagina AccountDeleted (`src/app/(auth)/account-deleted/page.tsx`) — confirmacao LGPD

---

## Fase 7 — Contas ✅
**Esforco:** Medio-Alto | **Dependencias:** Fases 4, 5

- [x] Criar `src/hooks/use-accounts.ts` (useAccounts, useCreateAccount, useUpdateAccount, useDeleteAccount)
- [x] Criar `src/components/accounts/account-card.tsx` (com secao CC, barra de progresso)
- [x] Criar `src/components/accounts/account-form.tsx` (Sheet lateral, campos condicionais CC)
- [x] Criar `src/components/accounts/invoice-payment-modal.tsx` (selecao de fatura, conta de debito)
- [x] Criar pagina Contas (`src/app/(dashboard)/accounts/page.tsx`) — grid + ordenacao persistida

---

## Fase 8 — Transacoes ✅
**Esforco:** Alto | **Dependencias:** Fases 6, 7

- [x] Criar `src/hooks/use-transactions.ts` (CRUD + installment purchase + optimistic delete)
- [x] Criar `src/components/transactions/transaction-table.tsx` (responsiva, mobile = cards)
- [x] Criar `src/components/transactions/transaction-filters.tsx` (URL state via nuqs)
- [x] Criar `src/components/transactions/transaction-form.tsx` (Dialog, tipo segmentado, parcelamento)
- [x] Criar `src/components/transactions/installment-modal.tsx` (confirmacao parcelamento)
- [x] Criar pagina Transacoes (`src/app/(dashboard)/transactions/page.tsx`) — filtros + tabela + paginacao

---

## Fase 9 — Transacoes Recorrentes ✅
**Esforco:** Medio | **Dependencias:** Fase 7

- [x] Criar `src/hooks/use-recurring.ts` (CRUD)
- [x] Criar componentes de card e form para recorrentes
- [x] Criar pagina Recorrentes (`src/app/(dashboard)/recurring/page.tsx`) — stat cards + grid

---

## Fase 10 — Orcamentos ✅
**Esforco:** Medio-Alto | **Dependencias:** Fases 6, 4

- [x] Criar `src/hooks/use-budgets.ts` (useBudget por mes, create, update)
- [x] Criar `src/components/budgets/budget-card.tsx` (progresso com limiares de cor)
- [x] Criar `src/components/budgets/budget-wizard.tsx` (3 etapas, animacao entre etapas)
- [x] Criar `src/components/budgets/budget-progress.tsx` (barra com verde/amarelo/vermelho)
- [x] Criar pagina Orcamentos (`src/app/(dashboard)/budgets/page.tsx`) — seletor de mes + grid + wizard

---

## Fase 11 — Dashboard ✅
**Esforco:** Alto | **Dependencias:** Fases 5, 7, 8, 10

- [x] Criar `src/hooks/use-dashboard.ts` (agrega accounts, transactions, categories, budgets)
- [x] Criar `src/components/dashboard/overview-cards.tsx` (4 StatCards com tendencia)
- [x] Criar `src/components/dashboard/income-expense-chart.tsx` (AreaChart)
- [x] Criar `src/components/dashboard/budget-usage-chart.tsx` (DonutChart categorias)
- [x] Criar `src/components/dashboard/accounts-chart.tsx` (BarChart saldos)
- [x] Criar `src/components/dashboard/recent-transactions.tsx` (tabela compacta, 10 itens)
- [x] Criar `src/components/dashboard/credit-card-summary.tsx` (cards horizontais com gauge)
- [x] Criar pagina Dashboard (`src/app/(dashboard)/page.tsx`) — grid responsivo completo

---

## Fase 12 — Relatorios ✅
**Esforco:** Medio | **Dependencias:** Fases 5, 4

- [x] Criar `src/hooks/use-reports.ts` (carrega transactions + calcula client-side)
- [x] Criar pagina Relatorios (`src/app/(dashboard)/reports/page.tsx`) — presets, stat cards, graficos, tabela

---

## Fase 13 — Cartoes de Credito e Faturas ✅
**Esforco:** Alto | **Dependencias:** Fases 5, 7

- [x] Criar `src/hooks/use-invoices.ts` (open, overdue, history, pay, pay-partial, summary, transactions)
- [x] Criar `src/components/credit-cards/invoice-card.tsx`
- [x] Criar `src/components/credit-cards/invoice-status-badge.tsx`
- [x] Criar `src/components/credit-cards/credit-limit-gauge.tsx` (RadialChart)
- [x] Criar pagina CreditCardDashboard (`src/app/(dashboard)/credit-cards/[accountId]/page.tsx`)
- [x] Criar pagina InvoiceDetails (`src/app/(dashboard)/invoices/[invoiceId]/page.tsx`)

---

## Fase 14 — Perfil + Configuracoes + LGPD ✅
**Esforco:** Medio | **Dependencias:** Fases 3, 4

- [x] Criar `src/hooks/use-profile.ts` (get, update, changePassword, updateEmail)
- [x] Criar `src/hooks/use-settings.ts` (get, update)
- [x] Criar `src/stores/settings-store.ts` (Zustand: tema, idioma, moeda client)
- [x] Criar pagina Perfil (`src/app/(dashboard)/profile/page.tsx`) — info, senha, email, LGPD
- [x] Criar pagina Configuracoes (`src/app/(dashboard)/settings/page.tsx`) — financeiro, push, aparencia

---

## Fase 15 — Polish, PWA, i18n e Deploy ✅
**Esforco:** Medio | **Dependencias:** Todas as fases anteriores

- [x] Configurar animacoes globais (transicoes de pagina, stagger em listas, numeros animados)
- [x] Configurar PWA com serwist (service worker, manifest, cache strategy)
- [x] Configurar i18n com next-intl (pt-BR completo, estruturas en-US/es-ES/fr-FR)
- [x] Ajustar tema dark (testar todas as paginas em dark mode)
- [x] Criar `Dockerfile` para producao (multi-stage, standalone output)
- [x] Atualizar `railway.web.toml` para o novo frontend
- [x] Testar integracao completa com a API backend
- [x] Revisar responsividade em mobile

---

## Resumo de Dependencias

```
Fase 0  (Scaffolding)
  └─> Fase 1  (Foundation)
        ├─> Fase 2  (Auth) ──> Fase 3  (Layout)
        ├─> Fase 4  (Shared Components)
        └─> Fase 5  (Charts)

Fase 3 + 4 ──> Fase 6  (Categorias, Onboarding)
Fase 4 + 5 ──> Fase 7  (Contas)
Fase 6 + 7 ──> Fase 8  (Transacoes)
Fase 7     ──> Fase 9  (Recorrentes)
Fase 6 + 4 ──> Fase 10 (Orcamentos)
Fase 5+7+8+10 ──> Fase 11 (Dashboard)
Fase 5 + 4 ──> Fase 12 (Relatorios)
Fase 5 + 7 ──> Fase 13 (Cartoes de Credito)
Fase 3 + 4 ──> Fase 14 (Perfil + Configuracoes)
Todas      ──> Fase 15 (Polish + Deploy)
```

---

## Estimativa por Fase

| Fase | Descricao | Complexidade | Arquivos ~= |
|---|---|---|---|
| 0 | Scaffolding | Baixo | 5 |
| 1 | Foundation | Baixo-Medio | 15 |
| 2 | Auth | Medio | 8 |
| 3 | Layout | Medio | 7 |
| 4 | Shared Components | Medio | 9 |
| 5 | Charts | Medio | 6 |
| 6 | Paginas Simples | Baixo | 5 |
| 7 | Contas | Medio-Alto | 5 |
| 8 | Transacoes | Alto | 6 |
| 9 | Recorrentes | Medio | 4 |
| 10 | Orcamentos | Medio-Alto | 5 |
| 11 | Dashboard | Alto | 8 |
| 12 | Relatorios | Medio | 2 |
| 13 | Cartoes de Credito | Alto | 6 |
| 14 | Perfil + Config | Medio | 5 |
| 15 | Polish + Deploy | Medio | 8 |
| **Total** | | | **~104 arquivos** |

---

## Plano de Correcao — Faturas e Limite de Cartao

### Objetivo

Equalizar a regra de negocio e a implementacao tecnica para que:

- o total da fatura bata com a soma das transacoes elegiveis daquela fatura;
- o pagamento de fatura atualize fatura, saldo e limite de forma consistente;
- compras parceladas comprometam o limite total no momento da compra;
- a liberacao do limite aconteca gradualmente conforme o pagamento das faturas.

### Diagnostico Consolidado

- O total persistido da fatura pode ficar defasado em relacao as transacoes da fatura por ordem incorreta de recalculo durante update de transacao.
- O fluxo de pagamento de fatura no frontend Next.js e no backend nao fecha o ciclo contabil completo.
- O dominio atual usa `Account.Balance` para representar divida do cartao, mas nao possui um conceito persistido separado para limite comprometido/limite disponivel.
- O parcelamento atual nao reserva o valor total da compra no limite desde a origem; ele materializa apenas parcelas ao longo do tempo.

### Decisao de Modelagem

Adotar modelo persistido para cartao de credito, separando explicitamente:

- `Balance`: divida contabil acumulada do cartao;
- `CreditLimit`: limite total configurado;
- `CommittedCredit`: limite atualmente comprometido por compras ja realizadas, incluindo parcelas futuras ainda nao faturadas;
- `AvailableCredit`: limite disponivel derivado ou persistido a partir de `CreditLimit - CommittedCredit`.

### Regras de Negocio Alvo

1. Compra a vista no cartao:
   entra integralmente na fatura correspondente;
   consome integralmente o limite no momento da compra.

2. Compra parcelada no cartao:
   cada parcela entra apenas na fatura do seu periodo;
   o valor total da compra consome o limite no momento da compra.

3. Pagamento de fatura:
   reduz o saldo devido do cartao;
   libera limite gradualmente no montante efetivamente pago;
   nao altera retroativamente o valor historico das parcelas/compras.

4. Total da fatura:
   deve refletir apenas transacoes de compra/despesa elegiveis para aquela fatura;
   deve bater com a soma das transacoes exibidas na tela de detalhe.

### Ordem de Execucao

#### Etapa 1 — Testes de Regressao e Contrato

- Criar testes para `TransactionService.UpdateAsync` cobrindo alteracao de valor/data/conta/fatura.
- Criar testes para `CreditCardInvoiceService.RecalculateInvoiceTotalAsync` e `GetInvoiceTransactionsAsync` com a mesma regra de composicao.
- Criar testes para pagamento total/parcial validando reflexo em fatura, conta pagadora, cartao e limite comprometido.
- Criar testes para compra parcelada validando reserva imediata do limite total e apropriacao mensal das parcelas.

#### Etapa 2 — Equalizacao de Faturas

- Corrigir a ordem de persistencia e recalculo em `TransactionService.UpdateAsync`.
- Centralizar a logica de sincronizacao de fatura em um fluxo unico pos-persistencia.
- Garantir que listagem de transacoes da fatura e total da fatura usem a mesma regra de elegibilidade.

#### Etapa 3 — Fluxo Completo de Pagamento

- Corrigir o contrato frontend/backend de pagamento (`sourceAccountId` x `PayFromAccountId`).
- Tornar o pagamento atomico no backend, incluindo:
  atualizacao da fatura;
  movimentacao financeira entre conta pagadora e cartao;
  atualizacao do limite comprometido.
- Eliminar dependencia de logica compensatoria no frontend/Blazor legado.

#### Etapa 4 — Modelo Persistido de Limite

- Evoluir `Account` para armazenar `CommittedCredit` e campo derivado/persistido de disponivel conforme decisoes de implementacao.
- Atualizar DTOs, validadores, mapeamentos e hooks para expor corretamente limite total, utilizado e disponivel.
- Ajustar a validacao de limite no backend para usar `CommittedCredit`, e nao apenas `Balance`.

#### Etapa 5 — Parcelamento Real no Backend Next.js

- Implementar fluxo explicito de compra parcelada no backend.
- Criar a compra parcelada como operacao de dominio unica, nao como efeito distribuido no frontend.
- Registrar o total da compra no limite comprometido imediatamente.
- Registrar cada parcela no calendario/fatura correspondente, respeitando `firstInstallmentInCurrentInvoice`.

#### Etapa 6 — Reconciliacao de Dados Existentes

- Criar rotina de reconciliacao para recalcular totais de fatura a partir das transacoes persistidas.
- Criar rotina para recomputar `CommittedCredit` dos cartoes com base em compras e pagamentos historicos.
- Executar reconciliacao antes de expor a nova logica em producao.

#### Etapa 7 — Ajustes de UI e Comunicacao

- Atualizar dashboard e paginas de cartao/fatura para exibir separadamente:
  fatura atual;
  saldo/debito do cartao;
  limite total;
  limite comprometido;
  limite disponivel.
- Revisar nomenclatura visual para evitar ambiguidade entre "saldo do cartao" e "fatura atual".

Status em 2026-04-01:

- concluido no frontend Next.js com cards/alertas explicativos nas telas de cartao e fatura;
- concluido com ajuste de nomenclatura visual para priorizar "limite comprometido" e "limite disponivel";
- concluido com acao manual de reconciliacao em Configuracoes para suporte operacional.

### Impacto Tecnico Esperado

- Backend:
  `Account`, `TransactionService`, `CreditCardInvoiceService`, DTOs, validadores, controller de transacoes e pagamentos.
- Frontend:
  hooks de invoices/transacoes/contas, dashboard de cartao, detalhe de fatura, cards e gauges de limite.
- Dados:
  necessidade de rotina de migracao/reconciliacao para usuarios que ja possuem cartoes e faturas cadastrados.

### Riscos e Cuidados

- Nao misturar conceito de fatura com conceito de limite utilizado.
- Nao liberar limite no fechamento da fatura; a liberacao deve ocorrer no pagamento.
- Evitar que transferencias/pagamentos entrem na soma de compras da fatura.
- Garantir idempotencia no fluxo de pagamento e no fluxo de parcelamento.

### Criterios de Aceite

- O total da fatura e a soma das transacoes exibidas na fatura devem ser identicos.
- Compra a vista deve consumir limite total e aparecer integralmente na fatura correta.
- Compra parcelada deve consumir limite total imediatamente e distribuir parcelas entre faturas futuras.
- Pagamento parcial deve liberar limite apenas no valor pago.
- Pagamento total deve zerar a fatura e liberar integralmente o limite correspondente.
- Dashboard e telas de cartao devem exibir numeros coerentes entre si, sem ambiguidade semantica.
