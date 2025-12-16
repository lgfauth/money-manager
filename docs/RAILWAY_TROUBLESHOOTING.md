# ?? Railway Troubleshooting Guide

## ?? Problemas Comuns e Soluções

### 1. Application failed to respond

#### Sintomas
```
Error: Application failed to respond
Health check timeout
```

#### Causas Possíveis
- Porta errada configurada
- Aplicação não está escutando em `0.0.0.0`
- Aplicação demora muito para iniciar

#### Soluções

**1.1 Verificar Porta**
```bash
# Deve ser 8080
ASPNETCORE_URLS=http://0.0.0.0:8080
```

**1.2 Verificar Dockerfile**
```dockerfile
# API
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
```

**1.3 Aumentar Timeout**
No `railway.toml`:
```toml
[deploy]
healthcheckTimeout = 60  # Aumentar para 60s
```

**1.4 Verificar Logs**
```bash
railway logs
# Procurar por:
# - Erros de inicialização
# - "Now listening on: http://[::]:8080"
```

---

### 2. MongoDB Connection Timeout

#### Sintomas
```
MongoConnectionException: Timed out while connecting
```

#### Causas Possíveis
- Connection string incorreta
- IP não liberado no MongoDB Atlas
- Credenciais inválidas

#### Soluções

**2.1 Verificar Connection String**
```bash
# Formato correto:
mongodb+srv://usuario:senha@cluster.mongodb.net/MoneyAgent?retryWrites=true&w=majority

# ?? Caracteres especiais na senha devem ser codificados!
# Exemplo: @ ? %40, # ? %23
```

**2.2 Testar Conexão Localmente**
```bash
# Instalar MongoDB Shell
npm install -g mongosh

# Testar conexão
mongosh "sua-connection-string"
```

**2.3 Verificar Network Access no MongoDB Atlas**
```
1. MongoDB Atlas ? Network Access
2. Adicionar IP Address
3. Selecionar: "Allow Access from Anywhere" (0.0.0.0/0)
4. Confirmar
```

**2.4 Verificar Usuário e Senha**
```
1. MongoDB Atlas ? Database Access
2. Verificar que o usuário existe
3. Role: "Atlas admin" ou "Read and write to any database"
```

---

### 3. CORS Error no Frontend

#### Sintomas
```
Access to fetch at 'https://api...' from origin 'https://web...' 
has been blocked by CORS policy
```

#### Causas Possíveis
- CORS não configurado na API
- Origem não permitida
- Headers incorretos

#### Soluções

**3.1 Verificar Configuração CORS na API**

Em `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Usar CORS ANTES de UseAuthorization
app.UseCors("AllowAll");
```

**3.2 Configuração Específica (Produção)**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
            "https://moneymanager-web-production.up.railway.app",
            "https://*.railway.app"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});
```

**3.3 Verificar Ordem dos Middlewares**
```csharp
app.UseRouting();
app.UseCors("AllowAll");  // ? Deve vir ANTES
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

---

### 4. JWT Authentication Failed

#### Sintomas
```
401 Unauthorized
Bearer token invalid
```

#### Causas Possíveis
- SecretKey diferente entre ambientes
- Token expirado
- Headers incorretos

#### Soluções

**4.1 Verificar SecretKey**
```bash
# API deve ter:
JWT__SECRETKEY=mesma-chave-em-todos-ambientes

# ?? Deve ter no mínimo 32 caracteres!
```

**4.2 Verificar Issuer e Audience**
```env
JWT__ISSUER=MoneyManager
JWT__AUDIENCE=MoneyManagerUsers
```

**4.3 Verificar Token no Frontend**
```javascript
// Console do navegador (F12)
localStorage.getItem('authToken')

// Decodificar em: https://jwt.io
```

**4.4 Logs da API**
```bash
railway logs
# Procurar por:
# - "Token validation failed"
# - "Unauthorized"
```

---

### 5. Static Files 404 (Frontend)

#### Sintomas
```
404 Not Found: /_framework/blazor.webassembly.js
```

#### Causas Possíveis
- Nginx não servindo arquivos corretamente
- Paths incorretos
- Build incompleto

#### Soluções

**5.1 Verificar nginx.conf**
```nginx
location / {
    root /usr/share/nginx/html;
    try_files $uri $uri/ /index.html =404;
}

# MIME types para Blazor
types {
    application/wasm wasm;
}
```

**5.2 Verificar Build**
```bash
# Localmente
cd src/MoneyManager.Web
dotnet publish -c Release

# Verificar se existe: bin/Release/net9.0/publish/wwwroot/
```

**5.3 Dockerfile.web Correto**
```dockerfile
# Copiar arquivos corretos
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
```

---

### 6. High Memory Usage

#### Sintomas
```
Memory usage > 500MB
Railway suspending service
```

#### Causas Possíveis
- Memory leaks
- Muitos objetos em memória
- Logs excessivos

