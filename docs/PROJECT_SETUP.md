# MoneyManager

Sistema de gerenciamento financeiro pessoal desenvolvido em **.NET 10 + MongoDB** no backend e **Next.js** no frontend, seguindo os princípios de **Clean Architecture**.

> Para uma visão técnica completa (arquitetura, aplicações, convenções e integrações), consulte `docs/instructions-for-development.md`.

## 📋 Estrutura do Projeto

```
money-manager/
├── src/
│   ├── APIs/
│   │   ├── MoneyManager.Api.Operational/   # API REST principal (usuários) — controllers, middlewares, Program.cs
│   │   └── MoneyManager.Api.Backoffice/    # API administrativa interna (operadores)
│   ├── Frontends/
│   │   ├── MoneyManager.Web/               # Frontend do usuário (Next.js 15)
│   │   └── MoneyManager.Backoffice/        # Portal de administração (Next.js 16)
│   ├── Supports/
│   │   ├── MoneyManager.Domain/            # Entidades, enums, interfaces, exceções, seeds
│   │   ├── MoneyManager.Application/       # Services, DTOs (Request/Response), Validators
│   │   ├── MoneyManager.Infrastructure/    # MongoDB, repositórios, JWT, criptografia, IA, gateways
│   │   └── MoneyManager.Observability/     # Logging estruturado de processos
│   └── Workers/
│       └── MoneyManager.Worker.Operational/ # Jobs agendados (recorrências, faturas, sync bancário etc.)
│
├── tests/
│   └── MoneyManager.Tests/                 # xUnit + NSubstitute (Domain/Application/Presentation)
│
├── Dockerfile.api / Dockerfile.admin-api / Dockerfile.worker
├── railway.*.toml                          # Configurações de deploy no Railway
├── dev.sh                                  # Sobe os ambientes de desenvolvimento
├── AGENTS.md / CLAUDE.md                   # Regras para pessoas e agentes de IA (checklist de entrega)
├── .githooks/                              # Hooks git versionados (verificação de changelog)
└── docs/                                   # Guias de desenvolvimento e deploy
```

## 🚀 Como Executar

### Pré-requisitos

- **.NET 10 SDK**
- **Node.js + npm**
- **MongoDB** acessível em `mongodb://localhost:27017` — por exemplo, via Docker:
  ```bash
  docker run -d --name mongodb -p 27017:27017 mongo:7
  ```

### Opção 1: Script único (`dev.sh`) — recomendado

Na raiz do repositório, `./dev.sh` sobe os cenários de desenvolvimento com um único comando, cuidando de API(s) .NET e frontend(s) Next.js juntos:

```bash
./dev.sh admin         # Ambiente administrativo: API Backoffice + Frontend Backoffice
./dev.sh operational   # Ambiente operativo: API Operacional + Frontend Web
./dev.sh all           # As 4 aplicações acima
```

Pré-requisitos adicionais:
- `npm ci` executado em `src/Frontends/MoneyManager.Web` e `src/Frontends/MoneyManager.Backoffice` (o script roda automaticamente se `node_modules` estiver ausente)

O script inicia cada processo em background, exibe os logs no mesmo terminal e encerra todos juntos com `Ctrl+C`. Se `Permission denied`, rode `chmod +x dev.sh` uma vez.

URLs por cenário:

| Cenário | API | Frontend |
|---|---|---|
| `admin` | http://localhost:5243 | http://localhost:3010 |
| `operational` | http://localhost:5000 | http://localhost:3000 |
| `all` | ambas acima | ambos acima |

> O Worker (`src/Workers/MoneyManager.Worker.Operational`) não é iniciado pelo `dev.sh`; rode-o à parte quando precisar testar os jobs agendados.

### Opção 2: Executar por processo

1. **Restaurar dependências:**
   ```bash
   dotnet restore MoneyManager.sln
   ```

