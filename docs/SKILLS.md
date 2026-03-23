# Skills de Desenvolvimento — MoneyManager

> Guia de competências e técnicas organizadas por projeto da solução.
> Use este documento para onboarding de novos contribuidores e como referência ao iniciar trabalho em cada camada.

---

## Sumário

- [Domain Skills](#domain-skills)
- [Application Skills](#application-skills)
- [Infrastructure Skills](#infrastructure-skills)
- [Presentation (API) Skills](#presentation-api-skills)
- [Web (Blazor) Skills](#web-blazor-skills)
- [Web.Host Skills](#webhost-skills)
- [Worker Skills](#worker-skills)
- [Tests Skills](#tests-skills)

---

## Domain Skills

> Projeto: `src/MoneyManager.Domain`

### O que você precisa saber

| Skill | Descrição |
|---|---|
| Modelagem de entidades | Criar/editar entidades com `[BsonId]`, `[BsonElement]` e soft delete (`IsDeleted`) |
| Contratos de repositório | Declarar ou estender interfaces em `Interfaces/` sem depender de infraestrutura |
| Enums de domínio | Adicionar novos valores de enum mantendo compatibilidade com dados existentes no MongoDB |
| Convenção de IDs | IDs são `string` representando `ObjectId` do MongoDB |
| Soft delete | Toda exclusão lógica usa `IsDeleted = true`; nunca remover documentos fisicamente em produção |

### Checklist ao adicionar uma nova entidade

- [ ] Herdar ou replicar padrão de `Id`, `UserId`, `CreatedAt`, `UpdatedAt`, `IsDeleted`
- [ ] Anotar todos os campos com `[BsonElement("camelCaseName")]`
- [ ] Usar `[BsonIgnoreExtraElements]` para tolerância a schema evolution
- [ ] Declarar interface de repositório em `Interfaces/` se o repositório precisar de métodos customizados
- [ ] Adicionar a nova entidade ao `IUnitOfWork`

### Exemplo de entidade

```csharp
[BsonIgnoreExtraElements]
public class MinhaEntidade
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("nome")]
    public string Nome { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }
}
```

---

## Application Skills

> Projeto: `src/MoneyManager.Application`

### O que você precisa saber

| Skill | Descrição |
|---|---|
| Serviços de negócio | Implementar `IFooService` e `FooService` no mesmo arquivo |
| DTOs | Criar `FooRequestDto` e `FooResponseDto` sem lógica de negócio |
| FluentValidation | Criar validadores em `Validators/` para cada DTO de entrada relevante |
| Uso do UnitOfWork | Acessar repositórios somente via `IUnitOfWork` injetado |
| Logging estruturado | Usar `ILogger<T>` com placeholders — nunca interpolar strings |
| Exceções de negócio | `KeyNotFoundException` para entidade não encontrada, `InvalidOperationException` para regra violada |
| Soft delete | Filtrar `!entity.IsDeleted` e `entity.UserId == userId` em toda consulta |
| Isolamento por usuário | Toda operação valida que o recurso pertence ao `userId` do contexto |

### Checklist ao adicionar um novo serviço

- [ ] Declarar interface e implementação no mesmo arquivo `.cs`
- [ ] Injetar `IUnitOfWork` e `ILogger<T>` no construtor
- [ ] Criar DTOs de Request e Response separados
- [ ] Criar validador FluentValidation para o DTO de entrada
- [ ] Registrar o serviço em `MoneyManager.Presentation/Program.cs` como `Scoped`
- [ ] Adicionar testes unitários no projeto `MoneyManager.Tests`

### Padrão de serviço

```csharp
public interface IFooService
{
    Task<FooResponseDto> CreateAsync(string userId, FooRequestDto request);
    Task<IEnumerable<FooResponseDto>> GetAllAsync(string userId);
    Task<FooResponseDto> GetByIdAsync(string userId, string id);
    Task<FooResponseDto> UpdateAsync(string userId, string id, FooRequestDto request);
    Task DeleteAsync(string userId, string id);
}

public class FooService : IFooService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FooService> _logger;

    public FooService(IUnitOfWork unitOfWork, ILogger<FooService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FooResponseDto> CreateAsync(string userId, FooRequestDto request)
    {
        // validar + criar entidade + salvar + retornar DTO
    }
}
```

### Mapeamento Manual (sem AutoMapper)

O projeto usa mapeamento manual de entidade ? DTO:

```csharp
private static FooResponseDto MapToDto(Foo entity) => new()
{
    Id = entity.Id,
    // ...
};
```

---

## Infrastructure Skills

> Projeto: `src/MoneyManager.Infrastructure`

### O que você precisa saber

| Skill | Descrição |
|---|---|
| Repositório genérico | `Repository<T>` cobre CRUD padrão para qualquer entidade |
| Repositório especializado | Criar classe que herda `Repository<T>` ou implementa interface específica do Domain |
| MongoContext | Obtém `IMongoCollection<T>` por nome de coleção |
| Criação de índices | Adicionar em `CreateCollectionsAndIndexesAsync()` no startup |
| TokenService | Gera JWT com claims de userId e email |
| Configuração | `MongoSettings` lido de `appsettings.json` seção `MongoDB` |

### Checklist ao adicionar suporte a nova entidade

- [ ] Registrar o repositório no `UnitOfWork`
- [ ] Criar a coleção e seus índices em `MongoContext.CreateCollectionsAndIndexesAsync()`
- [ ] Se precisar de queries customizadas, criar repositório especializado

### Exemplo de repositório especializado

```csharp
public class FooRepository : Repository<Foo>, IFooRepository
{
    public FooRepository(MongoContext context) : base(context, "foos") { }

    public async Task<Foo?> GetByUserAndCodeAsync(string userId, string code)
    {
        var filter = Builders<Foo>.Filter.And(
            Builders<Foo>.Filter.Eq(f => f.UserId, userId),
            Builders<Foo>.Filter.Eq(f => f.Code, code)
        );
        return await Collection.Find(filter).FirstOrDefaultAsync();
    }
}
```

### Adicionando índice

```csharp
// Em MongoContext.CreateCollectionsAndIndexesAsync()
var foosCollection = _database.GetCollection<Foo>("foos");
await foosCollection.Indexes.CreateOneAsync(new CreateIndexModel<Foo>(
    Builders<Foo>.IndexKeys.Ascending(f => f.UserId).Ascending(f => f.Code)
));
```

---

## Presentation (API) Skills

> Projeto: `src/MoneyManager.Presentation`

### O que você precisa saber

| Skill | Descrição |
|---|---|
| Controllers thin | Receber request ? validar ? delegar ao serviço ? retornar resultado |
| Extração do userId | Usar `User.FindFirst(ClaimTypes.NameIdentifier)?.Value` |
| Tratamento de exceções | Deixar o `ExceptionHandlingMiddleware` lidar com `KeyNotFoundException` e `InvalidOperationException` |
| FluentValidation nos controllers | Chamar `await _validator.ValidateAsync(request)` e retornar `BadRequest` se inválido |
| Rotas REST | Seguir padrão `api/[controller]` com verbos semânticos |
| Swagger | Todos os endpoints são documentados automaticamente; usar XML docs quando útil |

### Checklist ao adicionar um endpoint

- [ ] Criar ou estender controller em `Controllers/`
- [ ] Anotar com `[Authorize]` se requer autenticação
- [ ] Injetar o serviço correspondente de Application
- [ ] Injetar `IValidator<TRequest>` se o endpoint recebe body
- [ ] Usar `CreatedAtAction` para POST que cria recurso
- [ ] Retornar `NoContent()` para DELETE
- [ ] Não duplicar lógica de negócio no controller

### Padrão de controller

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoosController : ControllerBase
{
    private readonly IFooService _service;
    private readonly IValidator<FooRequestDto> _validator;
    private readonly ILogger<FoosController> _logger;

    public FoosController(
        IFooService service,
        IValidator<FooRequestDto> validator,
        ILogger<FoosController> logger)
    {
        _service = service;
        _validator = validator;
        _logger = logger;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FooRequestDto request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors);

        var result = await _service.CreateAsync(GetUserId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
```

---

## Web (Blazor) Skills

> Projeto: `src/MoneyManager.Web`

### O que você precisa saber

| Skill | Descrição |
|---|---|
| Páginas Blazor WASM | Criar `.razor` em `Pages/` com `@page "/rota"` |
| Serviços HTTP | Implementar chamadas em `Services/` — nunca injetar `HttpClient` direto em páginas |
| Autenticação client-side | `CustomAuthenticationStateProvider` gerencia token via `Blazored.LocalStorage` |
| Localização | `ILocalizationService.Get("Chave.Aninhada")` para textos localizados |
| Componentes reutilizáveis | Criar em `Shared/` quando compartilhado entre páginas; `Components/` para componentes de domínio |
| Injeção de dependência | `@inject IFooService FooService` nas páginas |
| Ciclo de vida | Usar `OnInitializedAsync` para carregamento inicial; `StateHasChanged()` para re-render manual |

### Checklist ao adicionar uma nova página

- [ ] Criar `NovaPagina.razor` em `Pages/`
- [ ] Definir a rota com `@page "/nova-pagina"`
- [ ] Adicionar link de navegação em `Shared/NavMenu.razor` se necessário
- [ ] Usar o serviço correspondente (criar se não existir em `Services/`)
- [ ] Adicionar chaves de localização em `wwwroot/i18n/pt-BR.json`
- [ ] Tratar estados: carregando, vazio, erro

### Checklist ao adicionar um novo serviço Web

- [ ] Criar interface `IFooService.cs` (pode ser arquivo separado ou no mesmo arquivo)
- [ ] Implementar `FooService.cs` usando `HttpClient` injetado
- [ ] Registrar em `Program.cs` como `Scoped`
- [ ] Manter alinhamento de rota e shape com o controller da API

### Padrão de serviço Web

```csharp
public interface IFooService
{
    Task<List<FooDto>> GetAllAsync();
    Task<FooDto?> CreateAsync(FooRequestDto request);
}

public class FooService : IFooService
{
    private readonly HttpClient _http;

    public FooService(HttpClient http) => _http = http;

    public async Task<List<FooDto>> GetAllAsync()
    {
        var result = await _http.GetFromJsonAsync<List<FooDto>>("api/foos");
        return result ?? [];
    }
}
```

### Padrão de localização

```json
// pt-BR.json
{
  "Foo": {
    "Title": "Minha Seção",
    "Create": "Criar",
    "Empty": "Nenhum item encontrado"
  }
}
```

```razor
@inject ILocalizationService L

<h1>@L.Get("Foo.Title")</h1>
```

---

## Web.Host Skills

> Projeto: `src/MoneyManager.Web.Host`

### O que você precisa saber

| Skill | Descrição |
|---|---|
| Servidor estático | Serve os arquivos publicados do Blazor WASM |
| MIME types | Configuração para `.wasm`, `.blat`, `.dat` etc. |
| Fallback SPA | Qualquer rota não encontrada retorna `index.html` |
| Dev vs Prod | Em dev aponta para `../MoneyManager.Web/wwwroot`; em prod usa local |

### Quando modificar este projeto

- Apenas ao adicionar suporte a novos tipos MIME ou modificar comportamento de cache.
- Para mudanças de conteúdo, editar o projeto `MoneyManager.Web`.

---

## Worker Skills

> Projeto: `src/MoneyManager.Worker`

### O que você precisa saber

| Skill | Descrição |
|---|---|
| BackgroundService | Hosted services herdam `BackgroundService` e implementam `ExecuteAsync` |
| Separação orquestração/processamento | Hosted service: agenda e dispara. Processor: executa a lógica |
| Options Pattern | Configurações fortemente tipadas via `IOptions<T>` |
| ITimeProvider | Abstrai tempo para testabilidade; usar em vez de `DateTime.UtcNow` diretamente |
| Idempotência | Processos devem ser seguros de re-executar sem efeitos duplicados |
| Cancelamento cooperativo | Verificar `CancellationToken` em loops e operações longas |
| Timeout por execução | Usar `CancellationTokenSource.CreateLinkedTokenSource` com `CancelAfter` |

### Checklist ao adicionar um novo job

- [ ] Criar classe `FooProcessor` com a lógica de negócio
- [ ] Criar classe `FooWorker : BackgroundService` para o agendamento
- [ ] Criar `FooScheduleOptions` com `Hour`, `Minute`, `LoopDelaySeconds`
- [ ] Registrar opções com `.AddOptions<FooScheduleOptions>().Bind(...).ValidateOnStart()`
- [ ] Registrar processor como `Scoped` e worker com `AddHostedService`
- [ ] Adicionar configuração em `appsettings.json`
- [ ] Garantir idempotência do processor

### Padrão de worker

```csharp
internal sealed class FooWorker(
    ILogger<FooWorker> logger,
    IOptions<FooScheduleOptions> scheduleOptions,
    IOptions<WorkerOptions> workerOptions,
    ITimeProvider timeProvider,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // verificar se é hora de executar
            // criar scope ? resolver processor ? executar
            await timeProvider.Delay(TimeSpan.FromSeconds(schedule.LoopDelaySeconds), stoppingToken);
        }
    }
}
```

---

## Tests Skills

> Projeto: `tests/MoneyManager.Tests`

### O que você precisa saber

| Skill | Descrição |
|---|---|
| xUnit | Framework de testes — `[Fact]` para caso único, `[Theory]` para parametrizado |
| NSubstitute | Mocking — `Substitute.For<IInterface>()` |
| Padrão AAA | Arrange / Act / Assert em todo teste |
| Nomenclatura | `MethodName_Scenario_ExpectedResult` |
| Isolamento | Sem dependências externas — tudo mockado |
| Escopo de testes | Testar serviços da camada Application; não testar controllers ou repositórios diretamente |

### Checklist ao adicionar testes para um novo serviço

- [ ] Criar `FooServiceTests.cs` em `tests/MoneyManager.Tests/Application/Services/`
- [ ] Mockar `IUnitOfWork` e dependências via NSubstitute
- [ ] Testar caso de sucesso principal
- [ ] Testar entidade não encontrada (`KeyNotFoundException`)
- [ ] Testar violação de regra de negócio (`InvalidOperationException`)
- [ ] Testar isolamento de usuário (recurso de outro usuário deve lançar exceção)
- [ ] Testar soft delete (registro com `IsDeleted=true` não deve ser retornado)

### Template de teste

```csharp
public class FooServiceTests
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<FooService> _logger;
    private readonly IFooService _service;

    public FooServiceTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<FooService>>();
        _service = new FooService(_uow, _logger);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldReturnDto()
    {
        // Arrange
        var userId = "user123";
        var request = new FooRequestDto { /* ... */ };
        var repo = Substitute.For<IRepository<Foo>>();
        repo.AddAsync(Arg.Any<Foo>()).Returns(x => x.Arg<Foo>());
        _uow.Foos.Returns(repo);

        // Act
        var result = await _service.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        await repo.Received(1).AddAsync(Arg.Any<Foo>());
    }
}
```

---

*Última atualização: consulte o histórico Git para a data exata.*
