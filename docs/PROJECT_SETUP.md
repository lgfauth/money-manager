# MoneyManager

Sistema completo de gerenciamento financeiro desenvolvido em **.NET 9 + MongoDB**, seguindo os princípios de **Clean Architecture**.

## 📋 Estrutura do Projeto

```
MoneyManager/
├── src/
│   ├── MoneyManager.Domain/              # Entidades e interfaces (camada de domínio)
│   │   ├── Entities/                     # User, Category, Account, Budget, Transaction
│   │   ├── Enums/                        # UserStatus, CategoryType, AccountType, etc
│   │   └── Interfaces/                   # IRepository, IUnitOfWork
│   │
│   ├── MoneyManager.Application/         # Serviços, DTOs e validações
│   │   ├── Services/                     # AuthService, CategoryService, etc
│   │   ├── DTOs/                         # Request/Response objects
│   │   └── Validators/                   # FluentValidation rules
│   │
│   ├── MoneyManager.Infrastructure/      # MongoDB, repositórios e auth
│   │   ├── Data/                         # MongoContext, MongoSettings
│   │   ├── Repositories/                 # UserRepository, CategoryRepository, etc
│   │   └── Security/                     # TokenService
│   │
│   └── MoneyManager.Presentation/        # API REST, controllers, middlewares
│       ├── Controllers/                  # AuthController, CategoriesController, etc
│       ├── Middlewares/                  # ExceptionHandlingMiddleware
│       ├── Extensions/                   # HttpContextExtensions
│       ├── Program.cs                    # Configuração da aplicação
│       ├── appsettings.json              # Configurações
│       ├── nlog.config                   # Configuração de logging
│       └── Dockerfile                    # Docker image configuration
│
├── tests/
│   └── MoneyManager.Tests/               # Testes unitários
│       └── Application/Services/         # AuthServiceTests, CategoryServiceTests, etc
│
├── docker-compose.yml                    # Orquestração de containers
└── README.md                             # Este arquivo
```

## 🚀 Como Executar

### Pré-requisitos

- **.NET 9 SDK** instalado
- **Docker** e **Docker Compose** (para executar com containers)
- **MongoDB** (local ou via Docker)

### Opção 1: Script único (`dev.sh`) — recomendado

Na raiz do repositório, `./dev.sh` sobe os cenários de desenvolvimento com um único comando, cuidando de API(s) .NET e frontend(s) Next.js juntos:

```bash
./dev.sh admin         # Ambiente administrativo: API Backoffice + Frontend Backoffice
./dev.sh operational   # Ambiente operativo: API Operacional + Frontend Web
./dev.sh all           # As 4 aplicações acima
```

Pré-requisitos:
- **dotnet SDK** e **node/npm** instalados
- **MongoDB** acessível em `mongodb://localhost:27017` (ex.: `docker compose up -d mongodb`)
- `npm ci` já executado em `src/Frontends/MoneyManager.Web` e `src/Frontends/MoneyManager.Backoffice` (o script roda automaticamente se `node_modules` estiver ausente)

O script inicia cada processo em background, exibe os logs no mesmo terminal e encerra todos juntos com `Ctrl+C`. Se `Permission denied`, rode `chmod +x dev.sh` uma vez.

URLs por cenário:

| Cenário | API | Frontend |
|---|---|---|
| `admin` | http://localhost:5243 | http://localhost:3010 |
| `operational` | http://localhost:5000 | http://localhost:3000 |
| `all` | ambas acima | ambos acima |

### Opção 2: Executar Localmente (por processo)

1. **Restaurar dependências:**
   ```bash
   dotnet restore
   ```

2. **Configurar MongoDB:**
   - Ter MongoDB rodando em `localhost:27017` ou ajustar `appsettings.json`

3. **Executar a API:**
   ```bash
   cd src/MoneyManager.Presentation
   dotnet run
   ```

4. **Acessar Swagger:**
   - http://localhost:5000/swagger

### Opção 2: Executar com Docker Compose

1. **Na raiz do projeto:**
   ```bash
   docker-compose up -d
   ```

2. **Aguardar os containers iniciarem (cerca de 30 segundos)**

