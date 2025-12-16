# ?? Quick Start - Railway Deploy

## ?? Checklist Rápido

### 1?? MongoDB Atlas (5 minutos)

```bash
1. Acesse: https://www.mongodb.com/cloud/atlas
2. Criar conta grátis
3. Criar cluster M0 (Free)
4. Database Access ? Criar usuário
5. Network Access ? Permitir 0.0.0.0/0
6. Connect ? Copiar connection string
```

**Connection String Exemplo:**
```
mongodb+srv://usuario:senha@cluster0.xxxxx.mongodb.net/MoneyAgent?retryWrites=true&w=majority
```

---

### 2?? Railway Setup (10 minutos)

#### Serviço 1: API

```bash
1. Railway ? New Project ? GitHub Repo ? money-manager
2. Nome: moneymanager-api
3. Settings ? Build:
   - Root Directory: /
   - Dockerfile Path: Dockerfile.api
4. Variables:
```

```env
MONGODB__CONNECTIONSTRING=mongodb+srv://usuario:senha@cluster0.xxxxx.mongodb.net/MoneyAgent
MONGODB__DATABASENAME=MoneyAgent
JWT__SECRETKEY=seu-secret-key-muito-seguro-com-pelo-menos-32-caracteres
JWT__ISSUER=MoneyManager
JWT__AUDIENCE=MoneyManagerUsers
JWT__EXPIRATIONHOURS=24
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080
```

```bash
5. Settings ? Networking ? Generate Domain
6. Copiar URL (ex: https://moneymanager-api-production.up.railway.app)
```

#### Serviço 2: Frontend

```bash
1. Railway ? New ? GitHub Repo ? money-manager
2. Nome: moneymanager-web
3. Settings ? Build:
   - Root Directory: /
   - Dockerfile Path: Dockerfile.web
4. Variables:
```

```env
API_URL=https://moneymanager-api-production.up.railway.app
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080
```

```bash
5. Settings ? Networking ? Generate Domain
6. Copiar URL (ex: https://moneymanager-web-production.up.railway.app)
```

---

### 3?? Testar Deploy

**API:**
```bash
curl https://moneymanager-api-production.up.railway.app/health
```

**Frontend:**
```bash
# Abrir no navegador
https://moneymanager-web-production.up.railway.app
```

---

## ?? Comandos Úteis

### Testar Localmente

```bash
# API
docker build -f Dockerfile.api -t moneymanager-api .
docker run -p 8080:8080 \
  -e MONGODB__CONNECTIONSTRING="sua-connection" \
  -e JWT__SECRETKEY="test-key-32-chars-minimum" \
  moneymanager-api

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

# Listar projetos
railway list

# Ver logs
railway logs

# Redeploy
railway up
```

---

## ?? Troubleshooting Rápido

### Problema: API não sobe

```bash
# Verificar logs no Railway
Railway ? moneymanager-api ? Logs

# Checklist:
? MONGODB__CONNECTIONSTRING está correto?
? Porta 8080 configurada?
? Dockerfile.api existe na raiz?
```

### Problema: Frontend não encontra API

```bash
# Verificar no console do navegador (F12)
# Deve mostrar: https://moneymanager-api-production.up.railway.app

# Checklist:
? API_URL configurada?
? API está rodando?
? CORS configurado na API?
```

### Problema: MongoDB não conecta

```bash
# Testar connection string localmente
mongosh "mongodb+srv://usuario:senha@cluster.mongodb.net/MoneyAgent"

# Checklist:
? Usuário e senha corretos?
? 0.0.0.0/0 liberado no Network Access?
? Nome do banco correto (MoneyAgent)?
```

---

## ?? Monitoramento

### Verificar Status

```bash
# Railway Dashboard
- CPU Usage
- Memory Usage
- Request Count
- Response Time
```

### Alertas Importantes

```bash
?? Memory > 500MB ? Considerar upgrade
?? CPU > 80% ? Otimizar código
?? Requests failing ? Verificar logs
```

---

## ?? Custos

### Free Tier
- Railway: $5 crédito/mês (grátis)
- MongoDB Atlas: 512MB (grátis)
- **Total: $0/mês** ?

### Produção
- Railway Hobby: $5/mês
- MongoDB Atlas M2: $9/mês
- **Total: ~$14/mês**

---

## ?? Segurança - Checklist

```bash
? JWT Secret alterado
? CORS configurado
? HTTPS ativo (Railway fornece)
? MongoDB protegido
? Variáveis de ambiente usadas
? .env no .gitignore
```

---

## ?? URLs Finais

Após o deploy:

```
API:      https://moneymanager-api-production.up.railway.app
Swagger:  https://moneymanager-api-production.up.railway.app/swagger
Health:   https://moneymanager-api-production.up.railway.app/health

Frontend: https://moneymanager-web-production.up.railway.app
```

---

## ?? Próximos Passos

1. ? Configurar domínio customizado (opcional)
2. ? Configurar CI/CD com GitHub Actions
3. ? Monitorar logs e métricas
4. ? Fazer backup do MongoDB
5. ? Configurar alertas

---

## ?? Links Úteis

- [Railway Docs](https://docs.railway.app)
- [MongoDB Atlas](https://www.mongodb.com/cloud/atlas)
- [Guia Completo](./RAILWAY_DEPLOYMENT_GUIDE.md)

---

**Tempo total estimado: ~20 minutos** ??

**Dificuldade: ????? (Intermediário)**

---

Criado por: Equipe MoneyManager  
Última atualização: ${new Date().toLocaleDateString('pt-BR')}
