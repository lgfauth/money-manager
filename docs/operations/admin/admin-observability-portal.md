# Portal Administrativo de Observabilidade

## Objetivo

Este documento descreve os pontos mais importantes para observabilidade e operacao do MoneyManager via portal administrativo.

A proposta e separar claramente:

1. O que ja pode ser observado e executado no sistema atual.
2. O que vale adicionar para aumentar confiabilidade, seguranca e velocidade de suporte.

## Principios do Portal Administrativo

1. Visibilidade antes de automacao: toda acao administrativa deve ter metricas e historico.
2. Seguranca por padrao: acoes de risco alto exigem confirmacao forte e auditoria.
3. Operacoes idempotentes: reexecucoes nao podem corromper dados.
4. UX operacional: erros devem ser explicaveis e acoplados a runbooks.
5. Escopo por impacto: separar acoes por usuario, por servico e por sistema.

## Estado Atual do Projeto (Resumo)

### Ja existe hoje

1. Endpoint de health da API (`/health`).
2. Logging estruturado por requisicao na API via `RequestLoggingMiddleware` + `IProcessLogger`.
3. Logger de processos para jobs do Worker (`RecurringTransactions`, `InvoiceClosure`, `DailyReminder`).
4. Endpoints administrativos na API para manutencao de cartoes/faturas:
   - `POST /api/admin/reconcile-credit-cards`
   - `POST /api/admin/recalculate-invoices`
   - `POST /api/admin/create-missing-open-invoices`
   - `POST /api/admin/migrate-credit-card-invoices`
5. Acao de reconciliacao ja integrada no frontend Next.js (tela de settings).
6. Swagger habilitado para apoio operacional.

### Lacunas atuais relevantes

1. Nao existe dashboard administrativo consolidado.
2. Nao existe endpoint dedicado de status do Worker (jobs, ultimo sucesso, ultima falha).
3. Nao existe trilha de auditoria persistente para acao administrativa sensivel.
4. Nao existe controle remoto de jobs (pausar, retomar, executar sob demanda).
5. Nao existe catalogo de metricas de negocio para reconciliacao e qualidade de dados.
6. Config de log ainda basica no NLog (sem pipeline completo de correlacao e analise centralizada).

## O que observar no Portal Administrativo

## 1. Saude do Sistema

### Observar

1. API health (status, timestamp, versao, ambiente).
2. Latencia P50/P95/P99 por endpoint.
3. Taxa de erro por endpoint e por classe HTTP (4xx/5xx).
4. Disponibilidade de MongoDB e tempo de resposta medio.

### Ajustar/Acionar

1. Teste de conectividade de dependencias (MongoDB e push provider).
2. Modo manutencao da API (somente leitura para operacoes criticas).

## 2. Qualidade de Dados Financeiros

### Observar

1. Divergencia entre saldo de cartao, comprometido e fatura aberta por conta.
2. Quantidade de transacoes de cartao sem `InvoiceId`.
3. Faturas com total inconsistente (soma de transacoes != total fatura).
4. Faturas abertas com periodo defasado.
5. Evolucao diaria de itens reconciliados.

### Ajustar/Acionar

1. Reconciliar cartoes (`reconcile-credit-cards`).
2. Recalcular faturas (`recalculate-invoices`).
3. Criar faturas abertas faltantes (`create-missing-open-invoices`).
4. Migrar historico legado com isolamento por usuario (`migrate-credit-card-invoices`).

## 3. Jobs do Worker

### Observar

1. Ultima execucao por job (`ScheduledTransactionWorker`, `InvoiceClosureWorker`, `DailyReminderWorker`).
2. Duracao de execucao e taxa de sucesso/falha.
3. Quantidade processada por job (ex: recorrencias processadas por usuario).
4. Falhas de timeout e cancelamento.
5. Backlog estimado (itens pendentes para processar).

### Ajustar/Acionar

1. Executar job sob demanda (run now).
2. Pausar/retomar job por tipo.
3. Alterar janela de agendamento (hour/minute/timezone/loop delay).
4. Alterar timeout operacional de execucao.
5. Reprocessar janela especifica (ex: ultimo dia, ultima semana).

## 4. Push Notifications

### Observar

1. Taxa de entrega e falha por tipo de push.
2. Quantidade de subscriptions ativas por usuario.
3. Erros por provider/chave invalida.

### Ajustar/Acionar

1. Envio de notificacao de teste por usuario.
2. Reenvio controlado para falhas transitorias.
3. Revogacao/limpeza de subscriptions invalidas.

## 5. Seguranca e Conformidade

### Observar

1. Tentativas de acesso administrativo negadas.
2. Origens CORS aceitas e rejeitadas.
3. Acoes administrativas por usuario operador.
4. Alteracoes de configuracao de ambiente sensivel.

