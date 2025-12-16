# ?? Deploy no Railway - MoneyManager

## Guia Completo de Deploy com Duas Aplicações

Este guia explica como fazer deploy do **MoneyManager** no Railway, que contém:
- ?? **API (Backend)** - MoneyManager.Presentation
- ?? **Frontend** - MoneyManager.Web (Blazor WebAssembly)

---

## ?? Pré-requisitos

1. ? Conta no [Railway.app](https://railway.app)
2. ? Conta no [MongoDB Atlas](https://www.mongodb.com/cloud/atlas) (banco de dados)
3. ? Repositório no GitHub com o código

---

## ??? Estrutura do Projeto

```
money-manager/
??? src/
?   ??? MoneyManager.Presentation/    # API (Backend)
?   ??? MoneyManager.Web/             # Blazor WebAssembly (Frontend)
??? Dockerfile.api                     # Dockerfile para API
??? Dockerfile.web                     # Dockerfile para Web
??? railway.toml                       # Configuração Railway
```

---

## ?? Passo 1: Configurar MongoDB Atlas

### 1.1 Criar Cluster no MongoDB Atlas

1. Acesse [MongoDB Atlas](https://www.mongodb.com/cloud/atlas)
2. Crie uma conta gratuita (Free Tier - M0)
3. Crie um novo cluster:
   - **Nome:** `moneymanager-cluster`
   - **Provider:** AWS
   - **Region:** `us-east-1` (ou mais próxima)
   - **Tier:** M0 Sandbox (GRÁTIS)

### 1.2 Configurar Acesso

1. **Database Access:**
   - Crie um usuário:
     - Username: `moneymanager`
     - Password: Gere uma senha forte
     - Role: `Atlas admin`

2. **Network Access:**
   - Clique em "Add IP Address"
   - Selecione "Allow Access from Anywhere" (`0.0.0.0/0`)
   - (Necessário para o Railway acessar)

### 1.3 Obter Connection String

1. Clique em "Connect" no seu cluster
2. Selecione "Connect your application"
3. Copie a connection string:
   ```
   mongodb+srv://moneymanager:<password>@cluster0.xxxxx.mongodb.net/?retryWrites=true&w=majority
   ```
4. Substitua `<password>` pela senha criada
5. Adicione o nome do banco: `MoneyAgent`

Connection string final:
```
mongodb+srv://moneymanager:SUA_SENHA@cluster0.xxxxx.mongodb.net/MoneyAgent?retryWrites=true&w=majority
```

---

## ?? Passo 2: Configurar Railway

### 2.1 Criar Projeto no Railway

1. Acesse [Railway.app](https://railway.app)
2. Faça login com GitHub
3. Clique em "New Project"
4. Selecione "Deploy from GitHub repo"
5. Escolha o repositório `money-manager`

### 2.2 Criar Dois Serviços

O Railway criará automaticamente um serviço. Você precisará criar dois:

#### **Serviço 1: API (Backend)**

1. No dashboard do Railway, clique em "+ New"
2. Selecione "GitHub Repo"
3. Escolha `money-manager`
4. Nome do serviço: `moneymanager-api`

**Configurações da API:**

1. Clique no serviço `moneymanager-api`
2. Vá em "Settings"
3. Configure:

```yaml
# Build Configuration
Root Directory: /
Dockerfile Path: Dockerfile.api

# Deploy Configuration
Start Command: (deixe vazio, usa o ENTRYPOINT do Dockerfile)

# Health Check
Health Check Path: /health
Health Check Timeout: 30s
```

4. Vá em "Variables" e adicione:

```env
# MongoDB
MONGODB__CONNECTIONSTRING=mongodb+srv://moneymanager:SUA_SENHA@cluster0.xxxxx.mongodb.net/MoneyAgent?retryWrites=true&w=majority
MONGODB__DATABASENAME=MoneyAgent

# JWT Configuration
JWT__SECRETKEY=your-production-secret-key-min-32-chars-change-this-in-production-please
JWT__ISSUER=MoneyManager
JWT__AUDIENCE=MoneyManagerUsers
JWT__EXPIRATIONHOURS=24

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080
```

5. Clique em "Deploy" ou aguarde o deploy automático

#### **Serviço 2: Frontend (Blazor WebAssembly)**

1. Clique em "+ New" novamente
2. Selecione "GitHub Repo"
3. Escolha `money-manager`
4. Nome do serviço: `moneymanager-web`

**Configurações do Frontend:**

1. Clique no serviço `moneymanager-web`
2. Vá em "Settings"
3. Configure:

```yaml
# Build Configuration
Root Directory: /
Dockerfile Path: Dockerfile.web

# Deploy Configuration
Start Command: (deixe vazio)
```

4. Vá em "Variables" e adicione:

```env
# API URL (será preenchida após o deploy da API)
API_URL=https://moneymanager-api-production.up.railway.app

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080
```

---

## ?? Passo 3: Criar Arquivos de Configuração

Você já possui alguns arquivos, mas vamos garantir que todos estão corretos:

### 3.1 Verificar Dockerfile.api (já existe)

O arquivo `Dockerfile.api` já está correto! ?

### 3.2 Criar Dockerfile.web

Crie o arquivo `Dockerfile.web` na raiz do projeto:

```dockerfile
# Build do Blazor WebAssembly
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar arquivos de projeto
COPY ["src/MoneyManager.Web/MoneyManager.Web.csproj", "src/MoneyManager.Web/"]
COPY ["src/MoneyManager.Domain/MoneyManager.Domain.csproj", "src/MoneyManager.Domain/"]

# Restaurar dependências
RUN dotnet restore "src/MoneyManager.Web/MoneyManager.Web.csproj"

# Copiar todo o código
COPY . .

# Publicar
WORKDIR "/src/src/MoneyManager.Web"
RUN dotnet publish "MoneyManager.Web.csproj" -c Release -o /app/publish

# Servir arquivos estáticos com Nginx
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# Copiar arquivos publicados
COPY --from=build /app/publish/wwwroot .

# Copiar configuração do Nginx
COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 8080
```

### 3.3 Criar nginx.conf

Crie o arquivo `nginx.conf` na raiz do projeto:

```nginx
events {
    worker_connections 1024;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;

    server {
        listen 8080;
        server_name localhost;
        root /usr/share/nginx/html;
        index index.html;

        location / {
            try_files $uri $uri/ /index.html =404;
        }

        # Headers de segurança
        add_header X-Frame-Options "SAMEORIGIN" always;
        add_header X-Content-Type-Options "nosniff" always;
        add_header X-XSS-Protection "1; mode=block" always;

        # Cache para assets
        location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
            expires 1y;
            add_header Cache-Control "public, immutable";
        }
    }
}
```

### 3.4 Criar railway.toml (Opcional)

Crie o arquivo `railway.toml` na raiz:

```toml
[build]
builder = "DOCKERFILE"
dockerfilePath = "Dockerfile.api"

[deploy]
startCommand = "dotnet MoneyManager.Presentation.dll"
healthcheckPath = "/health"
healthcheckTimeout = 30
restartPolicyType = "ON_FAILURE"
restartPolicyMaxRetries = 10
```

---

## ?? Passo 4: Ajustar Código para Produção

### 4.1 Atualizar Program.cs da Web

Arquivo: `src/MoneyManager.Web/Program.cs`

Certifique-se de que a URL da API é configurável:

```csharp
// Determinar a URL base da API
var apiUrl = builder.Configuration["ApiUrl"] 
    ?? Environment.GetEnvironmentVariable("API_URL")
    ?? (builder.HostEnvironment.IsDevelopment() 
        ? "https://localhost:5001" 
        : "https://moneymanager-api-production.up.railway.app");

// Configure HttpClient com base address
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(apiUrl) 
});
```

### 4.2 Atualizar appsettings.Production.json

Crie `src/MoneyManager.Presentation/appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "MongoDB": {
    "ConnectionString": "",
    "DatabaseName": "MoneyAgent"
  },
  "Jwt": {
    "SecretKey": "",
    "Issuer": "MoneyManager",
    "Audience": "MoneyManagerUsers",
    "ExpirationHours": 24
  },
  "AllowedHosts": "*"
}
```

(Os valores vazios serão preenchidos pelas variáveis de ambiente)

---

## ?? Passo 5: Conectar as Aplicações

### 5.1 Obter URL da API

1. No Railway, clique no serviço `moneymanager-api`
2. Vá em "Settings" ? "Networking"
3. Clique em "Generate Domain"
4. Copie a URL gerada (ex: `https://moneymanager-api-production.up.railway.app`)

### 5.2 Atualizar Frontend

1. Vá no serviço `moneymanager-web`
2. Em "Variables", atualize:
   ```env
   API_URL=https://moneymanager-api-production.up.railway.app
   ```
3. Clique em "Redeploy"

### 5.3 Configurar CORS na API

Certifique-se de que a API permite requisições do frontend.

No `Program.cs` da API, a configuração já está como `AllowAll`, mas você pode restringir:

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

// No app:
app.UseCors(app.Environment.IsDevelopment() ? "AllowAll" : "Production");
```

---

## ? Passo 6: Testar o Deploy

### 6.1 Verificar API

1. Acesse: `https://moneymanager-api-production.up.railway.app`
2. Deve redirecionar para o Swagger
3. Teste o endpoint `/health`

### 6.2 Verificar Frontend

1. Acesse: `https://moneymanager-web-production.up.railway.app`
2. Teste o login/registro
3. Verifique se as chamadas à API funcionam

---

## ?? Monitoramento

### Logs no Railway

1. Clique em cada serviço
2. Vá na aba "Logs"
3. Monitore erros e avisos

### Métricas

1. Vá em "Metrics" em cada serviço
2. Monitore:
   - CPU Usage
   - Memory Usage
   - Network Traffic

---

## ?? Segurança

### Checklist de Segurança

- ? Alterar `JWT__SECRETKEY` para uma chave forte e única
- ? Configurar CORS adequadamente
- ? Usar HTTPS (Railway fornece automaticamente)
- ? Restringir IP no MongoDB (ou usar rede privada)
- ? Não commitar senhas no código
- ? Usar variáveis de ambiente para secrets

---

## ?? Custos

### Railway Free Tier

- **$5 de crédito mensal** (grátis)
- ~500 horas de execução
- Suficiente para 2 serviços pequenos

### Plano Hobby ($5/mês)

- $5 de crédito + uso adicional
- Melhor para produção

### MongoDB Atlas Free Tier

- **512 MB de armazenamento** (grátis)
- Suficiente para começar

---

## ?? Troubleshooting

### Erro: "Application failed to respond"

**Solução:**
- Verifique se a porta é `8080`
- Confirme `ASPNETCORE_URLS=http://0.0.0.0:8080`

### Erro: "Cannot connect to MongoDB"

**Solução:**
- Verifique a connection string
- Confirme que `0.0.0.0/0` está permitido no Network Access
- Teste a conexão localmente primeiro

### Frontend não encontra a API

**Solução:**
- Verifique a variável `API_URL`
- Confirme que a API está rodando
- Verifique CORS na API

### Build falha

**Solução:**
- Verifique os logs de build no Railway
- Confirme que todos os arquivos estão no Git
- Teste o build localmente: `docker build -f Dockerfile.api .`

---

## ?? Deploy Automático

O Railway detecta mudanças no GitHub automaticamente!

### Configurar Auto-Deploy

1. Em cada serviço, vá em "Settings"
2. Em "Source Repo", verifique:
   - ? "Auto Deploy" está ativado
   - Branch: `main`

Agora, a cada push no GitHub:
1. Railway faz pull do código
2. Builda a aplicação
3. Faz deploy automaticamente

---

## ?? Comandos Úteis

### Testar localmente com Docker

```bash
# API
docker build -f Dockerfile.api -t moneymanager-api .
docker run -p 8080:8080 -e MONGODB__CONNECTIONSTRING="sua-connection" moneymanager-api

# Web
docker build -f Dockerfile.web -t moneymanager-web .
docker run -p 8080:8080 moneymanager-web
```

### Railway CLI

```bash
# Instalar
npm i -g @railway/cli

# Login
railway login

# Ver logs
railway logs

# Variáveis
railway variables
```

---

## ?? Checklist Final

Antes de ir para produção:

- [ ] MongoDB Atlas configurado e testado
- [ ] Variáveis de ambiente definidas no Railway
- [ ] API deployada e funcionando
- [ ] Frontend deployado e funcionando
- [ ] CORS configurado corretamente
- [ ] JWT SecretKey alterada
- [ ] Health checks funcionando
- [ ] Logs sem erros críticos
- [ ] Testes de login/registro funcionando
- [ ] SSL/HTTPS ativo (Railway fornece)

---

## ?? Recursos Adicionais

- [Railway Docs](https://docs.railway.app)
- [MongoDB Atlas Docs](https://www.mongodb.com/docs/atlas)
- [ASP.NET Core Production Best Practices](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/)

---

## ?? Suporte

Se encontrar problemas:

1. Verifique os logs no Railway
2. Teste localmente com Docker
3. Consulte a documentação
4. Entre em contato com a equipe

---

**Boa sorte com o deploy! ??**
