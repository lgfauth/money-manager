# MoneyManager — Copilot Instructions
 
## Projeto
Sistema de gerenciamento financeiro pessoal.
Repositório: lgfauth/money-manager
 
## Stack
- Runtime: .NET 9 / C# (latest stable features)
- API: ASP.NET Core Web API
- Banco: MongoDB (driver oficial, sem EF Core)
- Frontend: Next.js 14+ com App Router e TypeScript
- Auth: JWT Bearer
- Validação: FluentValidation (no pipeline, nunca nos services)
- Logging: NLog via ILogger<T>
- Testes: xUnit + NSubstitute
## Arquitetura — Clean Architecture (4 camadas)
 
### Domain (MoneyManager.Domain)
- Entidades: User, Category, Account, Budget, Transaction
- Enums: UserStatus, CategoryType, AccountType, TransactionType, etc.
- Interfaces: IRepository<T>, IUnitOfWork e afins
- ZERO dependências externas — nem NuGet, nem outras camadas
- Entidades com comportamento: sem setters públicos nas propriedades de domínio
### Application (MoneyManager.Application)
- Services: AuthService, CategoryService, TransactionService, etc.
- DTOs: objetos de Request e Response separados por feature
- Validators: regras FluentValidation (nunca dentro dos services)
- Orquestra o domínio — não tem lógica de infraestrutura aqui
### Infrastructure (MoneyManager.Infrastructure)
- Data: MongoContext, MongoSettings
- Repositories: implementações concretas dos IRepository
- Nunca referenciada diretamente pela camada Application ou Domain
### Presentation (MoneyManager.Presentation)
- Controllers REST finos — delegam tudo para os services
- Middlewares: ExceptionHandlingMiddleware (erros padronizados via ProblemDetails)
- Extensions: HttpContextExtensions
- Program.cs: composição root, DI, middlewares
## Padrões obrigatórios
 
### Dependências
- Sempre injetadas via construtor — nunca instanciadas com new()
- Novas dependências registradas em Program.cs ou em métodos de extensão
### Resultados de operação
- Usar Union/Result pattern para retornos tipados
- NUNCA substituir por throw ou return null onde Union já é usado
- Erros devem ter tipos específicos — nunca string genérica
### Acesso a dados
- Repository Pattern: toda query passa pelo IRepository
- Unit of Work quando múltiplas coleções são afetadas na mesma operação
- Soft Delete: exclusões usam IsDeleted = true, nunca delete físico
- User Isolation: toda query inclui filtro por UserId
### Async
- Tudo async/await — sem .Result ou .Wait()
- Sem fire-and-forget a não ser em background workers explícitos
### Nomenclatura
- Classes, métodos, propriedades: PascalCase (inglês)
- Variáveis locais, parâmetros: camelCase (inglês)
- Comentários e mensagens de log: português
- DTOs de request: sufixo Request (ex: CreateTransactionRequest)
- DTOs de response: sufixo Response (ex: TransactionResponse)
- Interfaces: prefixo I (ex: ITransactionRepository)
## O que NUNCA fazer
- Lógica de negócio em Controllers
- Queries diretas ao MongoDB fora dos Repositories
- Validação dentro dos Services (vai nos Validators)
- Referência de Infrastructure em Domain ou Application
- Adicionar pacotes NuGet sem ser explicitamente pedido
- Alterar assinatura pública de métodos sem instrução explícita
- Renomear membros públicos sem instrução explícita
- CQRS ou MediatR — não usamos neste projeto
## Testes
- Framework: xUnit
- Mocks: NSubstitute (Substitute.For<IInterface>())
- Padrão: Arrange / Act / Assert com comentários de seção
- Um Assert por teste sempre que possível
- Cobrir: caminho feliz + ao menos um caso de erro por service
## Formato de entrega de código
- Entregar arquivos na ordem: Domain → Application → Infrastructure → Presentation
- Cada arquivo em bloco de código separado com o caminho completo no topo
- Comentários em português apenas onde a lógica é não-óbvia
- Sem explicações longas após o código — só entregar o que foi pedido