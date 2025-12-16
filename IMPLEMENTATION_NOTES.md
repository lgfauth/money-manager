# MoneyManager - Notas de Implementação

## Tecnologias Instaladas

- **MongoDB.Driver** 2.25.0
- **FluentValidation** 11.9.2
- **BCrypt.Net-Next** 4.0.3
- **System.IdentityModel.Tokens.Jwt** 7.6.2
- **Swashbuckle.AspNetCore** 6.0.0
- **NLog.Web.AspNetCore** 5.3.7
- **Microsoft.AspNetCore.Authentication.JwtBearer** 9.0.0
- **xUnit** 2.7.1
- **NSubstitute** 5.1.0

## Estrutura Implementada

### Domain Layer
- Entidades: User, Category, Account, Transaction, Budget
- Enums: UserStatus, CategoryType, AccountType, TransactionType, TransactionStatus
- Interfaces: IRepository, IUserRepository, IUnitOfWork

### Application Layer
- Serviços: AuthService, CategoryService, AccountService, TransactionService, BudgetService, ReportService
- DTOs: Request e Response para todas as operações
- Validators: Usando FluentValidation para todas as entidades

### Infrastructure Layer
- MongoDB Context e Settings
- Repository Pattern com genérico Repository<T>
- Unit of Work implementation
- TokenService para JWT

### Presentation Layer
- Controllers: Auth, Categories, Accounts, Transactions, Budgets, Reports
- Exception Handling Middleware
- CORS Configuration
- Swagger/OpenAPI documentation
- Health check endpoint

## Como Começar

### Opção 1: Local (requer MongoDB rodando)
```bash
cd src/MoneyManager.Presentation
dotnet run
```

### Opção 2: Docker Compose
```bash
docker-compose up -d
```

## Endpoints Disponíveis

Todos os endpoints (exceto auth/register e auth/login) requerem JWT token no header:
`Authorization: Bearer {token}`

Acesse http://localhost:5000/swagger para documentação interativa completa.
