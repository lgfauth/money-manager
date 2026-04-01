# Plano Faseado - Portal Administrativo

## Escopo

Implementar:

1. Frontend administrativo em [src/MoneyManager.Administration](../../src/MoneyManager.Administration)
2. API administrativa em [src/MoneyManager.Api.Administration](../../src/MoneyManager.Api.Administration)

Objetivo: observabilidade operacional e controle administrativo seguro para o ecossistema MoneyManager.

## Ordem de Implementacao (do menos complexo para o mais complexo)

## Fase 0 - Fundacao e Bootstrap

Objetivo: preparar base de projeto sem regras de negocio complexas.

### Entregas

1. Estruturas de pasta e convencoes de nomenclatura.
2. Solucoes/projetos criados e referenciados na solution principal.
3. Pipeline de configuracao (appsettings, env vars, CORS, auth).
4. Health endpoints e contrato padrao de resposta.

### Checklist de atividades

- [x] Criar projeto frontend administrativo com stack equivalente ao frontend atual.
- [x] Criar projeto API administrativa em .NET.
- [ ] Adicionar ambos os projetos na [MoneyManager.sln](../../MoneyManager.sln).
- [x] Definir padrao de versionamento de API (v1).
- [ ] Definir contrato de erro e sucesso reutilizavel.
- [x] Configurar autenticacao/autorizacao base para rotas administrativas.
- [x] Configurar CORS para o dominio do portal admin.

## Fase 1 - Observabilidade somente leitura

Objetivo: dashboard inicial com dados de saude e status, sem acoes destrutivas.

### Entregas

1. Tela de status do sistema (API, MongoDB, worker).
2. Tela de status de jobs (ultima execucao, duracao, sucesso/falha).
3. Tela de metricas resumidas (erros, latencia, disponibilidade).

### Checklist de atividades

- [x] Criar endpoint `GET /api/admin/system-status`.
- [x] Criar endpoint `GET /api/admin/jobs/history`.
- [x] Criar endpoint `GET /api/admin/metrics/summary`.
- [x] Criar DTOs de leitura para metricas e status.
- [x] Implementar pagina "Visao Geral" no portal admin.
- [x] Implementar pagina "Jobs" no portal admin.
- [x] Implementar pagina "Erros e Latencia" no portal admin.

## Fase 2 - Operacao assistida (acoes de baixo risco)

Objetivo: habilitar operacoes administrativas existentes com trilha de auditoria.

### Entregas

1. Integracao segura das acoes de reconciliacao ja existentes.
2. Registro de auditoria para cada comando acionado.
3. Feedback operacional padronizado no frontend.

### Checklist de atividades

- [x] Expor no novo backend administrativo os comandos de cartao/fatura:
  - [x] `POST /api/admin/reconcile-credit-cards`
  - [x] `POST /api/admin/recalculate-invoices`
  - [x] `POST /api/admin/create-missing-open-invoices`
  - [x] `POST /api/admin/migrate-credit-card-invoices`
- [x] Criar endpoint `GET /api/admin/audit/actions`.
- [x] Persistir trilha de auditoria (quem, quando, acao, parametros, resultado).
- [x] Criar pagina "Manutencao Financeira" no portal.
- [x] Criar pagina "Auditoria" no portal.

## Fase 3 - Controle de jobs e agendamento

Objetivo: adicionar operacao em runtime dos jobs do worker.

### Entregas

1. Comandos run-now, pause, resume por job.
2. Alteracao de schedule em runtime com validacao.
3. Historico de execucoes por job.

### Checklist de atividades

- [x] Criar endpoint `POST /api/admin/jobs/{jobName}/run-now`.
- [x] Criar endpoint `POST /api/admin/jobs/{jobName}/pause`.
- [x] Criar endpoint `POST /api/admin/jobs/{jobName}/resume`.
- [x] Criar endpoint `PUT /api/admin/jobs/{jobName}/schedule`.
- [x] Criar endpoint `GET /api/admin/jobs/{jobName}/history`.
- [x] Criar tela "Controle de Jobs" com confirmacoes explicitas.
- [x] Adicionar guardrails para evitar execucao concorrente indevida.

## Fase 4 - Seguranca operacional e governanca

Objetivo: endurecer seguranca para acoes sensiveis e compliance.

### Entregas

1. RBAC administrativo (viewer/operator/admin).
2. Confirmacao forte para acoes destrutivas.
3. Aprovacao em duas etapas para comandos criticos.

### Checklist de atividades

- [x] Implementar perfis de acesso administrativos.
- [x] Bloquear endpoints sensiveis por role.
- [x] Exigir justificativa em comando critico.
- [ ] Implementar fluxo de aprovacao para acao de alto impacto.
- [x] Gerar relatorio mensal de auditoria.

## Fase 5 - Maturidade de observabilidade

Objetivo: padrao SLO/SLA, alertas e automacoes controladas.

### Entregas

1. Dashboard de SLO (latencia, erro, disponibilidade).
2. Alertas operacionais por threshold.
3. Playbooks semi-automaticos com confirmacao humana.

### Checklist de atividades

- [ ] Definir SLOs por endpoint critico e por job.
- [ ] Expor endpoint de SLO e compliance diario.
- [ ] Integrar alertas para incidentes principais.
- [ ] Criar playbook "detectar -> diagnosticar -> agir -> auditar".

## Checklist de pontos de atencao (nao pode falhar)

## Arquitetura e consistencia

- [x] Nao duplicar regra de negocio financeira entre APIs.
- [x] Reusar servicos de aplicacao existentes sempre que possivel.
- [x] Manter contratos de DTO estaveis e versionados.

## Seguranca

- [x] Todo endpoint administrativo com autenticacao obrigatoria.
- [x] Todo comando sensivel com autorizacao por role.
- [x] Nenhuma credencial em texto puro em codigo/repositorio.
- [x] Logs sem vazamento de dados sensiveis.

## Operacao

- [x] Toda acao administrativa deve ser auditavel.
- [x] Toda acao deve retornar resultado claro (sucesso parcial, total, falha).
- [x] Toda operacao deve ser idempotente quando possivel.
- [ ] Timeout e cancelamento devem ser tratados de forma explicita.

## UX do portal

- [x] Exibir impacto esperado antes de acao critica.
- [x] Exibir resultado detalhado apos execucao.
- [ ] Exibir orientacao de correcoes quando houver erro.

## Observabilidade

- [x] Correlation id em requisicoes e jobs.
- [x] Historico de eventos com filtro por periodo, usuario e acao.
- [x] Metricas minimas: disponibilidade, erro, latencia, backlog, reconciliacoes.

## Qualidade e entrega

- [ ] Testes de contrato para endpoints administrativos.
- [ ] Testes de autorizacao por role.
- [ ] Testes de regressao para operacoes financeiras sensiveis.
- [ ] Runbook operacional atualizado a cada fase entregue.

## Primeiros passos recomendados (execucao imediata)

1. Criar os dois projetos (Fase 0).
2. Publicar `system-status` e `jobs/history` (Fase 1).
3. Montar a primeira tela do portal com leitura apenas (Fase 1).
4. Integrar a primeira acao operacional: reconciliacao de cartoes (Fase 2).
