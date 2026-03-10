# ?? PASSO A PASSO: Configurar Worker no Railway

## ?? **Problema Atual**
Railway está executando a API em vez do Worker.

## ? **Solução em 5 Passos**

---

### **1?? Acesse o Railway Dashboard**
```
https://railway.app ? Seu Projeto ? Serviço "Worker"
```

---

### **2?? Vá em Settings**
```
Clique na aba "Settings" (ícone de engrenagem)
```

---

### **3?? Procure a seção "Build"**

Você verá algo como:
```
? Custom Build Command
? Custom Start Command
? Dockerfile Path
```

---

### **4?? Configure o Dockerfile**

**Opção A (Melhor):** Se houver campo "Dockerfile Path":
```
Dockerfile Path: Dockerfile.worker
```

**Opção B:** Se não houver, use Custom Build/Start:
```
Custom Build Command:
   docker build -f Dockerfile.worker -t worker . && docker cp $(docker create worker):/app /app

Custom Start Command:
   dotnet /app/MoneyManager.Worker.dll
```

**Opção C (Mais Simples):** Build direto:
```
Build Command:
   dotnet publish src/MoneyManager.Worker/MoneyManager.Worker.csproj -c Release -o /opt/render/project/src/out

Start Command:
   dotnet /opt/render/project/src/out/MoneyManager.Worker.dll
```

---

### **5?? Salvar e Redeploy**

1. Clique em **"Save Changes"** (no final da página)
2. Clique em **"Redeploy"** (botão no topo direito)
3. Aguarde o build (1-2 minutos)

---

## ?? **Verificar se Funcionou**

### **Aba Logs do Worker:**

**? SE APARECER ISTO (errado - é a API):**
```
Now listening on: http://0.0.0.0:8080
Request starting HTTP/1.1 GET /health
```

**? SE APARECER ISTO (correto - é o Worker):**
```
========================================
TransactionSchedulerWorker INICIADO
Agendado para 08:00 (TimeZone: E. South America Standard Time)
========================================
STARTUP EXECUTION: Processando recorrências vencidas imediatamente...
```

---

## ?? **Plano B: Root Directory**

Se não funcionar, tente configurar o **Root Directory**:

Railway ? Worker ? Settings ? **Root Directory**:
```
/ 
```
(barra, indicando raiz do repositório)

Depois **Redeploy**.

---

## ?? **Ainda Não Funciona?**

Se o Railway continuar confundindo, **crie um serviço novo**:

1. Railway Dashboard ? **"+ New Service"**
2. **"GitHub Repo"** ? Selecione `lgfauth/money-manager`
3. Nome: **"MoneyManager-Worker"**
4. Settings:
   - **Dockerfile Path:** `Dockerfile.worker`
   - **Variáveis:** (copie as mesmas da API)
5. Deploy

Depois **delete o serviço Worker antigo**.

---

## ?? **Resumo das Configurações Corretas**

| Campo | Valor |
|-------|-------|
| **Dockerfile Path** | `Dockerfile.worker` |
| **Root Directory** | `/` (raiz) |
| **MongoDB__ConnectionString** | (mesmo da API) |
| **MongoDB__DatabaseName** | `MoneyManager` |
| **Schedule__Hour** | `8` |
| **Schedule__Minute** | `0` |
| **Schedule__TimeZoneId** | `E. South America Standard Time` |
| **Worker__ExecutionTimeoutMinutes** | `5` |

---

## ?? **Resultado Esperado**

Após configurar corretamente, **em 2 minutos** você verá:

1. Build concluído
2. Logs mostrando worker (não API)
3. "STARTUP EXECUTION: Concluída com sucesso"
4. Transações sendo criadas no banco

---

**Dica:** Se tiver dúvida sobre qual configuração o Railway está usando, veja o **"Build Logs"** na primeira execução.
