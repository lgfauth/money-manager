# MoneyManager.Tests

Projeto de testes unitários da API MoneyManager.

## Visão Geral

Este projeto contém todos os testes unitários das camadas de Aplicação, Domínio e Apresentação do MoneyManager, garantindo a qualidade e confiabilidade do código.

## Estatísticas

- **Total de Testes:** 133
- **Taxa de Sucesso:** 100%
- **Framework:** xUnit 2.7.1
- **Target Framework:** .NET 9.0

## Estrutura do Projeto

```
tests/MoneyManager.Tests/
├── Application/
│   └── Services/
│       ├── AccountServiceTests.cs              # 13 testes
│       ├── AccountDeletionServiceTests.cs      # 5 testes
│       ├── AuthServiceTests.cs                 # 3 testes
│       ├── BudgetServiceTests.cs               # 18 testes
│       ├── CategoryServiceTests.cs             # 2 testes
│       ├── CreditCardServiceTests.cs           # 8 testes
│       ├── FinancialHealthServiceTests.cs      # 16 testes
│       ├── OnboardingServiceTests.cs           # 7 testes
│       ├── RecurringTransactionServiceTests.cs # 11 testes
│       ├── ReportServiceTests.cs               # 5 testes
│       ├── TransactionServiceTests.cs          # 13 testes
│       ├── UserProfileServiceTests.cs          # 9 testes
│       └── UserSettingsServiceTests.cs         # 6 testes
├── Domain/
│   └── Entities/
│       ├── FinancialHealthEntityTests.cs       # 7 testes
│       └── RecurringTransactionBsonTests.cs    # 1 teste
├── Presentation/
│   └── Controllers/
│       ├── FinancialHealthControllerTests.cs   # 8 testes
│       └── UsersControllerTests.cs             # 1 teste
└── MoneyManager.Tests.csproj
```

## Dependências

```xml
<PackageReference Include="xunit" Version="2.7.1" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.6" />
<PackageReference Include="NSubstitute" Version="5.1.0" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
<PackageReference Include="coverlet.collector" Version="6.0.2" />
```

## Executando os Testes

### Todos os testes
```bash
dotnet test
```

### Com verbosidade detalhada
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Testes específicos de um serviço
```bash
# Apenas AccountService
dotnet test --filter "FullyQualifiedName~AccountServiceTests"

# Apenas FinancialHealthService
dotnet test --filter "FullyQualifiedName~FinancialHealthServiceTests"
```

### Com cobertura de código
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Executar teste específico
```bash
dotnet test --filter "MethodName=CreateAsync_WithValidRequest_ShouldCreateAccount"
```

## Cobertura por Serviço

| Serviço / Componente | Testes |
|---|---|
| `BudgetService` | 18 |
| `FinancialHealthService` | 16 |
| `AccountService` | 13 |
| `TransactionService` | 13 |
| `RecurringTransactionService` | 11 |
| `UserProfileService` | 9 |
| `FinancialHealthControllerTests` | 8 |
| `CreditCardService` | 8 |
| `OnboardingService` | 7 |
| `FinancialHealthEntityTests` | 7 |
| `UserSettingsService` | 6 |
| `ReportService` | 5 |
| `AccountDeletionService` | 5 |
| `AuthService` | 3 |
| `CategoryService` | 2 |
| `UsersController` | 1 |
| `RecurringTransactionBsonTests` | 1 |

## Padrões de Teste

### Nomenclatura

Seguimos o padrão: `MethodName_Scenario_ExpectedResult`

```csharp
[Fact]
public async Task CreateAsync_WithValidRequest_ShouldCreateAccount()
```

### Estrutura AAA (Arrange-Act-Assert)

```csharp
[Fact]
public async Task ExampleTest()
{
    // Arrange
    var userId = "user123";
    var request = new SomeRequestDto { ... };

    // Act
    var result = await _service.SomeMethodAsync(userId, request);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(expected, result.Property);
}
```

## Uso de Mocks

Utilizamos **NSubstitute** para criar mocks das dependências:

```csharp
private readonly IUnitOfWork _unitOfWorkMock;

public TransactionServiceTests()
{
    _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    _transactionService = new TransactionService(_unitOfWorkMock, ...);
}
```

## Cenários Testados

### Casos de Sucesso
- Criação de entidades válidas
- Atualização de dados
- Listagem e filtros
- Cálculos e agregações

### Tratamento de Erros
- Entidades não encontradas
- Validação de permissões
- Dados inválidos
- Regras de negócio

### Casos Especiais
- Soft deletes (exclusão lógica)
- Isolamento entre usuários
- Transações com impacto em saldo
- Recorrências automáticas
- Score de saúde financeira e projeções FIRE
- Serialização BSON de entidades

## Ferramentas Úteis

### Watch Mode (Execução Contínua)
```bash
dotnet watch test
```

### Filtros Avançados
```bash
# Apenas testes que contêm "Create"
dotnet test --filter "DisplayName~Create"

# Apenas testes de erro
dotnet test --filter "DisplayName~Exception"
```

### Relatório HTML de Cobertura
```bash
# Instalar ferramenta
dotnet tool install -g dotnet-reportgenerator-globaltool

# Gerar cobertura
dotnet test --collect:"XPlat Code Coverage"

# Gerar relatório
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

## Debugging

### VS Code
1. Instale a extensão ".NET Core Test Explorer"
2. Clique no ícone de debug ao lado do teste

### Linha de Comando
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Adicionando Novos Testes

### Template Básico

```csharp
using NSubstitute;
using Xunit;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Tests.Application.Services;

public class NewServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly INewService _newService;

    public NewServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _newService = new NewService(_unitOfWorkMock);
    }

    [Fact]
    public async Task MethodName_Scenario_ExpectedResult()
    {
        // Arrange

        // Act

        // Assert
    }
}
```

## Contribuindo

Ao adicionar novos testes:

1. Siga o padrão de nomenclatura `MethodName_Scenario_ExpectedResult`
2. Use a estrutura AAA (Arrange-Act-Assert)
3. Teste casos de sucesso e erro
4. Isole os testes (sem dependências externas)
5. Mantenha os testes rápidos
6. Documente cenários complexos

## Recursos

- [xUnit Documentation](https://xunit.net/)
- [NSubstitute Documentation](https://nsubstitute.github.io/)
- [.NET Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

---

**Última Atualização:** 17 de junho de 2026
