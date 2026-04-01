# Análise e Correção de Problemas no Frontend

## Categorização por Dificuldade

### Fase 1 — Fácil (Mudanças simples de configuração/URL)

| # | Problema | Causa Raiz | Correção |
|---|---------|------------|---------|
| 1 | Menu aberto ao logar | `sidebarOpen: true` no estado inicial do Zustand (`ui-store.ts`) faz o Sheet mobile abrir automaticamente | Alterar para `sidebarOpen: false` |
| 10 | Transações recorrentes retorna 404 | Frontend usa `/api/recurring-transactions` (kebab-case) mas o backend usa `/api/RecurringTransactions` (PascalCase via `[Route("api/[controller]")]`) | Alterar endpoint em `use-recurring.ts` |
| 9 | Erro 400 ao editar orçamento | Frontend envia `PUT /api/budgets/{id}` com ObjectId, mas o backend espera `PUT /api/Budgets/{month}` com string no formato YYYY-MM | Alterar para usar `month` em vez de `id` no `budget-wizard.tsx` |

### Fase 2 — Médio (Lógica de componentes)

| # | Problema | Causa Raiz | Correção |
|---|---------|------------|---------|
| 3 | Botão de busca não funciona | O botão Ctrl+K alterna `commandOpen` no store, mas nenhum `CommandDialog` é renderizado em lugar algum | Remover botão de busca do header (funcionalidade não implementada) |
| 6 | Botão limpar filtros não funciona | `onFiltersChange({})` chama `setFilters({page:1})` que faz merge com filtros existentes via spread, mantendo filtros antigos | Corrigir `clearFilters` para passar todos os campos como `undefined` explicitamente |
| 4 | Account type dropdown mostra inglês | `<Select defaultValue={...}>` (modo não-controlado) + `<SelectValue>` do base-ui exibe o valor raw (ex: `Checking`) em vez do label traduzido quando value != item label | Usar `value` controlado + `placeholder` com label traduzido |
| 7 | Filtro de contas mostra ID | `<SelectValue>` do base-ui pode mostrar o value (ID) em vez do texto do item quando os items ainda não estão montados/atualizados | Garantir que a renderização seja controlada e mostre o nome |

### Fase 3 — Complexo (Integração form/API)

| # | Problema | Causa Raiz | Correção |
|---|---------|------------|---------|
| 2 | Botão "?" no topo sem ação | O "?" é o fallback do Avatar quando `user.name` não está disponível. O DropdownMenu usa `<Link>` dentro de `<DropdownMenuItem>` que pode não propagar o click corretamente em base-ui | Corrigir navegação dos itens do dropdown e garantir que iniciais do usuário apareçam |
| 5 | Criar conta não funciona | O formulário usa `<Select defaultValue={...}>` (não-controlado) que pode não sincronizar com react-hook-form, causando falha silenciosa de validação | Converter Selects para modo controlado com `value` |
| 8 | Erro 400 ao criar transação | Backend faz check de concorrência (version) ao atualizar saldo da conta após criar transação. `ConcurrencyException` disparada no Repository.UpdateAsync | Adicionar `clientRequestId` (UUID idempotency) nas requests de criação |

## Status de Execução

- [x] Fase 1 — Implementada
- [x] Fase 2 — Implementada  
- [x] Fase 3 — Implementada

---

## Sessão 2 — Novos Problemas Identificados

| # | Problema | Causa Raiz | Arquivo | Correção |
|---|---------|------------|---------|---------|
| 1 | Dropdowns de conta/categoria no form de recorrências mostram ID | `SelectValue` do base-ui mostra o `value` raw (ID) quando não tem children render function | `recurring-form.tsx` | Adicionar render function children no `SelectValue` para resolver nome a partir da lista de contas/categorias |
| 2 | Tabela "Detalhamento por Categoria" em Relatórios mostra só cor | Backend `TransactionResponseDto` não retorna `categoryName`. Frontend usava `t.categoryName` (sempre `undefined`) | `use-reports.ts` | Construir `categoryNameMap` a partir de `useCategories()` e usar no lugar de `t.categoryName` |
| 3 | Gráfico de despesas por categoria (dashboard) sem nome | Mesmo problema — `t.categoryName` é `undefined` pois a API não retorna esse campo | `use-dashboard.ts` | Importar `useCategories`, construir `categoryNameMap`, usar na construção de `expensesByCategory` |
| 4 | Tooltip do gráfico donut mostra índice em vez do nome | Recharts PieChart tooltip `p.name` retorna índice numérico, não o campo `name` do data item | `pie-chart.tsx` | Usar `(p.payload as PieChartItem).name` em vez de `p.name` |
| 5 | Erro 400 "record modified by another process" ao criar transação | `Repository.UpdateAsync` filtra por `version == currentVersion`, mas documentos antigos no MongoDB não possuem campo `version` → filter não encontra → `ConcurrencyException` | `Repository.cs` | Alterar filtro para `(version == current OR version não existe)` para migrar documentos antigos |

### Status — Sessão 2

- [x] Fix #1 — Dropdowns de recorrências (recurring-form.tsx)
- [x] Fix #2 — Nomes de categoria em relatórios (use-reports.ts)
- [x] Fix #3 — Nomes de categoria no dashboard (use-dashboard.ts)
- [x] Fix #4 — Tooltip do gráfico donut (pie-chart.tsx)
- [x] Fix #5 — Concorrência no backend (Repository.cs)
