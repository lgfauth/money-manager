# 🚀 Quick Start - Railway Deploy

Guia rápido para publicar os serviços do MoneyManager no Railway. Cada serviço tem um arquivo `railway.*.toml` na raiz do repositório com o build já configurado.

| Serviço | Arquivo de config | Build |
|---|---|---|
| API Operacional | `railway.api.toml` (ou `railway.toml`) | `Dockerfile.api` |
| Frontend Web | `railway.web.toml` | `src/Frontends/MoneyManager.Web/Dockerfile` |
| Worker | `railway.worker.toml` | Nixpacks (`dotnet publish` do Worker) |
| API Backoffice | `railway.admin-api.toml` | `Dockerfile.admin-api` |
| Portal Admin | `railway.admin.toml` | `src/Frontends/MoneyManager.Backoffice/Dockerfile` |

## Passo a Passo Rápido

### 1️⃣ Preparar MongoDB (5 minutos)

**MongoDB Atlas (Grátis):**
```
1. Acesse https://www.mongodb.com/cloud/atlas
2. Crie conta gratuita
3. Crie cluster (M0 Free)
4. Database Access → Add User
5. Network Access → Add IP (0.0.0.0/0)
6. Copie a connection string
```

### 2️⃣ Deploy da API Operacional (10 minutos)

```bash
# 1. No Railway:
# - New Project → Deploy from GitHub
# - Selecione o repositório

# 2. Settings → Config-as-code: aponte para railway.api.toml
#    (usa Dockerfile.api e health check em /health)

# 3. Configure as variáveis:
MongoDB__ConnectionString=mongodb+srv://usuario:senha@cluster.mongodb.net
MongoDB__DatabaseName=MoneyManager
Jwt__SecretKey=sua-chave-super-secreta-minimo-32-caracteres
Encryption__Key=<gere com: openssl rand -base64 48>
ALLOWED_ORIGIN=https://seu-web.up.railway.app
ASPNETCORE_ENVIRONMENT=Production

# Integrações (quando usadas):
Anthropic__ApiKey=...
Efi__ClientId=...  Efi__ClientSecret=...  Efi__CertificatePath=...
Vapid__PublicKey=...  Vapid__PrivateKey=...

# 4. A API escuta na porta definida pela variável PORT (o Railway injeta automaticamente; padrão 8080)
# 5. Copie a URL gerada (ex: https://xxx.up.railway.app)
```

> `ALLOWED_ORIGIN` é obrigatória: sem ao menos uma origem de CORS a API não inicia. Aceita várias origens separadas por vírgula.

### 3️⃣ Deploy do Frontend Web (10 minutos)

```bash
# 1. Mesmo projeto Railway → + New → GitHub Repo
# 2. Mesmo repositório, renomeie para "Web"

# 3. Settings → Config-as-code: aponte para railway.web.toml

# 4. Configure a variável usada no build:
API_URL=https://sua-api.up.railway.app
#    O railway.web.toml repassa API_URL como build arg NEXT_PUBLIC_API_URL,
#    que é embutido no bundle do Next.js durante o build.

# 5. O container expõe a porta 3000
```

> Como `NEXT_PUBLIC_API_URL` é definido em tempo de build, alterar `API_URL` exige um novo deploy do Web.

### 4️⃣ Testar (2 minutos)

```bash
# API Health:
curl https://sua-api.up.railway.app/health

# Web App:
https://seu-web.up.railway.app
```

> O Swagger só é habilitado com `ASPNETCORE_ENVIRONMENT=Development`; em produção ele não fica disponível.

## ⚡ Comandos Úteis

```bash
# Ver logs de um serviço
railway logs --service api

# Restart
railway restart --service api

# Variáveis de ambiente
railway variables
```

## 🔧 Troubleshooting Rápido

**API não inicia:**
```bash
# Verifique as variáveis do MongoDB e do CORS
railway variables | grep -E "MongoDB__|ALLOWED_ORIGIN"

# Teste localmente
dotnet run --project src/APIs/MoneyManager.Api.Operational
```