### Ajustar/Acionar

1. Bloquear temporariamente usuario operador.
2. Rotacionar modo de permissao (somente leitura vs operador).
3. Exigir aprovacao em duas etapas para acoes destrutivas.

## O que o Portal deve permitir fazer (Matriz Operacional)

| Categoria | Observar | Fazer agora | Evoluir |
|---|---|---|---|
| Cartoes/Faturas | Inconsistencias por conta | Reconciliar, recalcular, criar faltantes | Playbooks automaticos com thresholds |
| Worker | Ultimo run, duracao, falhas | Run now manual (via endpoint novo) | Pausar/retomar e reprocessamento por janela |
| API | Health, latencia, erros | Health check e consulta de logs | SLOs, alertas e rollback operacional |
| Push | Entregas/falhas | Teste de push por usuario | Retry policy central + quarentena de tokens |
| Seguranca | Acessos admin e trilha | Controle basico de autorizacao | Auditoria assinada e aprovacao dupla |

## Sugestoes de Endpoints Administrativos Novos

## 1. Status e Telemetria

1. `GET /api/admin/system-status`
   - Retorna saude consolidada (API, MongoDB, Worker, push).
2. `GET /api/admin/metrics/summary`
   - KPIs de erros, latencia e jobs nas ultimas 24h.
3. `GET /api/admin/metrics/credit-cards`
   - Indicadores de reconciliacao e inconsistencias de cartao.

## 2. Controle de Jobs

1. `POST /api/admin/jobs/{jobName}/run-now`
2. `POST /api/admin/jobs/{jobName}/pause`
3. `POST /api/admin/jobs/{jobName}/resume`
4. `PUT /api/admin/jobs/{jobName}/schedule`
5. `GET /api/admin/jobs/{jobName}/history`

## 3. Governanca de Acoes Sensiveis

1. `GET /api/admin/audit/actions`
2. `POST /api/admin/audit/approve`
3. `POST /api/admin/maintenance/read-only-mode`

## Recomendacoes de Implementacao por Fase

## Fase 1 (rapida, alto valor)

1. Criar pagina "Observabilidade" no portal admin com:
   - health da API
   - status dos jobs
   - resumo de erros recentes
2. Expor `system-status` e `jobs history` na API.
3. Salvar cada acao administrativa em trilha de auditoria persistida.

## Fase 2 (operacao ativa)

1. Adicionar comandos `run-now` por job.
2. Adicionar ajuste de schedule/timeout via portal.
3. Adicionar painel de qualidade de dados de cartao (inconsistencias + acao rapida).

## Fase 3 (confiabilidade e compliance)

1. SLOs com alertas (P95, erro 5xx, falha de job).
2. Aprovacao dupla para acoes criticas.
3. Relatorio mensal de auditoria operacional.

## O que iniciar, parar, ajustar ou evitar

### Iniciar

1. Medicao formal de KPIs operacionais e de dados financeiros.
2. Auditoria persistente de toda acao administrativa.
3. Endpoints administrativos para status consolidado de jobs.

### Parar

1. Operacoes manuais sem trilha de auditoria.
2. Ajustes de schedule apenas por arquivo/config sem validacao em runtime.
3. Decisoes operacionais baseadas apenas em log textual solto.

### Ajustar

1. NLog para layout estruturado com correlacao e filtro por ambiente.
2. CORS e logs de seguranca com visao no portal.
3. Exposicao de status do Worker para alem de logs de console.

### Evitar

1. Permitir acoes destrutivas sem confirmacao forte.
2. Misturar operacao de emergencia com configuracao permanente.
3. Reconciliacao automatica sem observabilidade dos resultados.

## KPIs Minimos do Portal Admin

1. API uptime em 24h e 7d.
2. Taxa de erro 5xx por endpoint.
3. Tempo medio/P95 de endpoints criticos.
4. Jobs: sucesso, falha, duracao, backlog.
5. Cartoes: contas com divergencia, faturas inconsistentes, itens reconciliados.
6. Push: envio, entrega estimada, falha.
7. Acoes admin por operador (quem, quando, o que, resultado).

## Riscos de produto se nao fizer

1. Incidentes de dados financeiros demorando para detectar.
2. Correcao manual repetitiva sem padrao.
3. Dificuldade para auditoria e investigacao de falhas.
4. Alto MTTR em eventos de fechamento de fatura e recorrencias.

## Resultado esperado com o portal administrativo

1. Menor tempo para detectar inconsistencias financeiras.
2. Menor tempo para corrigir incidentes operacionais.
3. Maior previsibilidade de jobs diarios.
4. Melhor rastreabilidade e governanca para operacoes sensiveis.
5. Base pronta para observabilidade madura (SLO, alerta, auditoria e automacao segura).