#### Soluções

**6.1 Otimizar Logs**
```json
// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",  // Menos logs
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**6.2 Garbage Collection Agressivo**
```dockerfile
# Dockerfile.api
ENV DOTNET_gcServer=0
ENV DOTNET_GCHeapHardLimit=100000000  # 100MB limit
```

**6.3 Monitorar no Railway**
```bash
railway logs
railway metrics
```

---

### 7. Build Failing

#### Sintomas
```
Build failed
Docker build error
```

#### Causas Possíveis
- Dockerfile incorreto
- Arquivos faltando
- Dependências não restauradas

#### Soluções

**7.1 Testar Build Localmente**
```bash
# API
docker build -f Dockerfile.api -t test-api .
docker run -p 8080:8080 test-api

# Web
docker build -f Dockerfile.web -t test-web .
docker run -p 8080:8080 test-web
```

**7.2 Verificar .dockerignore**
```
# .dockerignore
**/bin/
**/obj/
**/node_modules/
**/.vs/
**/.vscode/
```

**7.3 Verificar Paths no Dockerfile**
```dockerfile
# Devem começar de src/
COPY ["src/MoneyManager.Web/...", "src/MoneyManager.Web/"]
```

---

### 8. Environment Variables Not Working

#### Sintomas
```
Configuration value is null
MongoDB connection string not found
```

#### Causas Possíveis
- Variáveis não definidas
- Nomenclatura incorreta
- Formato errado

#### Soluções

**8.1 Nomenclatura Correta**
```bash
# ASP.NET Core usa __ (dois underscores)
MONGODB__CONNECTIONSTRING=...   # ? Correto
MONGODB_CONNECTIONSTRING=...    # ? Errado

JWT__SECRETKEY=...              # ? Correto
JWT_SECRETKEY=...               # ? Errado
```

**8.2 Verificar no Railway**
```bash
railway variables
# Listar todas as variáveis

railway variables set KEY=VALUE
# Adicionar/Atualizar
```

**8.3 Testar Localmente**
```bash
# Linux/Mac
export MONGODB__CONNECTIONSTRING="..."
dotnet run

# Windows (PowerShell)
$env:MONGODB__CONNECTIONSTRING="..."
dotnet run
```

---

## ?? Comandos de Diagnóstico

### Railway CLI

```bash
# Ver status
railway status

# Ver logs em tempo real
railway logs -f

# Ver variáveis
railway variables

# Ver domínio
railway domain

# Redeploy
railway up

# Abrir no navegador
railway open
```

### Docker Local

```bash
# Build
docker build -f Dockerfile.api -t api .

# Run com variáveis
docker run -p 8080:8080 \
  -e MONGODB__CONNECTIONSTRING="..." \
  -e JWT__SECRETKEY="..." \
  api

# Ver logs
docker logs <container-id>

# Entrar no container
docker exec -it <container-id> /bin/bash
```

---

## ?? Análise de Logs

### Padrões de Erro Comuns

**MongoDB:**
```
? MongoConnectionException
? Authentication failed
? Connection refused
```

**ASP.NET Core:**
```
? Application startup exception
? An unhandled exception occurred
? Failed to bind to address
```

**JWT:**
```
? Token validation failed
? The signature is invalid
? Token expired
```

### Como Interpretar

```bash
# 1. Ver últimas 100 linhas
railway logs --limit 100

# 2. Filtrar erros
railway logs | grep -i "error"

# 3. Filtrar warnings
railway logs | grep -i "warning"

# 4. Seguir em tempo real
railway logs -f
```

---

## ?? Suporte

### Onde Buscar Ajuda

1. **Documentação Oficial:**
   - [Railway Docs](https://docs.railway.app)
   - [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core)

2. **Comunidade:**
   - [Railway Discord](https://discord.gg/railway)
   - [Stack Overflow](https://stackoverflow.com)

3. **Projeto:**
   - GitHub Issues
   - Documentação local

---

## ? Checklist de Deploy

Antes de reportar erro, verifique:

- [ ] MongoDB Atlas configurado e acessível
- [ ] Connection string correta (com senha codificada)
- [ ] IP 0.0.0.0/0 liberado no MongoDB
- [ ] Variáveis de ambiente definidas no Railway
- [ ] JWT SecretKey com mínimo 32 caracteres
- [ ] CORS configurado na API
- [ ] Porta 8080 em todos os lugares
- [ ] Dockerfiles na raiz do projeto
- [ ] Build local funciona
- [ ] Logs não mostram erros críticos

---

**Se o problema persistir:**

1. Faça um redeploy: `railway up`
2. Limpe e reconstrua: Delete o serviço e recrie
3. Teste localmente com Docker
4. Abra uma issue no GitHub com logs

---

Criado por: Equipe MoneyManager  
Última atualização: ${new Date().toLocaleDateString('pt-BR')}