**Web não conecta:**
```bash
# Verifique API_URL (usada no build do Web)
railway variables | grep API_URL

# Confirme que a URL do Web está em ALLOWED_ORIGIN da API
# Teste a API no navegador
https://sua-api.up.railway.app/health
```

**MongoDB erro:**
```bash
# Whitelist all IPs temporariamente
MongoDB Atlas → Network Access → 0.0.0.0/0
```

## ✅ Checklist

- [ ] Código no GitHub
- [ ] MongoDB Atlas configurado
- [ ] Railway projeto criado
- [ ] API deployada (MongoDB, JWT, Encryption e ALLOWED_ORIGIN OK)
- [ ] Web deployada (API_URL OK)
- [ ] Worker deployado (mesmas variáveis de infraestrutura da API)
- [ ] Health check da API OK
- [ ] Web App abrindo
- [ ] Login funcionando

## 🔗 URLs Importantes

Substitua `xxx` com seu domínio Railway:

- 🔌 **API**: `https://xxx-api.up.railway.app`
- ❤️ **Health**: `https://xxx-api.up.railway.app/health`
- 🌐 **Web**: `https://xxx-web.up.railway.app`

## 💡 Dicas

1. **MongoDB**: Use Atlas Free Tier (512MB grátis)
2. **Railway**: confira os créditos e limites do seu plano no painel
3. **Domínio**: Configure custom domain depois
4. **HTTPS**: Railway provê SSL automático (a API já trata os headers `X-Forwarded-*`)
5. **Logs**: Monitore o primeiro deploy

## ⏱️ Tempo Total

- MongoDB setup: 5 min
- API deploy: 10 min
- Web deploy: 10 min
- Testes: 5 min
- **Total: ~30 minutos** ⏱️

## 🆘 Ajuda

Algo não funcionou?

1. Verifique `RAILWAY.md` (na raiz do repositório) para detalhes
2. Veja logs no Railway Dashboard
3. Teste localmente primeiro
4. Connection string do MongoDB correta?
5. `Jwt__SecretKey` tem 32+ caracteres?

---

## 🔐 Worker — variáveis de ambiente obrigatórias

O serviço **Money-manager-worker** precisa das mesmas variáveis de infraestrutura da API. Em especial:

```
MongoDB__ConnectionString=<mesma connection string da API>
MongoDB__DatabaseName=<mesmo banco da API>
Encryption__Key=<MESMO valor configurado na API>
ASPNETCORE_ENVIRONMENT=Production
```

**Importante — `Encryption__Key`:**
- É usada para descriptografar a API key do Banco MCP dos usuários durante o sync bancário (`BankSyncWorker`).
- Precisa ser **idêntica** à da API — chaves diferentes tornam impossível descriptografar os dados já salvos.
- Gere com `openssl rand -base64 48` (somente na primeira vez; depois reutilize o mesmo valor nos dois serviços).
- Sintoma quando ausente: `System.FormatException: The input is not a valid Base-64 string` no log do worker ao iniciar o sync (o placeholder do `appsettings.Production.json` chega ao `AesEncryptionService`).

---

## 🛡️ Backoffice (API + Portal Admin)

**API Backoffice** (`railway.admin-api.toml`):

```
MongoDB__ConnectionString=<mesma connection string da API>
MongoDB__DatabaseName=<mesmo banco da API>
ADMIN_AUTH_SECRET=<segredo próprio, diferente do Jwt__SecretKey>
ADMIN_USERNAME=<usuário do operador>
ADMIN_PASSWORD=<senha forte>
ADMIN_ROLE=admin
ADMIN_PORTAL_ALLOWED_ORIGINS=https://seu-admin.up.railway.app
```

**Portal Admin** (`railway.admin.toml`):

```
ADMIN_API_URL=https://sua-admin-api.up.railway.app
```

`ADMIN_API_URL` é usada pelo proxy server-side (`/api/proxy`) e repassada como build arg `NEXT_PUBLIC_ADMIN_API_URL`.
