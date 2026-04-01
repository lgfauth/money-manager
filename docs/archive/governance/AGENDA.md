# AGENDA — MoneyManager

> Catálogo de boas práticas, tarefas permitidas, tarefas proibidas e itens de melhoria futura.
> Consulte este documento antes de iniciar qualquer mudança relevante na solução.

---

## Sumário

1. [O que DEVE ser feito](#1-o-que-deve-ser-feito)
2. [O que NÃO DEVE ser feito](#2-o-que-não-deve-ser-feito)
3. [Melhorias Pendentes (Backlog Técnico)](#3-melhorias-pendentes-backlog-técnico)
4. [Riscos Conhecidos a Mitigar](#4-riscos-conhecidos-a-mitigar)
5. [Checklist de Pull Request](#5-checklist-de-pull-request)

---

## 1. O que DEVE ser feito

### Arquitetura e Código

- [x] Manter separação estrita de camadas: `Domain` ? `Application` ? `Infrastructure/Presentation/Worker`
- [x] Toda lógica de negócio fica em `Application` — controllers e workers apenas orquestram
- [x] Toda entidade nova segue o padrão: `Id`, `UserId`, `CreatedAt`, `UpdatedAt`, `IsDeleted`
- [x] Toda exclusão usa soft delete (`IsDeleted = true`)
- [x] Toda query filtra por `userId` E `!isDeleted`
- [x] Serviços novos: interface + implementação no mesmo arquivo
- [x] Serviços novos: registrados como `Scoped` no container de DI
- [x] DTOs separados para entrada (`Request`) e saída (`Response`)
- [x] Validação de entrada com FluentValidation para endpoints que recebem body
- [x] Logging estruturado com placeholders — `{NomeDoParametro}` — nunca interpolação de string

### Testes

- [x] Adicionar ou atualizar testes ao modificar comportamento de serviços da camada Application
- [x] Seguir padrão `MethodName_Scenario_ExpectedResult` na nomenclatura
- [x] Seguir estrutura AAA (Arrange / Act / Assert) em todos os testes
- [x] Mockar todas as dependências externas com NSubstitute — sem acesso a banco real em testes
- [x] Testar: sucesso, entidade não encontrada, violação de regra, isolamento por usuário

### Frontend (Blazor)

- [x] Toda chamada HTTP feita através de serviço em `Services/` — nunca `HttpClient` direto em `.razor`
- [x] Toda string visível ao usuário deve usar chave de localização em `wwwroot/i18n/pt-BR.json`
- [x] Toda nova página deve ter tratamento explícito de estado: carregando, vazio, erro
- [x] Componentes reutilizados em 2+ páginas devem ir para `Shared/` ou `Components/`

### Worker

- [x] Todo job de background deve ser idempotente
- [x] Lógica de negócio em `*Processor` — nunca diretamente no `*Worker` (hosted service)
- [x] Usar `ITimeProvider` para abstrair tempo (testabilidade)
- [x] Usar `CancellationToken` em todos os loops e operações longas
- [x] Configurar timeout por execução via `WorkerOptions.ExecutionTimeoutMinutes`

### Documentação

- [x] Atualizar `docs/ARCHITECTURE-GUIDE.md` ao mudar estrutura de projeto ou camadas
- [x] Atualizar o guia de desenvolvimento relevante (`api-development-guide.md`, `web-development-guide.md`, `worker-development-guide.md`) ao mudar fluxo de desenvolvimento
- [x] Registrar incidentes recorrentes em `docs/troubleshooting/`
- [x] Registrar features concluídas em `docs/history/features/`

### MongoDB

- [x] Criar índices para campos usados em filtros frequentes (especialmente `userId`)
- [x] Adicionar `[BsonIgnoreExtraElements]` em todas as entidades para tolerância a schema evolution
- [x] Manter `[BsonElement("camelCaseName")]` em todos os campos mapeados

---

## 2. O que NÃO DEVE ser feito

### Arquitetura

- ? **Não colocar lógica de negócio em controllers** — controllers são thin; lógica fica em `Application`
- ? **Não referenciar `Infrastructure` ou `MongoContext` diretamente em `Application`** — usar apenas `IUnitOfWork`
- ? **Não referenciar `Presentation` ou `Web` em `Application` ou `Domain`**
- ? **Não criar dependência circular entre projetos**
- ? **Não adicionar atributos HTTP ou de UI em entidades do `Domain`**
- ? **Não acessar `HttpClient` diretamente em páginas `.razor`** — usar serviços
- ? **Não usar `AutoMapper` ou outro mapeador automático sem consenso** — o projeto usa mapeamento manual explícito
- ? **Não adicionar novos pacotes NuGet sem avaliar necessidade** — preferir bibliotecas já presentes

### Banco de Dados

- ? **Não deletar documentos fisicamente em produção** — sempre usar soft delete (`IsDeleted = true`)
- ? **Não fazer queries sem filtrar por `userId`** — risco de vazamento de dados entre usuários
- ? **Não criar collections sem índice para `userId`** — performance degradada
- ? **Não remover o `[BsonIgnoreExtraElements]`** — quebra desserialização ao evoluir o schema

### Segurança

- ? **Não colocar secrets em `appsettings.json` no repositório** — usar variáveis de ambiente
- ? **Não retornar `PasswordHash` em nenhum DTO de resposta**
- ? **Não remover a validação de `userId` no serviço** — toda operação valida que o recurso pertence ao usuário autenticado
- ? **Não tornar endpoints de negócio públicos** sem justificativa explícita (todos devem ter `[Authorize]`)

### Testes

- ? **Não escrever testes que dependem de banco de dados real**
- ? **Não ignorar testes com `[Skip]` permanentemente** — corrigir ou remover
- ? **Não testar detalhes de implementação** — testar comportamento observável
- ? **Não commitar com testes falhando**

### Frontend

- ? **Não hardcodar textos visíveis ao usuário em `.razor`** — usar localização
- ? **Não fazer polling agressivo** (chamadas em loop sem delay) no frontend
- ? **Não armazenar dados sensíveis além do token no `LocalStorage`**

### Worker

- ? **Não colocar lógica de negócio complexa diretamente no `ExecuteAsync` do hosted service**
- ? **Não criar jobs que não sejam idempotentes** — re-execuções devem ser seguras
- ? **Não ignorar o `CancellationToken`** em loops do worker

---

## 3. Melhorias Pendentes (Backlog Técnico)

> Itens identificados como dívidas técnicas ou evoluções desejáveis. Priorize antes de adicionar novas features.

### Alta Prioridade

| # | Item | Projeto | Impacto |
|---|---|---|---|
| T-01 | Mover URL da API de hardcoded em `Program.cs` para variável de ambiente / configuração | `MoneyManager.Web` | Permite múltiplos ambientes sem rebuild |
| T-02 | Restringir CORS para origins conhecidos em produção | `MoneyManager.Presentation` | Segurança |
| T-03 | Mover filtro `!IsDeleted` e `userId` para o nível de repositório (filtro MongoDB) | `MoneyManager.Infrastructure` | Performance em coleções grandes |
| T-04 | Gerenciar JWT secret exclusivamente via variável de ambiente | `MoneyManager.Presentation` | Segurança |

### Média Prioridade

| # | Item | Projeto | Impacto |
|---|---|---|---|
| T-05 | Implementar refresh token (atualmente o JWT expira sem renovação automática) | `Application` + `Web` | UX |
| T-06 | Adicionar paginação nos endpoints que retornam listas | `Presentation` + `Application` | Performance |
| T-07 | Adicionar índice composto `userId + isDeleted` nas collections de alta frequência | `Infrastructure` | Performance |
| T-08 | Remover `Console.WriteLine` do `Program.cs` da API e substituir por logging estruturado | `Presentation` | Observabilidade |
| T-09 | Substituir `Console.WriteLine` no `LocalizationService` por `ILogger` | `MoneyManager.Web` | Observabilidade |

### Baixa Prioridade / Futuro

| # | Item | Projeto | Impacto |
|---|---|---|---|
| T-10 | Suporte a múltiplos idiomas no frontend (infraestrutura já existe) | `MoneyManager.Web` | Internacionalização |
| T-11 | Testes de integração para os controllers principais | `MoneyManager.Tests` | Qualidade |
| T-12 | Health check detalhado com status do MongoDB | `Presentation` | Observabilidade |
| T-13 | Implementar transações MongoDB multi-documento onde necessário | `Infrastructure` | Consistência |
| T-14 | Dashboard de investimentos (páginas e serviços parcialmente criados) | `MoneyManager.Web` | Feature |
| T-15 | Detectar idioma do browser automaticamente via JS Interop (TODO existente em `LocalizationService`) | `MoneyManager.Web` | UX |

---

## 4. Riscos Conhecidos a Mitigar

| # | Risco | Situação Atual | Ação Recomendada |
|---|---|---|---|
| R-01 | URL da API hardcoded | `MoneyManager.Web/Program.cs` linha 8 | Extrair para `appsettings.json` ou env var (T-01) |
| R-02 | CORS permissivo | Qualquer `Origin` aceito | Restringir para domínios conhecidos (T-02) |
| R-03 | Filtro de soft delete no serviço | Carrega todos os documentos e filtra em memória | Implementar filtro no repositório (T-03) |
| R-04 | JWT secret em appsettings | Configurado como fallback hardcoded | Garantir env var em produção (T-04) |
| R-05 | `SaveChangesAsync()` no-op | Sem suporte a transações multi-documento | Avaliar uso de sessões MongoDB (T-13) |
| R-06 | `Console.WriteLine` em produção | Vários arquivos | Substituir por logging estruturado (T-08, T-09) |

---

## 5. Checklist de Pull Request

Antes de submeter qualquer mudança, verificar:

### Código

- [ ] A mudança está na camada correta?
- [ ] A lógica de negócio está em `Application`, não no controller ou página?
- [ ] Toda nova entidade segue o padrão de campos obrigatórios?
- [ ] Toda query filtra `userId` e `!isDeleted`?
- [ ] Logging usa placeholders estruturados?

### Testes

- [ ] Testes adicionados ou atualizados para comportamentos modificados?
- [ ] Todos os testes passando (`dotnet test`)?
- [ ] Novos testes cobrem: sucesso, entidade não encontrada, isolamento por usuário?

### Frontend

- [ ] Textos novos adicionados em `pt-BR.json`?
- [ ] Chamadas HTTP feitas via serviço (não `HttpClient` direto em `.razor`)?
- [ ] Novo serviço registrado em `Program.cs`?

### API

- [ ] Novo endpoint tem `[Authorize]` onde necessário?
- [ ] Controller está thin (sem lógica de negócio)?
- [ ] Novo serviço registrado em `Program.cs` como `Scoped`?

### Worker

- [ ] Novo job é idempotente?
- [ ] Lógica de negócio está no `*Processor`, não no `*Worker`?
- [ ] Configuração adicionada em `appsettings.json`?

### Documentação

- [ ] `ARCHITECTURE-GUIDE.md` atualizado se a estrutura mudou?
- [ ] Guia de desenvolvimento relevante atualizado?
- [ ] `AGENDA.md` atualizado (novos riscos, itens resolvidos)?

---

*Última atualização: consulte o histórico Git para a data exata.*
