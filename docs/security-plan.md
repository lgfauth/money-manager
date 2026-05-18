# Plano de Segurança — MoneyManager

> **Escopo:** Levantamento completo das vulnerabilidades encontradas na aplicação e plano de correção ordenado por criticidade. Nenhuma mudança de arquitetura ou padrão de desenvolvimento é necessária — todos os ajustes são pontuais nos arquivos existentes.
>
> **Positivos já implementados:** BCrypt com salt automático para hashing de senhas ✅ · Queries MongoDB com `Builders<T>.Filter.Eq()` parametrizadas (sem risco de injection) ✅

---

## Sumário executivo

| Fase | Severidade | Itens | Pré-requisito |
|------|-----------|-------|---------------|
| [Fase 1](#fase-1--crítico) | 🔴 Crítico | 6 | Nenhum |
| [Fase 2](#fase-2--alto) | 🟠 Alto | 7 | Fase 1 concluída |
| [Fase 3](#fase-3--médio) | 🟡 Médio | 8 | Fase 2 concluída |

---

## Fase 1 — Crítico

> Estas correções devem ser aplicadas **antes de qualquer deploy em produção**. São portas de entrada diretas para roubo de identidade, acesso não autorizado e comprometimento total do banco de dados.

---

### 1.1 — Remover fallbacks de secrets JWT hardcoded

**Risco:** Roubo de identidade / autenticação falsa  
**OWASP:** A02 Cryptographic Failures · A07 Identification and Authentication Failures

**Problema:**  
Nos três arquivos abaixo, o código usa o operador `??` para cair em uma chave conhecida publicamente quando a variável de ambiente não está configurada. Qualquer pessoa com acesso ao repositório pode assinar tokens JWT válidos e se passar por qualquer usuário.

```csharp
// TokenService.cs — linha ~20
_secretKey = jwtSettings["SecretKey"] ?? "your-super-secret-key-that-is-long-enough-for-256-bits";

// Program.cs (Operational) — linha ~63
var secretKey = jwtSettings["SecretKey"] ?? "your-super-secret-key-that-is-long-enough-for-256-bits";

// Program.cs (Backoffice) — linha ~38
var adminSecret = Environment.GetEnvironmentVariable("ADMIN_AUTH_SECRET")
    ?? builder.Configuration["AdminAuth:SecretKey"]
    ?? "change-this-admin-secret-key-with-at-least-32-characters";
```

**Correção:**  
Substituir todos os fallbacks por uma exceção que impede a aplicação de subir sem o secret configurado.

```csharp
_secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey não configurada. Defina a variável de ambiente JWT__SecretKey.");
```

**Arquivos:**
- `src/Supports/MoneyManager.Infrastructure/Security/TokenService.cs`
- `src/APIs/MoneyManager.Api.Operational/Program.cs`
- `src/APIs/MoneyManager.Api.Backoffice/Program.cs`

---

### 1.2 — Eliminar o fallback permissivo do CORS

**Risco:** CSRF (Cross-Site Request Forgery) — site malicioso pode fazer requisições autenticadas em nome do usuário  
**OWASP:** A01 Broken Access Control · A05 Security Misconfiguration

**Problema:**  
Quando a variável `AllowedOrigins` não está configurada, ambas as APIs entram em um bloco `else` que permite **qualquer origem** com credenciais habilitadas. A combinação de `SetIsOriginAllowed(_ => true)` com `AllowCredentials()` contorna completamente a proteção do browser contra CSRF.

```csharp
// Program.cs (Operational e Backoffice) — bloco else do CORS
else
{
    // PERIGOSO: qualquer site pode fazer requisições autenticadas
    policy.SetIsOriginAllowed(_ => true)
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials();
}
```

**Correção:**  
Remover o bloco `else` e lançar exceção na startup se as origens não estiverem configuradas.

```csharp
var allowedOrigins = /* leitura das configurações */;

if (allowedOrigins.Length == 0)
    throw new InvalidOperationException(
        "CORS AllowedOrigins não configurado. Defina ALLOWED_ORIGINS ou Cors:AllowedOrigins.");

options.AddPolicy("AppPolicy", policy =>
    policy.WithOrigins(allowedOrigins)
          .WithMethods("GET", "POST", "PUT", "DELETE")
          .WithHeaders("Content-Type", "Authorization")
          .AllowCredentials());
```

**Arquivos:**
- `src/APIs/MoneyManager.Api.Operational/Program.cs`
- `src/APIs/MoneyManager.Api.Backoffice/Program.cs`

---

### 1.3 — Cookie do admin com `HttpOnly = true`

**Risco:** Roubo de token de administrador por XSS  
**OWASP:** A03 Injection (XSS) · A07 Identification and Authentication Failures

**Problema:**  
O cookie `mm_admin_token` é definido com `httpOnly: false`, tornando-o acessível via `document.cookie`. Qualquer XSS na página do backoffice pode exfiltrar o token de administrador. Além disso, o mesmo token é armazenado em `localStorage`, que é sempre acessível por JavaScript.

```typescript
// login/route.ts — linha ~39
redirect.cookies.set("mm_admin_token", data.accessToken, {
    httpOnly: false,  // ← VULNERÁVEL: token acessível por JS
    secure: true,
    sameSite: "lax",
});

// admin-auth.ts — linha ~20
localStorage.setItem(TOKEN_KEY, token);  // ← VULNERÁVEL: sempre acessível por JS
```

**Correção:**

```typescript
// login/route.ts — setar como httpOnly
redirect.cookies.set("mm_admin_token", data.accessToken, {
    httpOnly: true,   // ← JavaScript não consegue ler este cookie
    secure: true,
    sameSite: "strict",
    path: "/",
    maxAge: 60 * 60,
});

// admin-auth.ts — remover a linha do localStorage
// localStorage.setItem(TOKEN_KEY, token);  ← REMOVER
```

**Arquivos:**
- `src/Frontends/MoneyManager.Backoffice/src/app/api/login/route.ts`
- `src/Frontends/MoneyManager.Backoffice/src/lib/admin-auth.ts`

---

### 1.4 — MongoDB sem portas expostas externamente

**Risco:** Acesso direto ao banco de dados por qualquer pessoa na internet  
**OWASP:** A05 Security Misconfiguration · A04 Insecure Design

**Problema:**  
O `docker-compose.yml` expõe o MongoDB (porta 27017) e o mongo-express (porta 8081) para o host e, em ambientes mal configurados, para a internet. O MongoDB não tem autenticação habilitada nesta configuração.

```yaml
# docker-compose.yml
mongodb:
  ports:
    - "27017:27017"   # ← banco de dados acessível externamente

mongo-express:
  ports:
    - "8081:8081"     # ← UI de administração do banco acessível externamente
```

**Correção:**  
Remover os bindings de porta para que os serviços fiquem apenas na rede interna Docker. As APIs se comunicam com o MongoDB pela rede interna sem precisar de porta exposta.

```yaml
# docker-compose.yml — REMOVER as seções ports do mongodb e mongo-express
mongodb:
  # ports:           ← remover ou comentar
  #   - "27017:27017"
  networks:
    - money-manager-network

mongo-express:
  # ports:           ← remover ou comentar
  #   - "8081:8081"
  networks:
    - money-manager-network
```

**Arquivo:** `docker-compose.yml`

> **Nota:** Em produção (Railway), o MongoDB não deve ser exposto via porta pública. Usar a connection string interna da plataforma.

---

### 1.5 — Não vazar `InnerException` nas respostas HTTP

**Risco:** Divulgação de informações internas (connection strings, caminhos de arquivos, versões de libs)  
**OWASP:** A05 Security Misconfiguration · A09 Security Logging and Monitoring Failures

**Problema:**  
O middleware de tratamento de exceções inclui `exception.InnerException?.Message` no corpo da resposta HTTP em todos os ambientes. Em produção, esse campo pode vazar connection strings do MongoDB, caminhos internos do servidor e informações sobre a stack tecnológica.

```csharp
// ApiErrorResponseFactory.cs ou ExceptionHandlingMiddleware.cs
var response = ApiErrorResponseFactory.Create(
    context,
    statusCode,
    exception.Message,
    details: exception.InnerException?.Message);  // ← vaza em produção
```

**Correção:**  
Incluir os detalhes somente em ambiente de desenvolvimento.

```csharp
// Injetar IHostEnvironment no middleware
var details = _environment.IsDevelopment()
    ? exception.InnerException?.Message
    : null;

var response = ApiErrorResponseFactory.Create(context, statusCode, exception.Message, details);
```

**Arquivos:**
- `src/APIs/MoneyManager.Api.Operational/Extensions/ApiErrorResponseFactory.cs`
- Middleware de exceção que chama o factory

---

### 1.6 — Swagger desabilitado em produção

**Risco:** Documentação completa da API exposta para atacantes  
**OWASP:** A05 Security Misconfiguration

**Problema:**  
O Swagger está ativo em todos os ambientes, incluindo produção, com um comentário explícito: `"Enable Swagger in all environments for Railway"`. A rota raiz `/` redireciona para `/swagger`, tornando o inventário completo de endpoints imediatamente acessível.

```csharp
// Program.cs (Operational)
app.UseSwagger();      // ← ativo em produção
app.UseSwaggerUI();    // ← ativo em produção

app.MapGet("/", () => Results.Redirect("/swagger"));  // ← redireciona para o Swagger
```

**Correção:**

```csharp
// Envolver em verificação de ambiente
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}
else
{
    app.MapGet("/", () => Results.Ok(new { status = "healthy" }));
}
```

**Arquivos:**
- `src/APIs/MoneyManager.Api.Operational/Program.cs`
- `src/APIs/MoneyManager.Api.Backoffice/Program.cs`

---

### Checklist de verificação — Fase 1

- [ ] Subir a API sem `JWT__SecretKey` configurado → deve lançar exceção na startup e não subir
- [ ] Chamar a API com header `Origin: http://malicioso.com` sem origens configuradas → API não deve subir
- [ ] Inspecionar cookie `mm_admin_token` via DevTools → coluna `HttpOnly` deve estar marcada
- [ ] `telnet <host> 27017` de fora do Docker → conexão recusada
- [ ] Fazer requisição que causa exception → resposta não deve conter stack trace nem connection string
- [ ] Acessar `/swagger` em produção → 404 ou resposta de health check

---

## Fase 2 — Alto

> Estas correções eliminam vetores de ataque de alto impacto. Devem ser aplicadas logo após a Fase 1.

---

### 2.1 — JWT em cookie `HttpOnly` em vez de corpo da resposta / `sessionStorage`

**Risco:** Roubo de token de usuário por XSS  
**OWASP:** A03 Injection (XSS) · A07 Identification and Authentication Failures

**Problema:**  
O token JWT é retornado no corpo JSON do login e armazenado em `sessionStorage` pelo frontend. Qualquer XSS na aplicação pode ler o `sessionStorage` e exfiltrar o token.

```csharp
// AuthResponseDto.cs
public class AuthResponseDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Token { get; set; }  // ← token exposto no body JSON
}
```

```typescript
// auth-store.ts
sessionStorage.setItem(SESSION_TOKEN_KEY, token);  // ← acessível por JS
```

**Correção:**  
O controller de autenticação deve definir o token via `Set-Cookie` com flags seguras. O frontend deve ler o token do cookie (que o browser envia automaticamente) e não precisa mais armazená-lo.

```csharp
// AuthController.cs — após login bem-sucedido
Response.Cookies.Append("mm_access_token", result.Token, new CookieOptions
{
    HttpOnly = true,
    Secure = true,
    SameSite = SameSiteMode.Strict,
    Expires = DateTimeOffset.UtcNow.AddHours(1)
});

// Remover o campo Token do AuthResponseDto retornado
return Ok(new { result.Id, result.Name, result.Email });
```

```typescript
// auth-store.ts — remover sessionStorage; o cookie é enviado automaticamente
// sessionStorage.setItem(SESSION_TOKEN_KEY, token);  ← REMOVER
```

**Arquivos:**
- `src/APIs/MoneyManager.Api.Operational/Controllers/AuthController.cs`
- `src/Supports/MoneyManager.Application/DTOs/Response/AuthResponseDto.cs`
- `src/Frontends/MoneyManager.Web/src/stores/auth-store.ts`

> **Atenção:** O Blazor WebAssembly precisa de ajuste no `AuthorizationMessageHandler` para ler o token do cookie em vez do estado local. Se a API e o frontend estiverem em domínios diferentes, usar `SameSite=None` com `Secure=true`.

---

### 2.2 — Rate limiting nos endpoints de autenticação

**Risco:** Brute force de senhas / credential stuffing  
**OWASP:** A07 Identification and Authentication Failures

**Problema:**  
Não há nenhum limite de tentativas nos endpoints `/api/auth/login` e `/api/auth/register`. Um atacante pode tentar senhas indefinidamente sem bloqueio.

**Correção:**  
Usar `Microsoft.AspNetCore.RateLimiting` (nativo no .NET 7+, sem novos pacotes).

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", config =>
    {
        config.PermitLimit = 10;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// No pipeline
app.UseRateLimiter();
```

```csharp
// AuthController.cs — decorar os endpoints
[HttpPost("login")]
[EnableRateLimiting("auth")]
public async Task<IActionResult> Login(...)

[HttpPost("register")]
[EnableRateLimiting("auth")]
public async Task<IActionResult> Register(...)
```

**Arquivo:** `src/APIs/MoneyManager.Api.Operational/Program.cs` e `src/APIs/MoneyManager.Api.Operational/Controllers/AuthController.cs`

---

### 2.3 — Middleware de proteção de rotas no frontend Web

**Risco:** Usuários não autenticados acessam páginas protegidas  
**OWASP:** A01 Broken Access Control

**Problema:**  
O frontend principal (`MoneyManager.Web`) não possui `middleware.ts`, enquanto o backoffice já tem. Rotas como `/dashboard`, `/transactions` e `/accounts` são acessíveis sem autenticação.

**Correção:**  
Criar `middleware.ts` na raiz de `src/` do projeto Web.

```typescript
// src/Frontends/MoneyManager.Web/src/middleware.ts
import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

const PUBLIC_ROUTES = ['/login', '/register', '/'];

export function middleware(request: NextRequest) {
    const token = request.cookies.get('mm_access_token');
    const isPublic = PUBLIC_ROUTES.some(r => request.nextUrl.pathname.startsWith(r));

    if (!token && !isPublic) {
        return NextResponse.redirect(new URL('/login', request.url));
    }

    return NextResponse.next();
}

export const config = {
    matcher: ['/((?!_next/static|_next/image|favicon.ico|api/).*)'],
};
```

**Arquivo:** `src/Frontends/MoneyManager.Web/src/middleware.ts` (novo arquivo)

---

### 2.4 — Cabeçalhos de segurança HTTP na API ASP.NET Core

**Risco:** Clickjacking, MIME sniffing, downgrade de HTTPS, XSS sem Content-Security-Policy  
**OWASP:** A05 Security Misconfiguration

**Problema:**  
A API não define cabeçalhos de segurança essenciais: `Strict-Transport-Security`, `Content-Security-Policy`, `X-Content-Type-Options`, `X-Frame-Options`, `Permissions-Policy`.

**Correção:**  
Adicionar middleware inline no pipeline antes dos controllers.

```csharp
// Program.cs — adicionar no pipeline
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains; preload";
    }

    await next();
});

// Ativar HTTPS redirect em produção
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
```

**Arquivo:** `src/APIs/MoneyManager.Api.Operational/Program.cs`

---

### 2.5 — Cabeçalhos de segurança e TLS no nginx

**Risco:** Tráfego em texto puro interceptável (MITM), ausência de HSTS  
**OWASP:** A02 Cryptographic Failures · A05 Security Misconfiguration

**Problema:**  
O `nginx.conf` escuta apenas em HTTP (porta 8080). Os cabeçalhos `Content-Security-Policy`, `Strict-Transport-Security` e `Permissions-Policy` estão ausentes. O header `X-XSS-Protection` presente está obsoleto desde 2019 e pode causar comportamentos inesperados em browsers modernos.

**Correção:**

```nginx
# nginx.conf — substituir/adicionar headers
server {
    listen 8080;

    # Remover — obsoleto e potencialmente problemático
    # add_header X-XSS-Protection "1; mode=block";

    # Manter e adicionar
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header Permissions-Policy "camera=(), microphone=(), geolocation=()" always;
    add_header Content-Security-Policy "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; connect-src 'self' https://money-manager-api.up.railway.app; frame-ancestors 'none';" always;

    # HSTS — ativar somente quando TLS estiver configurado na plataforma (Railway cuida do TLS)
    # add_header Strict-Transport-Security "max-age=63072000; includeSubDomains; preload" always;
}
```

> **Nota sobre TLS:** Em produção no Railway, o TLS é terminado na borda da plataforma antes de chegar ao nginx. O nginx pode continuar em HTTP internamente. O HSTS deve ser configurado na camada de TLS da plataforma ou no header nginx somente após TLS estar ativo de ponta a ponta.

**Arquivo:** `nginx.conf`

---

### 2.6 — `AccountDeletionService`: queries filtradas por `UserId`

**Risco:** Exposição de dados de todos os usuários em memória durante operação de um único usuário  
**OWASP:** A01 Broken Access Control · A04 Insecure Design

**Problema:**  
`AccountDeletionService` chama `GetAllAsync()` em três repositórios (accounts, categories, transactions) e filtra em memória por `userId`. Em uma base com muitos usuários, isso carrega temporariamente dados financeiros de todos em memória.

```csharp
// AccountDeletionService.cs
var accounts = await _unitOfWork.Accounts.GetAllAsync();         // Todos os dados
var categories = await _unitOfWork.Categories.GetAllAsync();     // Todos os dados
var transactions = await _unitOfWork.Transactions.GetAllAsync(); // Todos os dados

var totalCount = accounts.Count(a => a.UserId == userId) + ...  // Filtra depois
```

**Correção:**  
Usar os métodos de repositório filtrados por `userId`.

```csharp
// AccountDeletionService.cs
var accounts = await _unitOfWork.Accounts.GetByUserIdAsync(userId);
var categories = await _unitOfWork.Categories.GetByUserIdAsync(userId);
var transactions = await _unitOfWork.Transactions.GetByUserIdAsync(userId);

var totalCount = accounts.Count + categories.Count + transactions.Count;
```

**Arquivo:** `src/Supports/MoneyManager.Application/Services/AccountDeletionService.cs`

---

### 2.7 — CORS: restringir métodos e headers permitidos

**Risco:** Superfície de ataque desnecessariamente ampla  
**OWASP:** A05 Security Misconfiguration

**Problema:**  
Mesmo quando as origens estão configuradas corretamente, as políticas CORS permitem qualquer método HTTP (`AllowAnyMethod`) e qualquer header (`AllowAnyHeader`).

**Correção:**

```csharp
// Program.cs — tanto Operational quanto Backoffice
policy.WithOrigins(allowedOrigins)
      .WithMethods("GET", "POST", "PUT", "DELETE")
      .WithHeaders("Content-Type", "Authorization")
      .AllowCredentials();
```

**Arquivos:**
- `src/APIs/MoneyManager.Api.Operational/Program.cs`
- `src/APIs/MoneyManager.Api.Backoffice/Program.cs`

---

### Checklist de verificação — Fase 2

- [ ] Fazer login e inspecionar a aba Network → campo `token` não deve aparecer no JSON da resposta
- [ ] Verificar cookie `mm_access_token` → `HttpOnly` e `Secure` marcados, `SameSite: Strict`
- [ ] 15+ tentativas de login inválidas com o mesmo IP → receber `HTTP 429 Too Many Requests`
- [ ] Acessar `/dashboard` sem estar autenticado → redirecionado para `/login`
- [ ] `curl -I https://<host>/api/health` → headers `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy` presentes
- [ ] `AccountDeletionService`: verificar que não há chamada a `GetAllAsync()` sem filtro

---

## Fase 3 — Médio

> Melhorias de postura de segurança que fortalecem a aplicação contra ataques mais sofisticados. Recomendadas antes do primeiro usuário real.

---

### 3.1 — Implementar Refresh Token

**Risco:** Token comprometido válido por horas sem mecanismo de revogação  
**OWASP:** A07 Identification and Authentication Failures

**Problema:**  
Não existe refresh token. Quando o access token expira, o usuário precisa enviar novamente usuário e senha. Tokens comprometidos permanecem válidos até expirar naturalmente.

**Correção:**  
Seguindo o padrão existente (Domain → Application → Infrastructure → Presentation):

```csharp
// Domain — User.cs: adicionar campos
public string? RefreshToken { get; private set; }
public DateTime? RefreshTokenExpiry { get; private set; }

public void SetRefreshToken(string token, DateTime expiry)
{
    RefreshToken = token;
    RefreshTokenExpiry = expiry;
}
```

```csharp
// Application — ITokenService.cs: adicionar método
string GenerateRefreshToken();

// Infrastructure — TokenService.cs: implementar
public string GenerateRefreshToken()
    => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
```

```csharp
// Presentation — AuthController.cs: novo endpoint
[HttpPost("refresh")]
[AllowAnonymous]
public async Task<IActionResult> Refresh()
{
    var refreshToken = Request.Cookies["mm_refresh_token"];
    var result = await _authService.RefreshAsync(refreshToken);
    // ... setar novo access token e refresh token em cookies
}
```

O refresh token é entregue em um segundo cookie `HttpOnly; Secure; SameSite=Strict` com expiração de 7 dias. O access token continua com expiração curta (1h).

---

### 3.2 — Endpoint de Logout com revogação de token

**Risco:** Sessão não pode ser encerrada pelo usuário ou pelo sistema  
**OWASP:** A07 Identification and Authentication Failures

**Problema:**  
Não existe endpoint de logout. Mesmo que o usuário "saia", o token JWT permanece válido até expirar. Se houver comprometimento, não há como invalidar a sessão.

**Correção:**  
Blacklist em memória com `IMemoryCache` (sem novos pacotes).

```csharp
// Application — ITokenBlacklistService.cs (nova interface)
public interface ITokenBlacklistService
{
    void Revoke(string jti, DateTime expiry);
    bool IsRevoked(string jti);
}

// Infrastructure — TokenBlacklistService.cs (implementação)
// Usa IMemoryCache para armazenar JTIs revogados com TTL igual à expiração do token
```

```csharp
// Presentation — AuthController.cs
[HttpPost("logout")]
[Authorize]
public IActionResult Logout()
{
    var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
    var exp = /* extrair expiração do token */;
    _blacklistService.Revoke(jti, exp);

    Response.Cookies.Delete("mm_access_token");
    Response.Cookies.Delete("mm_refresh_token");
    return NoContent();
}
```

> **Pré-requisito:** O claim `jti` (JWT ID) deve ser adicionado ao token em `TokenService.GenerateToken()`.

---

### 3.3 — Reduzir expiração do JWT

**Risco:** Janela de exploração longa para tokens comprometidos  
**OWASP:** A07 Identification and Authentication Failures

**Correção:**

```json
// appsettings.json (Operational) — reduzir de 24h para 1h
"Jwt": {
    "ExpirationHours": 1
}

// appsettings.json (Backoffice) — reduzir de 60min para 15min
"AdminAuth": {
    "TokenExpirationMinutes": 15
}
```

**Arquivos:**
- `src/APIs/MoneyManager.Api.Operational/appsettings.json`
- `src/APIs/MoneyManager.Api.Backoffice/appsettings.json`

---

### 3.4 — Aumentar requisito mínimo de senha

**Risco:** Senhas fracas facilmente quebradas por brute force ou dicionário  
**OWASP:** A07 Identification and Authentication Failures

**Problema:**  
A senha mínima é de 6 caracteres sem regras de complexidade. Isso permite senhas como `123456` ou `abc123`.

**Correção:**

```csharp
// RegisterRequestValidator.cs
RuleFor(x => x.Password)
    .NotEmpty().WithMessage("Senha é obrigatória.")
    .MinimumLength(12).WithMessage("A senha deve ter pelo menos 12 caracteres.")
    .Matches("[A-Z]").WithMessage("A senha deve conter pelo menos uma letra maiúscula.")
    .Matches("[0-9]").WithMessage("A senha deve conter pelo menos um número.")
    .Matches("[^a-zA-Z0-9]").WithMessage("A senha deve conter pelo menos um caractere especial.");
```

**Arquivo:** `src/Supports/MoneyManager.Application/Validators/RegisterRequestValidator.cs`

---

### 3.5 — Validators para DTOs de perfil e configurações

**Risco:** Dados malformados ou ataques enviados para operações sensíveis sem validação  
**OWASP:** A03 Injection · A04 Insecure Design

**Problema:**  
Os seguintes DTOs não possuem validators FluentValidation registrados:

| DTO | Endpoint | Risco |
|-----|----------|-------|
| `ChangePasswordRequestDto` | `POST /profile/change-password` | Senha sem regras de complexidade |
| `UpdateEmailRequestDto` | `PUT /profile/email` | Email malformado aceito |
| `UpdateProfileRequestDto` | `PUT /profile` | Campos vazios ou excessivamente longos |
| `DeleteAccountRequestDto` | `DELETE /account` | Exclusão sem confirmação de senha |

**Correção:**  
Criar um validator por DTO seguindo o padrão existente em `src/Supports/MoneyManager.Application/Validators/`.

```csharp
// ChangePasswordRequestValidator.cs (novo arquivo)
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Senha atual é obrigatória.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Nova senha é obrigatória.")
            .MinimumLength(12).WithMessage("A nova senha deve ter pelo menos 12 caracteres.")
            .Matches("[A-Z]").WithMessage("Deve conter pelo menos uma letra maiúscula.")
            .Matches("[0-9]").WithMessage("Deve conter pelo menos um número.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Deve conter pelo menos um caractere especial.")
            .NotEqual(x => x.CurrentPassword).WithMessage("A nova senha não pode ser igual à atual.");
    }
}
```

**Arquivos:** `src/Supports/MoneyManager.Application/Validators/` (novos arquivos por DTO)

---

### 3.6 — Sanitizar logs: remover credenciais e dados sensíveis

**Risco:** Credenciais expostas em arquivos de log / saída de containers  
**OWASP:** A09 Security Logging and Monitoring Failures

**Problema 1 — AdminTokenService:**  
O log registra o `EffectiveUsername` (usuário admin real) e detalhes de configuração de credenciais, facilitando ataques dirigidos de quem tiver acesso aos logs.

```csharp
// AdminTokenService.cs — linha ~63
_logger.LogInformation(
    "..., EffectiveUsername={EffectiveUsername}, ...",
    expectedUsername,  // ← loga o username real do admin
    ...);
```

**Correção:** Substituir `EffectiveUsername` por um indicador genérico sem o valor real.

**Problema 2 — AccountDeletionService:**  
Usa `Console.WriteLine` em vez de `ILogger<T>`, o que não passa pelo pipeline de logging configurado (NLog), não respeita níveis de log e pode vazar `userId` em produção.

```csharp
// AccountDeletionService.cs
catch (Exception ex)
{
    Console.WriteLine($"Erro ao deletar conta do usuário {userId}: {ex.Message}");
    // ↑ usa Console em vez de ILogger; vaza userId no stdout
}
```

**Correção:** Substituir por `_logger.LogError(ex, "Erro ao deletar conta. UserId={UserId}", userId)`.

**Arquivos:**
- `src/APIs/MoneyManager.Api.Backoffice/Services/AdminTokenService.cs`
- `src/Supports/MoneyManager.Application/Services/AccountDeletionService.cs`

---

### 3.7 — Containers Docker executando como usuário não-root

**Risco:** Escape de container compromete o host  
**OWASP:** A05 Security Misconfiguration

**Problema:**  
Nenhum dos Dockerfiles define um usuário não-root com a diretiva `USER`. Processos internos rodam como root, o que amplia o impacto de uma exploração.

**Correção:**  
Adicionar as instruções abaixo em cada Dockerfile, antes do `ENTRYPOINT`.

```dockerfile
# Dockerfile.api, Dockerfile.web, Dockerfile.admin-api, Dockerfile.worker
RUN addgroup --gid 1000 appgroup && \
    adduser --disabled-password --gecos "" --uid 1000 --ingroup appgroup appuser

USER appuser
```

**Arquivos:** `Dockerfile.api`, `Dockerfile.web`, `Dockerfile.admin-api`, `Dockerfile.worker`

---

### 3.8 — Connection string do MongoDB somente via variável de ambiente

**Risco:** Connection string real versionada no repositório  
**OWASP:** A02 Cryptographic Failures · A05 Security Misconfiguration

**Problema:**  
Os `appsettings.json` de ambas as APIs contêm a chave `ConnectionString` com valores placeholder. O risco real é de uma connection string de produção ser acidentalmente commitada neste campo.

**Correção:**  
Garantir que o campo no JSON sempre seja vazio ou conste um placeholder explícito, e documentar que o valor real vem exclusivamente da variável de ambiente `MONGODB__CONNECTIONSTRING`.

```json
// appsettings.json — placeholder explícito
"MongoDB": {
    "ConnectionString": "",
    "DatabaseName": "MoneyAgent"
}
```

Adicionar validação na startup:

```csharp
// Program.cs
var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"];
if (string.IsNullOrWhiteSpace(mongoConnectionString))
    throw new InvalidOperationException(
        "MongoDB ConnectionString não configurada. Defina MONGODB__CONNECTIONSTRING.");
```

**Arquivos:**
- `src/APIs/MoneyManager.Api.Operational/appsettings.json`
- `src/APIs/MoneyManager.Api.Backoffice/appsettings.json`
- `src/APIs/MoneyManager.Api.Operational/Program.cs`

---

### Checklist de verificação — Fase 3

- [ ] Registrar com senha `abc12345` (8 chars, sem maiúscula nem símbolo) → `HTTP 400` com mensagem de validação
- [ ] Fazer login, chamar `POST /api/auth/logout`, usar o mesmo token → `HTTP 401`
- [ ] Chamar `POST /profile/change-password` sem `NewPassword` → `HTTP 400`
- [ ] `docker inspect <container>` → campo `User` não deve ser vazio ou `root`
- [ ] Verificar logs de tentativa de login admin fracassada → não deve conter o username tentado

---

## Referências

| Padrão | Relevância |
|--------|-----------|
| [OWASP Top 10 2021](https://owasp.org/Top10/) | Base do levantamento |
| [OWASP ASVS 4.0](https://owasp.org/www-project-application-security-verification-standard/) | Verificação de autenticação e sessão |
| [NIST SP 800-63B](https://pages.nist.gov/800-63-3/sp800-63b.html) | Requisitos de senha e autenticação |
| [RFC 6749 — OAuth 2.0](https://www.rfc-editor.org/rfc/rfc6749) | Referência para Refresh Token |
| [MDN — Set-Cookie](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Set-Cookie) | Flags de cookie seguro |