2. **Configurar MongoDB e segredos** em `appsettings.Development.json` (ignorado pelo git) ou via variáveis de ambiente — veja [Configuração](#-configuração).

3. **Executar a API Operacional:**
   ```bash
   dotnet run --project src/APIs/MoneyManager.Api.Operational
   ```

4. **Executar o frontend Web:**
   ```bash
   cd src/Frontends/MoneyManager.Web
   npm run dev
   ```

5. **Acessar:**
   - Frontend: http://localhost:3000
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000/ (disponível apenas em `Development`)
   - Health check: http://localhost:5000/health

### Executar Testes

```bash
dotnet test MoneyManager.sln
```

## 🔑 Configuração

A API Operacional lê as configurações do `appsettings.json` e de variáveis de ambiente (o separador `__` mapeia para `:`, ex.: `Jwt__SecretKey` → `Jwt:SecretKey`). Os valores do `appsettings.json` versionado são placeholders:

```json
{
  "MongoDB": {
    "ConnectionString": "mongo_connection_string+here",
    "DatabaseName": "MoneyAgent"
  },
  "Jwt": {
    "SecretKey": "strong_text_for_secret_here",
    "Issuer": "MoneyManager",
    "Audience": "MoneyManagerUsers",
    "ExpirationHours": 1
  },
  "AllowedOrigins": ["http://localhost:5002"],
  "Encryption": { "Key": "strong_text_for_secret_here" },
  "Anthropic": { "ApiKey": "", "Model": "claude-haiku-4-5-20251001" },
  "Efi": { "ClientId": "...", "ClientSecret": "...", "CertificatePath": "..." },
  "Vapid": { "Subject": "mailto:admin@moneymanager.app", "PublicKey": "...", "PrivateKey": "..." }
}
```

Pontos de atenção:
- **CORS:** a API não sobe sem origens permitidas. Configure `AllowedOrigins` ou a variável `ALLOWED_ORIGIN` (aceita várias, separadas por vírgula), ex.: `http://localhost:3000`.
- **`Encryption:Key`** precisa ser idêntica na API e no Worker (descriptografa as API keys do Banco MCP).
- **Frontend Web:** `NEXT_PUBLIC_API_URL` (veja `src/Frontends/MoneyManager.Web/.env.example`).

> **⚠️ IMPORTANTE:** nunca use os placeholders em produção — defina segredos reais via variáveis de ambiente.

## 📡 Endpoints Principais

A lista completa está no Swagger (em `Development`). Alguns exemplos:

### Autenticação

- `POST /api/auth/register` - Registrar novo usuário
- `POST /api/auth/login` - Login (token JWT em cookie httpOnly)
- `POST /api/auth/refresh` - Renovar token
- `GET /api/auth/me` - Usuário autenticado
- `POST /api/auth/logout` - Logout (revoga o token)

### Categorias, Contas e Transações

- `POST|GET /api/categories`, `GET|PUT|DELETE /api/categories/{id}`
- `POST|GET /api/accounts`, `GET|PUT|DELETE /api/accounts/{id}`
- `POST|GET /api/transactions`, `GET|PUT|DELETE /api/transactions/{id}`

### Orçamentos

- `POST /api/budgets` - Criar orçamento
- `GET /api/budgets` - Listar orçamentos
- `GET /api/budgets/{month}` - Obter orçamento do mês (`yyyy-MM`)
- `PUT /api/budgets/{month}` - Atualizar orçamento
- `POST /api/budgets/{sourceMonth}/copy` - Copiar orçamento para outro mês

### Relatórios

- `GET /api/reports/overview?startDate=2026-01-01&endDate=2026-01-31&basis=Accrual` - Relatório consolidado do período (regime `Accrual` = competência ou `Cash` = caixa), usado pela página de Relatórios
- `GET /api/reports/summary?month=2026-01` - Resumo do mês
- `GET /api/reports/by-category?month=2026-01` - Despesas por categoria

Outros grupos: cartões de crédito e faturas, transações recorrentes, saúde financeira, conexões bancárias (Open Finance), assinaturas, análise de comprovantes, push, onboarding, configurações e exclusão de conta (LGPD).

## 🏗️ Arquitetura

Segue Clean Architecture com as seguintes camadas:

- **Domain**: Entidades, enums e interfaces (depende apenas de `MongoDB.Bson` e `FluentValidation`)
- **Application**: Serviços, DTOs, validações e lógica de negócio
- **Infrastructure**: Implementação técnica (MongoDB, autenticação, criptografia, integrações externas)
- **Presentation**: APIs (Operacional e Backoffice) — controllers, middlewares e composição de DI
- **Worker**: jobs agendados que consomem Application + Infrastructure diretamente

## ✨ Recursos Implementados

- ✅ Autenticação JWT (cookie httpOnly) com BCrypt e revogação no logout
- ✅ Contas, categorias, transações, orçamentos e transações recorrentes
- ✅ Cartões de crédito com faturas e parcelamento
- ✅ Relatórios com regime de competência ou caixa
- ✅ Saúde financeira (metas, reserva, FIRE)
- ✅ Open Finance (Pluggy via Banco MCP) para sincronizar contas e cartões
- ✅ Análise de comprovantes por IA (Anthropic Claude)
- ✅ Assinatura premium (gateway EfiBank) e notificações Web Push
- ✅ Validação com FluentValidation
- ✅ Isolamento de dados por usuário
- ✅ Logging estruturado com NLog e logs de processo (`IProcessLogger`)
- ✅ Swagger/OpenAPI (em Development)
- ✅ Testes unitários com xUnit e NSubstitute
- ✅ Rate limiting nos endpoints de autenticação
- ✅ Middleware global de tratamento de exceções
- ✅ Health check (`/health`)

## 📝 Exemplo de Uso

### 1. Registrar novo usuário
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "João Silva",
    "email": "joao@example.com",
    "password": "Senha@123"
  }'
```

### 2. Fazer login (salva o cookie de autenticação)
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -c cookies.txt \
  -H "Content-Type: application/json" \
  -d '{
    "email": "joao@example.com",
    "password": "Senha@123"
  }'
```

### 3. Criar conta (autenticado)
```bash
curl -X POST http://localhost:5000/api/accounts \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Conta Corrente",
    "type": "Checking",
    "initialBalance": 5000.00
  }'
```

## 🛠️ Tecnologias Utilizadas

- .NET 10 / ASP.NET Core Web API
- MongoDB (`MongoDB.Driver`)
- JWT Bearer + BCrypt
- FluentValidation
- NLog
- Swagger/OpenAPI (Swashbuckle)
- xUnit + NSubstitute
- Next.js, React, TypeScript, Tailwind CSS, TanStack Query
- Docker + Railway

## 🤝 Contribuindo

Toda alteração de código segue o checklist do `AGENTS.md`: testes atualizados e passando,
documentação em `docs/` revisada e changelog do frontend
(`src/Frontends/MoneyManager.Web/public/changelog.md`) com nova versão.

Ative uma vez os hooks versionados, que verificam o changelog antes de cada commit:

```bash
git config core.hooksPath .githooks
```

## 📞 Suporte

Para dúvidas sobre os endpoints, consulte o Swagger UI com a API rodando em `Development`.
