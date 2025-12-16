# MoneyManager - Deploy no Railway

Este projeto possui duas aplicações separadas que devem ser deployadas no Railway:

1. **API** (MoneyManager.Presentation) - Backend .NET 9 
2. **Web** (MoneyManager.Web) - Frontend Blazor WebAssembly

## ?? Pré-requisitos

- Conta no Railway (https://railway.app)
- MongoDB Atlas ou instância MongoDB (pode usar o plugin do Railway)
- Repositório Git configurado

## ?? Deploy da API

### 1. Criar Novo Projeto no Railway

1. Acesse Railway Dashboard
2. Clique em "New Project"
3. Selecione "Deploy from GitHub repo"
4. Escolha seu repositório MoneyManager

### 2. Configurar Serviço da API

1. No projeto, clique em "+ New"
2. Selecione "GitHub Repo"
3. Configure as variáveis de ambiente:

```
MONGODB_URI=mongodb+srv://seu-usuario:senha@cluster.mongodb.net/
JWT_SECRET_KEY=sua-chave-secreta-super-segura-minimo-32-caracteres
ASPNETCORE_ENVIRONMENT=Production
```

4. Em Settings ? Build:
   - **Build Command**: `docker build -f Dockerfile.api -t moneymanager-api .`
   - **Dockerfile Path**: `Dockerfile.api`
   
5. Em Settings ? Deploy:
   - **Port**: `8080`
   - **Health Check Path**: `/health`

6. Deploy será automático após salvar

### 3. Obter URL da API

Após o deploy, você terá uma URL como:
```
https://moneymanager-api-production.up.railway.app
```

Copie esta URL para usar no deploy do Web.

## ?? Deploy do Web (Blazor WASM)

### 1. Criar Segundo Serviço no Mesmo Projeto

1. No mesmo projeto Railway, clique em "+ New"
2. Selecione "GitHub Repo" (mesmo repositório)
3. Nomeie como "MoneyManager-Web"

### 2. Configurar Serviço Web

1. Configure as variáveis de ambiente:

```
API_URL=https://moneymanager-api-production.up.railway.app
ASPNETCORE_ENVIRONMENT=Production
```

2. Em Settings ? Build:
   - **Build Command**: `docker build -f Dockerfile.web -t moneymanager-web .`
   - **Dockerfile Path**: `Dockerfile.web`
   
3. Em Settings ? Deploy:
   - **Port**: `8080`

### 3. Atualizar Configuração do Web

Antes do build final, atualize o arquivo `src/MoneyManager.Web/wwwroot/appsettings.Production.json`:

```json
{
  "ApiUrl": "https://sua-api-url.railway.app"
}
```

Ou use uma variável de ambiente e script de build para substituir automaticamente.

## ??? MongoDB Setup

### Opção 1: MongoDB Atlas (Recomendado)

1. Crie uma conta em https://www.mongodb.com/cloud/atlas
2. Crie um cluster gratuito
3. Configure acesso de rede (IP 0.0.0.0/0 ou IPs do Railway)
4. Crie um usuário de banco de dados
5. Obtenha a connection string
6. Use como `MONGODB_URI` na API

### Opção 2: Railway MongoDB Plugin

1. No projeto Railway, clique em "+ New"
2. Selecione "Database" ? "MongoDB"
3. Railway criará automaticamente
4. Use a variável `MONGO_URL` gerada

## ?? Variáveis de Ambiente Necessárias

### API (MoneyManager.Presentation)

| Variável | Descrição | Exemplo |
|----------|-----------|---------|
| `MONGODB_URI` | Connection string do MongoDB | `mongodb+srv://...` |
| `JWT_SECRET_KEY` | Chave secreta para JWT (mín 32 chars) | `sua-super-chave-secreta-32-caracteres` |
| `ASPNETCORE_ENVIRONMENT` | Ambiente de execução | `Production` |

### Web (MoneyManager.Web)

| Variável | Descrição | Exemplo |
|----------|-----------|---------|
| `API_URL` | URL da API deployada | `https://moneymanager-api.up.railway.app` |
| `ASPNETCORE_ENVIRONMENT` | Ambiente de execução | `Production` |

## ?? CI/CD Automático

Após configuração inicial, o Railway fará deploy automático quando:

- Push na branch `main`
- Merge de Pull Request
- Commit direto na branch

Para desabilitar auto-deploy:
1. Settings ? Deploy ? Auto Deployments ? Off

## ?? Monitoramento

### Health Checks

- **API**: `https://sua-api.railway.app/health`
- **Web**: `https://seu-web.railway.app` (verifica se carrega)

### Logs

No Railway Dashboard:
1. Clique no serviço
2. Aba "Deployments"
3. Clique no deployment ativo
4. Visualize logs em tempo real

### Métricas

Railway mostra:
- CPU Usage
- Memory Usage
- Network Traffic
- Response Times

## ?? Troubleshooting

### API não inicia

1. Verifique logs no Railway
2. Confirme `MONGODB_URI` está correto
3. Teste connection string localmente
4. Verifique `JWT_SECRET_KEY` tem mínimo 32 caracteres

### Web não conecta na API

1. Verifique `API_URL` está correto
2. Confirme CORS está habilitado na API
3. Teste API no Swagger: `https://sua-api.railway.app`
4. Verifique logs de erro no console do navegador

### MongoDB Connection Failed

1. Verifique IP whitelist no MongoDB Atlas
2. Adicione `0.0.0.0/0` temporariamente para testar
3. Confirme usuário e senha estão corretos
4. Teste connection string com MongoDB Compass

### Build Failures

1. Verifique Dockerfile está na raiz do repositório
2. Confirme paths dos projetos estão corretos
3. Limpe build cache no Railway
4. Reconstrua do zero

## ?? Estrutura de Arquivos

```
MoneyManager/
??? Dockerfile.api           # Dockerfile da API
??? Dockerfile.web           # Dockerfile do Web
??? .dockerignore           # Arquivos ignorados no build
??? railway.api.toml        # Config Railway da API
??? railway.web.toml        # Config Railway do Web
??? src/
?   ??? MoneyManager.Presentation/
?   ?   ??? appsettings.Production.json
?   ??? MoneyManager.Web/
?       ??? wwwroot/
?           ??? appsettings.Production.json
??? RAILWAY.md              # Este arquivo
```

## ?? URLs Finais

Após deploy completo:

- **API**: `https://moneymanager-api-production.up.railway.app`
- **Swagger**: `https://moneymanager-api-production.up.railway.app/swagger`
- **Web**: `https://moneymanager-web-production.up.railway.app`
- **Health Check**: `https://moneymanager-api-production.up.railway.app/health`

## ?? Custos

Railway oferece:
- $5/mês de crédito gratuito
- $0.000231/GB-hora para recursos
- Sem custo para hobby projects (com limites)

Estimativa para este projeto:
- API: ~$3-5/mês
- Web: ~$2-3/mês
- MongoDB Atlas Free Tier: $0

**Total estimado: $5-8/mês** (ou grátis dentro dos limites)

## ?? Suporte

- Railway Docs: https://docs.railway.app
- Railway Discord: https://discord.gg/railway
- MongoDB Docs: https://docs.mongodb.com

## ?? Pronto!

Agora você tem:
- ? API deployada e funcionando
- ? Web app deployada e funcionando  
- ? MongoDB configurado
- ? CI/CD automático
- ? Monitoramento e logs
- ? Health checks ativos

Acesse sua aplicação e comece a usar! ??
