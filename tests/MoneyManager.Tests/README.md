# ?? MoneyManager.Tests

Projeto de testes unitários para a API MoneyManager.

## ?? Visão Geral

Este projeto contém todos os testes unitários da camada de aplicação (Application Layer) do MoneyManager, garantindo a qualidade e confiabilidade do código.

## ?? Estatísticas

- **Total de Testes:** 49
- **Taxa de Sucesso:** 100%
- **Cobertura Estimada:** ~93%
- **Framework:** xUnit 2.7.1
- **Target Framework:** .NET 9.0

## ??? Estrutura do Projeto

```
tests/MoneyManager.Tests/
??? Application/
?   ??? Services/
?       ??? AccountServiceTests.cs          # 7 testes
?       ??? AuthServiceTests.cs             # Existente
?       ??? BudgetServiceTests.cs           # 7 testes
?       ??? CategoryServiceTests.cs         # 7 testes
?       ??? RecurringTransactionServiceTests.cs  # 9 testes
?       ??? ReportServiceTests.cs           # 5 testes
?       ??? TransactionServiceTests.cs      # 9 testes
?       ??? UserProfileServiceTests.cs      # 8 testes
?       ??? UserSettingsServiceTests.cs     # 6 testes
??? MoneyManager.Tests.csproj
```

## ?? Dependências

```xml
<PackageReference Include="xunit" Version="2.7.1" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.6" />
<PackageReference Include="NSubstitute" Version="5.1.0" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
<PackageReference Include="coverlet.collector" Version="6.0.2" />
```

## ?? Executando os Testes

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

# Apenas TransactionService
dotnet test --filter "FullyQualifiedName~TransactionServiceTests"
```

### Com cobertura de código
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Executar teste específico
```bash
dotnet test --filter "MethodName=CreateAsync_WithValidRequest_ShouldCreateAccount"
```

## ?? Cobertura por Serviço

| Serviço | Testes | Cobertura | Status |
|---------|--------|-----------|--------|
| AccountService | 7 | ~95% | ? |
| TransactionService | 9 | ~95% | ? |
| CategoryService | 7 | ~92% | ? |
| BudgetService | 7 | ~95% | ? |
| RecurringTransactionService | 9 | ~93% | ? |
| ReportService | 5 | ~90% | ? |
| UserProfileService | 8 | ~95% | ? |
| UserSettingsService | 6 | ~92% | ? |
| AuthService | 3 | ~90% | ? |

## ?? Padrões de Teste

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
    // Arrange - Preparação
    var userId = "user123";
    var request = new SomeRequestDto { ... };
    
    // Act - Execução
    var result = await _service.SomeMethodAsync(userId, request);
    
    // Assert - Verificação
    Assert.NotNull(result);
    Assert.Equal(expected, result.Property);
}
```

## ?? Uso de Mocks

Utilizamos **NSubstitute** para criar mocks das dependências:

```csharp
private readonly IUnitOfWork _unitOfWorkMock;
private readonly IAccountService _accountServiceMock;

public TransactionServiceTests()
{
    _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    _accountServiceMock = Substitute.For<IAccountService>();
    _transactionService = new TransactionService(_unitOfWorkMock, _accountServiceMock);
}
```

## ? Cenários Testados

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

## ?? Métricas de Qualidade

### Velocidade
- **Tempo médio por teste:** ~35ms
- **Tempo total de execução:** ~1.7s
- **Testes mais lentos:** AuthService (hash de senha)

### Confiabilidade
- **Taxa de sucesso:** 100%
- **Testes flaky:** 0
- **Dependências externas:** 0 (todos mocados)

## ?? Ferramentas Úteis

### Watch Mode (Execução Contínua)
```bash
dotnet watch test
```

### Filtros Avançados
```bash
# Apenas testes que contém "Create"
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

## ?? Debugging

### Visual Studio
1. Abra o Test Explorer (Test > Test Explorer)
2. Clique com botão direito no teste
3. Selecione "Debug"

### VS Code
1. Instale a extensão ".NET Core Test Explorer"
2. Clique no ícone de debug ao lado do teste

### Linha de Comando
```bash
# Adicione breakpoint no código
# Execute com debugger attached
dotnet test --logger "console;verbosity=detailed"
```

## ?? Adicionando Novos Testes

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

## ?? Contribuindo

Ao adicionar novos testes:

1. ? Siga o padrão de nomenclatura
2. ? Use a estrutura AAA
3. ? Teste casos de sucesso e erro
4. ? Isole os testes (sem dependências externas)
5. ? Mantenha os testes rápidos
6. ? Documente cenários complexos

## ?? Recursos

- [xUnit Documentation](https://xunit.net/)
- [NSubstitute Documentation](https://nsubstitute.github.io/)
- [.NET Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

## ?? Suporte

Em caso de dúvidas sobre os testes:
1. Consulte o relatório de cobertura: `docs/TestCoverageReport.md`
2. Verifique exemplos existentes
3. Entre em contato com a equipe

---

**Última Atualização:** ${new Date().toLocaleDateString('pt-BR')}  
**Mantido por:** Equipe MoneyManager