3. **Acessar os serviços:**
   - API: http://localhost:5000
   - Swagger: http://localhost:5000/swagger
   - Mongo Express: http://localhost:8081

4. **Parar os containers:**
   ```bash
   docker-compose down
   ```

### Executar Testes

```bash
dotnet test tests/MoneyManager.Tests/MoneyManager.Tests.csproj
```

## 🔑 Variáveis de Ambiente

Configurar no `appsettings.json` ou via variáveis de ambiente:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "moneymanager"
  },
  "Jwt": {
    "SecretKey": "your-super-secret-key-that-is-long-enough-for-256-bits",
    "Issuer": "MoneyManager",
    "Audience": "MoneyManagerUsers",
    "ExpirationHours": 24
  }
}
```

> **⚠️ IMPORTANTE:** Alterar a `SecretKey` em produção para uma chave segura!

## 📡 Endpoints Principais

### Autenticação

- `POST /api/auth/register` - Registrar novo usuário
- `POST /api/auth/login` - Login e obter token JWT

### Categorias

- `POST /api/categories` - Criar categoria
- `GET /api/categories` - Listar categorias
- `GET /api/categories/{id}` - Obter categoria
- `PUT /api/categories/{id}` - Atualizar categoria
- `DELETE /api/categories/{id}` - Deletar categoria

### Contas

- `POST /api/accounts` - Criar conta
- `GET /api/accounts` - Listar contas
- `GET /api/accounts/{id}` - Obter conta
- `PUT /api/accounts/{id}` - Atualizar conta
- `DELETE /api/accounts/{id}` - Deletar conta

### Transações

- `POST /api/transactions` - Criar transação
- `GET /api/transactions` - Listar transações
- `GET /api/transactions/{id}` - Obter transação
- `PUT /api/transactions/{id}` - Atualizar transação
- `DELETE /api/transactions/{id}` - Deletar transação

### Orçamentos

- `POST /api/budgets` - Criar orçamento
- `GET /api/budgets/{month}` - Obter orçamento do mês
- `GET /api/budgets` - Listar todos os orçamentos
- `PUT /api/budgets/{month}` - Atualizar orçamento

### Relatórios

- `GET /api/reports/summary?month=2024-01` - Resumo do mês
- `GET /api/reports/by-category?month=2024-01` - Despesas por categoria

## 🏗️ Arquitetura

Segue Clean Architecture com as seguintes camadas:

- **Domain**: Entidades, enums e interfaces sem dependências externas
- **Application**: Serviços, DTOs, validações e lógica de negócio
- **Infrastructure**: Implementação técnica (MongoDB, autenticação)
- **Presentation**: Controllers, middlewares e configuração da API

## ✨ Recursos Implementados

- ✅ Autenticação JWT com BCrypt
- ✅ CRUD completo para todas as entidades
- ✅ Validação com FluentValidation
- ✅ Isolamento de dados por usuário
- ✅ Transações com impacto em saldos
- ✅ Orçamentos e relatórios financeiros
- ✅ Logging estruturado com NLog
- ✅ Documentação com Swagger/OpenAPI
- ✅ Testes unitários com xUnit e NSubstitute
- ✅ Docker e Docker Compose
- ✅ Índices de performance no MongoDB
- ✅ Middleware de tratamento de exceções global
- ✅ CORS configurável
- ✅ Health check endpoint

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

### 2. Fazer login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "joao@example.com",
    "password": "Senha@123"
  }'
```

### 3. Criar conta (com token)
```bash
curl -X POST http://localhost:5000/api/accounts \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Conta Corrente",
    "type": 0,
    "initialBalance": 5000.00
  }'
```

## 🛠️ Tecnologias Utilizadas

- .NET 9
- MongoDB
- ASP.NET Core Web API
- JWT (System.IdentityModel.Tokens.Jwt)
- FluentValidation
- NLog
- xUnit
- NSubstitute
- Swagger/OpenAPI
- Docker

## 📞 Suporte

Para dúvidas, consulte a documentação do Swagger em `/swagger` quando a API estiver rodando.

---

**Desenvolvido com ❤️ usando .NET 9 + MongoDB**
