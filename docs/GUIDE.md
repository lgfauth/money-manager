# BankConnection — Integração Banco MCP

## Resumo

A implementação existente (baseada em API key por usuário) foi migrada para o modelo
multi-tenant definido no design original: uma única API key de workspace
(`BancoMcp:ApiKey`) + convites de guest via Management API do Banco MCP
(`IBankMcpManagementClient`, `GET /api/bank-connections/invite`). O fluxo por API key
por usuário (`SaveBankMcpApiKeyRequestDto`, `User.BankMcpApiKey`, `IEncryptionService`)
foi removido do backend e do wizard do frontend, substituído por um passo de convite
(`StepInvite`) que abre a URL do Banco MCP e retoma o onboarding ao voltar.

Também foram corrigidas divergências técnicas: `BankMcpClient` agora usa exclusivamente
POST e desembrulha o envelope `{ ok, result }`; valores monetários são parseados de
string com `CultureInfo.InvariantCulture`; a sincronização de transações pagina em
lotes de 500 até `page >= totalPages`; `SelectedBankAccount` ganhou `bankName`,
`subtype` e `number`; `BankConnection` ganhou `ConnectorId` e `MarkError()`; e foram
adicionados índices MongoDB para `bank_connections` (userId+isDeleted,
externalConnectionId, status+isDeleted) e para dedup de transações
(userId+externalId, único e esparso).

Solução .NET compila (`dotnet build`) e o frontend (`MoneyManager.Web`) passa em
`tsc --noEmit`.

---

## Critérios de aceite
- [x] `GET /api/bank-connections/invite` retorna `ConnectUrl` único por usuário (gerado via Management API) e bloqueia com 403 se não for premium.
- [x] `GET /api/bank-connections/available` lista conexões do workspace com `alreadyRegistered` correto.
- [x] `POST /api/bank-connections` valida status no Banco MCP antes de persistir — rejeita `LOGIN_ERROR`.
- [x] `GET /api/bank-connections/{id}/accounts` usa campo `bank` (não `name`) como `displayName`.
- [x] `balance` e `amount` são parseados de string com `CultureInfo.InvariantCulture` — nunca desserializado direto como `decimal`.
- [x] Transações: `id` do Banco MCP é persistido como `ExternalId` — upsert nunca cria duplicata.
- [x] `accountId` é repassado do parâmetro do request pra cada `BankMcpTransaction` (não vem no body).
- [x] `SyncConnectionAsync` pagina com `page_size: 500` até `page >= totalPages`.
- [x] Worker roda nas horas 2, 9, 14, 20 (horário de Brasília) — nunca mais de uma vez por hora.
- [x] `DisconnectAsync` faz soft delete local mesmo se a revogação no Banco MCP falhar (log de warning + continua).
- [ ] `BankMcpManagementClient.InviteRaw` — confirmar nomes dos campos JSON contra response real da Management API após primeiro teste com toolkit real.
- [x] Todos os endpoints do `BankMcpClient` usam POST. Envelope `{ ok, result }` sempre desembrulhado antes de desserializar.
- [x] Nenhuma lógica de negócio nos controllers. Logs e comentários em português.
