# MoneyManager - Sistema de Controle de Gastos

Sistema completo de gerenciamento financeiro desenvolvido em **.NET 9 + MongoDB**, seguindo os princípios de **Clean Architecture**.

## 📋 Sumário

- [Tecnologias](#tecnologias)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Como Executar](#como-executar)
- [Endpoints](#endpoints)
- [Variáveis de Ambiente](#variáveis-de-ambiente)
- [Decisões de Arquitetura](#decisões-de-arquitetura)

---

## 🛠️ Tecnologias

- **.NET 9** - Framework
- **MongoDB** - Banco de dados NoSQL
- **ASP.NET Core Web API** - Framework para APIs REST
- **JWT** - Autenticação
- **FluentValidation** - Validação de dados
- **NLog** - Logging
- **Docker** - Containerização
- **xUnit** - Testes unitários
- **NSubstitute** - Mock testing
- **Swagger/OpenAPI** - Documentação da API

---

## 📁 Estrutura do Projeto

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
│   │   └── Repositories/                 # UserRepository, CategoryRepository, etc
│   │
│   └── MoneyManager.Presentation/        # API REST, controllers, middlewares
│       ├── Controllers/                  # AuthController, CategoriesController, etc
│       ├── Middlewares/                  # ExceptionHandlingMiddleware
│       ├── Extensions/                   # HttpContextExtensions
│       ├── Program.cs                    # Configuração da aplicação
│       ├── appsettings.json              # Configurações
│       └── nlog.config                   # Configuração de logging
│
├── tests/
│   └── MoneyManager.Tests/               # Testes unitários
│       └── Application/Services/         # TransactionServiceTests, CategoryServiceTests, etc
│
├── docker-compose.yml                    # Orquestração de containers
└── README.md                             # Este arquivo
```

---

## 🚀 Como Executar

### Pré-requisitos

- **.NET 9 SDK** instalado
- **Docker** e **Docker Compose** (para executar com containers)
- **MongoDB** (local ou via Docker)

### Opção 1: Executar Localmente

1. **Clonar o repositório:**
   ```bash
   git clone https://github.com/seuusuario/moneymanager.git
   cd moneymanager
   ```

2. **Restaurar dependências:**
   ```bash
   dotnet restore
   ```

3. **Configurar MongoDB:**
   - Ter MongoDB rodando em `localhost:27017` ou ajustar `appsettings.json`

4. **Executar a API:**
   ```bash
   cd src/MoneyManager.Presentation
   dotnet run
   ```

5. **Acessar Swagger:**
   - http://localhost:5000/swagger

### Opção 2: Executar com Docker Compose

1. **Na raiz do projeto:**
   ```bash
   docker-compose up -d
   ```

2. **Aguardar os containers iniciarem**

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

---

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

---

## 📡 Endpoints

### Autenticação

#### Registrar
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "João Silva",
    "email": "joao@example.com",
    "password": "Senha@123"
  }'
```

**Resposta (201):**
```json
{
  "id": "507f1f77bcf86cd799439011",
  "name": "João Silva",
  "email": "joao@example.com"
}
```

#### Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "joao@example.com",
    "password": "Senha@123"
  }'
```

**Resposta (200):**
```json
{
  "id": "507f1f77bcf86cd799439011",
  "name": "João Silva",
  "email": "joao@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

> **Salvar o `token` para usar nos próximos endpoints** 

---

### Categorias

#### Criar Categoria
```bash
curl -X POST http://localhost:5000/api/categories \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Alimentação",
    "type": 1,
    "color": "#FF5733"
  }'
```

**Tipos:** 
- `0` = Income (Receita)
- `1` = Expense (Despesa)

#### Listar Categorias
```bash
curl -X GET "http://localhost:5000/api/categories?type=1" \
  -H "Authorization: Bearer {token}"
```

#### Atualizar Categoria
```bash
curl -X PUT http://localhost:5000/api/categories/{id} \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Supermercado",
    "type": 1,
    "color": "#FF5733"
  }'
```

#### Deletar Categoria
```bash
curl -X DELETE http://localhost:5000/api/categories/{id} \
  -H "Authorization: Bearer {token}"
```

---

### Contas

#### Criar Conta
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

**Tipos:**
- `0` = Checking (Conta Corrente)
- `1` = Savings (Poupança)
- `2` = Cash (Dinheiro)
- `3` = CreditCard (Cartão de Crédito)
- `4` = Investment (Investimento)

#### Listar Contas
```bash
curl -X GET http://localhost:5000/api/accounts \
  -H "Authorization: Bearer {token}"
```

#### Obter Conta
```bash
curl -X GET http://localhost:5000/api/accounts/{id} \
  -H "Authorization: Bearer {token}"
```

---

### Transações

#### Criar Transação (Receita/Despesa)
```bash
curl -X POST http://localhost:5000/api/transactions \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "accountId": "{accountId}",
    "categoryId": "{categoryId}",
    "type": 0,
    "amount": 100.50,
    "date": "2024-01-15T10:30:00Z",
    "description": "Salário",
    "tags": ["salário", "renda"],
    "status": 0
  }'
```

**Tipos:**
- `0` = Income (Receita)
- `1` = Expense (Despesa)
- `2` = Transfer (Transferência)

#### Criar Transação (Transferência)
```bash
curl -X POST http://localhost:5000/api/transactions \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "accountId": "{fromAccountId}",
    "type": 2,
    "amount": 500.00,
    "date": "2024-01-15T10:30:00Z",
    "toAccountId": "{toAccountId}",
    "description": "Transferência entre contas",
    "status": 0
  }'
```

#### Listar Transações
```bash
curl -X GET http://localhost:5000/api/transactions \
  -H "Authorization: Bearer {token}"
```

#### Atualizar Transação
```bash
curl -X PUT http://localhost:5000/api/transactions/{id} \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "accountId": "{accountId}",
    "categoryId": "{categoryId}",
    "type": 0,
    "amount": 150.00,
    "date": "2024-01-15T10:30:00Z",
    "description": "Salário (atualizado)",
    "tags": ["salário"],
    "status": 0
  }'
```

#### Deletar Transação
```bash
curl -X DELETE http://localhost:5000/api/transactions/{id} \
  -H "Authorization: Bearer {token}"
```

---

### Orçamentos

#### Criar Orçamento
```bash
curl -X POST http://localhost:5000/api/budgets \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "month": "2024-01",
    "items": [
      {
        "categoryId": "{categoryId1}",
        "limitAmount": 500.00
      },
      {
        "categoryId": "{categoryId2}",
        "limitAmount": 300.00
      }
    ]
  }'
```

#### Obter Orçamento do Mês
```bash
curl -X GET http://localhost:5000/api/budgets/2024-01 \
  -H "Authorization: Bearer {token}"
```

#### Listar Todos os Orçamentos
```bash
curl -X GET http://localhost:5000/api/budgets \
  -H "Authorization: Bearer {token}"
```

#### Atualizar Orçamento
```bash
curl -X PUT http://localhost:5000/api/budgets/2024-01 \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "items": [
      {
        "categoryId": "{categoryId1}",
        "limitAmount": 600.00
      }
    ]
  }'
```

---

### Relatórios

#### Resumo do Mês
```bash
curl -X GET "http://localhost:5000/api/reports/summary?month=2024-01" \
  -H "Authorization: Bearer {token}"
```

**Resposta:**
```json
{
  "totalIncome": 5000.00,
  "totalExpense": 1200.50,
  "balance": 3799.50
}
```

#### Despesas por Categoria
```bash
curl -X GET "http://localhost:5000/api/reports/by-category?month=2024-01&type=1" \
  -H "Authorization: Bearer {token}"
```

#### Fluxo de Caixa
```bash
curl -X GET "http://localhost:5000/api/reports/cashflow?from=2024-01-01&to=2024-01-31" \
  -H "Authorization: Bearer {token}"
```

---

## 🏗️ Decisões de Arquitetura

### Clean Architecture
- **Domain**: Entidades puras sem dependências
- **Application**: Serviços, DTOs e regras de negócio
- **Infrastructure**: Implementação técnica (MongoDB, autenticação)
- **Presentation**: API REST e middlewares

### Padrões Utilizados

1. **Repository Pattern**: Abstração de acesso a dados
2. **Unit of Work**: Gerenciamento de transações
3. **Dependency Injection**: Injeção de dependências via .NET DI
4. **DTO Pattern**: Separação entre entidades e respostas
5. **Validation Pattern**: FluentValidation para regras de negócio
6. **JWT Authentication**: Token-based authentication
7. **Soft Delete**: Exclusões lógicas com flag IsDeleted
8. **User Isolation**: Todas as operações filtradas por UserId

### Boas Práticas Implementadas

✅ **Async/Await em toda a aplicação**  
✅ **Middleware global de exceção**  
✅ **ProblemDetails para erros padronizados**  
✅ **Logging estruturado com NLog**  
✅ **Swagger/OpenAPI para documentação**  
✅ **Health Checks em `/health`**  
✅ **Validação fluente com FluentValidation**  
✅ **Isolamento de dados por usuário**  
✅ **Índices de performance no MongoDB**  
✅ **Testes unitários com xUnit e NSubstitute**  

### Segurança

- ✅ JWT Bearer Token
- ✅ Password hashing com BCrypt
- ✅ User isolation em todas as queries
- ✅ Validação de entrada com FluentValidation
- ✅ HTTPS em produção
- ✅ CORS configurável

---

## 🔄 Fluxos Importantes

### Criar Transação com Impacto no Saldo
1. Valida conta existe
2. Aplica impacto no saldo (Income: +, Expense: -)
3. Para transferências: debita origem e credita destino
4. Persiste transação
5. Atualiza conta com novo saldo

### Atualizar Transação
1. Obtém transação original
2. Reverte impacto anterior
3. Aplica novo impacto
4. Persiste mudanças

### Deletar Transação
1. Marca como IsDeleted = true (Soft Delete)
2. Reverte impacto no saldo
3. Atualiza conta

### Orçamento
1. Agrupa transações por categoria
2. Compara com limite definido
3. Retorna análise de gastos vs. limites

---

## 📝 Notas Importantes

> **Performance**: MongoDB com índices em userId + date para queries rápidas  
> **Escalabilidade**: Pronto para sharding horizontal  
> **Manutenção**: Código limpo e bem estruturado para fácil manutenção  
> **Testes**: 100% cobertura em serviços críticos (transações, orçamento)  

---

## 📞 Suporte

Para dúvidas ou problemas, consulte a documentação do Swagger em `/swagger` quando a API estiver rodando.

---

**Desenvolvido com ❤️ usando .NET 9 + MongoDB**

