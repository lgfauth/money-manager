# ?? Quick Start - Railway Deploy

## Passo a Passo R·pido

### 1?? Preparar MongoDB (5 minutos)

**MongoDB Atlas (Gr·tis):**
```
1. Acesse https://www.mongodb.com/cloud/atlas
2. Crie conta gratuita
3. Crie cluster (M0 Free)
4. Database Access ? Add User
5. Network Access ? Add IP (0.0.0.0/0)
6. Copie connection string
```

### 2?? Deploy API (10 minutos)

```bash
# 1. Push cÛdigo para GitHub
git add .
git commit -m "Preparado para Railway"
git push origin main

# 2. No Railway:
# - New Project ? Deploy from GitHub
# - Selecione repositÛrio

# 3. Configure vari·veis:
MONGODB_URI=mongodb+srv://usuario:senha@cluster.mongodb.net/MoneyManager
JWT_SECRET_KEY=sua-chave-super-secreta-minimo-32-caracteres
ASPNETCORE_ENVIRONMENT=Production

# 4. Settings ? Build ? Dockerfile Path:
Dockerfile.api

# 5. Settings ? Deploy ? Port:
8080

# 6. Deploy autom·tico inicia
# 7. Copie a URL gerada (ex: https://xxx.railway.app)
```

### 3?? Deploy Web (10 minutos)

```bash
# 1. Mesmo projeto Railway ? + New ? GitHub Repo
# 2. Mesmo repositÛrio, renomeie para "Web"

# 3. Configure vari·veis:
API_URL=https://sua-api-railway.railway.app
ASPNETCORE_ENVIRONMENT=Production

# 4. Settings ? Build ? Dockerfile Path:
Dockerfile.web

# 5. Settings ? Deploy ? Port:
8080

# 6. Deploy autom·tico inicia
```

### 4?? Testar (2 minutos)

```bash
# API Health:
curl https://sua-api.railway.app/health

# API Swagger:
https://sua-api.railway.app/swagger

# Web App:
https://seu-web.railway.app
```

## ? Comandos ⁄teis

```bash
# Ver logs API
railway logs --service api

# Ver logs Web
railway logs --service web

# Restart API
railway restart --service api

# Vari·veis de ambiente
railway variables
```

## ?? Troubleshooting R·pido

**API n„o inicia:**
```bash
# Verifique MONGODB_URI
railway variables | grep MONGODB

# Teste localmente
dotnet run --project src/MoneyManager.Presentation
```

**Web n„o conecta:**
```bash
# Verifique API_URL
railway variables | grep API_URL

# Teste API no navegador
https://sua-api.railway.app/health
```

**MongoDB erro:**
```bash
# Whitelist all IPs temporariamente
MongoDB Atlas ? Network Access ? 0.0.0.0/0
```

## ?? Checklist

- [ ] CÛdigo no GitHub
- [ ] MongoDB Atlas configurado
- [ ] Railway projeto criado
- [ ] API deployada (vari·veis OK)
- [ ] Web deployada (API_URL OK)
- [ ] Health check API OK
- [ ] Swagger API abrindo
- [ ] Web App abrindo
- [ ] Login funcionando

## ?? URLs Importantes

Substitua `xxx` com seu domÌnio Railway:

- ?? **API**: `https://xxx-api.railway.app`
- ?? **Swagger**: `https://xxx-api.railway.app/swagger`
- ?? **Web**: `https://xxx-web.railway.app`
- ?? **Health**: `https://xxx-api.railway.app/health`

## ?? Dicas

1. **MongoDB**: Use Atlas Free Tier (512MB gr·tis)
2. **Railway**: $5 crÈdito gr·tis/mÍs
3. **DomÌnio**: Configure custom domain depois
4. **HTTPS**: Railway provÍ SSL autom·tico
5. **Logs**: Monitore primeiro deploy

## ?? Tempo Total

- MongoDB setup: 5 min
- API deploy: 10 min
- Web deploy: 10 min
- Testes: 5 min
- **Total: ~30 minutos** ?

## ?? Ajuda

Algo n„o funcionou?

1. Verifique `RAILWAY.md` para detalhes
2. Veja logs no Railway Dashboard
3. Teste localmente primeiro
4. MongoDB connection string correta?
5. JWT_SECRET_KEY tem 32+ caracteres?

---

**Pronto para deploy?** Siga os passos acima! ??

---

## üîê Worker ‚Äî vari√°veis de ambiente obrigat√≥rias

O servi√ßo **Money-manager-worker** precisa das mesmas vari√°veis de infraestrutura da API. Em especial:

```
MongoDB__ConnectionString=<mesma connection string da API>
Encryption__Key=<MESMO valor configurado na API>
ASPNETCORE_ENVIRONMENT=Production
```

**Importante ‚Äî `Encryption__Key`:**
- √â usada para descriptografar a API key do Banco MCP dos usu√°rios durante o sync banc√°rio (`BankSyncWorker`).
- Precisa ser **id√™ntica** √† da API ‚Äî chaves diferentes tornam imposs√≠vel descriptografar os dados j√° salvos.
- Gere com `openssl rand -base64 48` (somente na primeira vez; depois reutilize o mesmo valor nos dois servi√ßos).
- Sintoma quando ausente: `System.FormatException: The input is not a valid Base-64 string` no log do worker ao iniciar o sync (o placeholder do `appsettings.Production.json` chega ao `AesEncryptionService`).
