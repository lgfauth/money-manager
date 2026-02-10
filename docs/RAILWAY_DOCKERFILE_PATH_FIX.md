# ?? SOLUÇÃO DEFINITIVA: Railway Pegando Arquivo Errado

## ? Problema Identificado

Railway está lendo `railway.toml` (da API) para AMBOS os serviços:
- ? API ? usa `railway.toml` corretamente ? `Dockerfile.api`
- ? Worker ? também usa `railway.toml` ? **ERRADO!** ? Executa API

O Railway **não reconhece** `railway.worker.toml` automaticamente.

---

## ? SOLUÇÃO FINAL (2 minutos)

### **Configure o Worker para IGNORAR railway.toml**

**No Railway Dashboard:**

```
1. Projeto ? Serviço "Worker"
2. Settings
3. Procure "Source" ou "Deploy"
4. Configure:

   ???????????????????????????????????????
   ? Dockerfile Path:                    ?
   ? Dockerfile.worker                   ?
   ???????????????????????????????????????

5. Salve
6. Delete o railway.worker.toml (não é usado)
7. Redeploy
```

Isso faz o Worker **ignorar completamente** o `railway.toml` e usar só o `Dockerfile.worker`.

---

## ?? Configuração Final dos Serviços

### **API (MoneyManager.Presentation)**
```
Root Directory: /
Dockerfile Path: Dockerfile.api
railway.toml: ? Usado
Start Command: (definido no railway.toml)
```

### **Worker (MoneyManager.Worker)**
```
Root Directory: /
Dockerfile Path: Dockerfile.worker
railway.toml: ? IGNORADO
Start Command: (definido no Dockerfile.worker)
Variables: MongoDB__, Schedule__, Worker__
```

---

## ?? Passos Detalhados

### **1. Railway ? Worker ? Settings ? Build**

Procure por um desses campos:
- "Dockerfile Path"
- "Custom Build"
- "Build Configuration"

**Configure:**
```
Dockerfile Path: Dockerfile.worker
```

### **2. (Opcional) Desabilitar railway.toml**

Se houver opção "Use railway.toml":
```
? Use railway.toml for configuration  [DESMARCADO]
```

### **3. Variables (Garantir que estão configuradas)**

```env
MongoDB__ConnectionString=<valor_da_api>
MongoDB__DatabaseName=MoneyManager
Schedule__Hour=8
Schedule__Minute=0
Schedule__TimeZoneId=E. South America Standard Time
Schedule__LoopDelaySeconds=30
Worker__ExecutionTimeoutMinutes=5
```

### **4. Redeploy**

Clique em "Redeploy" no topo direito.

---

## ? Validação

### **Logs da API (não devem mudar):**
```
? Now listening on: http://0.0.0.0:8080
? Request starting HTTP/1.1 GET /health
```

### **Logs do Worker (DEVEM aparecer):**
```
? ========================================
? TransactionSchedulerWorker INICIADO
? Agendado para 08:00 (TimeZone: E. South America Standard Time)
? STARTUP EXECUTION: Processando recorrências vencidas imediatamente...
? Found X due recurring transactions to process
? STARTUP EXECUTION: Concluída com sucesso
```

---

## ??? Limpeza (Após Funcionar)

Depois que o Worker estiver funcionando:

```sh
# Pode deletar o railway.worker.toml (não é usado)
git rm railway.worker.toml
git commit -m "chore: remove unused railway.worker.toml"
git push
```

O Railway não precisa dele se você configurar via Dashboard.

---

## ?? Se o Campo "Dockerfile Path" Não Existir

**Alternativa:** Use Custom Start Command

```
Build Command:
   docker build -f Dockerfile.worker -t worker .

Start Command:
   dotnet MoneyManager.Worker.dll
```

---

## ?? Como Deve Ficar no Railway

```
????????????????????????????????????????????
?  Worker Service - Settings               ?
????????????????????????????????????????????
?  Build                                   ?
?  ?? Builder: Dockerfile                  ?
?  ?? Dockerfile Path: Dockerfile.worker   ?
?                                          ?
?  Deploy                                  ?
?  ?? Start Command: (from Dockerfile)     ?
?  ?? Healthcheck: (none needed)           ?
?                                          ?
?  Variables                               ?
?  ?? MongoDB__ConnectionString            ?
?  ?? MongoDB__DatabaseName                ?
?  ?? Schedule__Hour                       ?
?  ?? Schedule__Minute                     ?
?  ?? Schedule__TimeZoneId                 ?
?  ?? Worker__ExecutionTimeoutMinutes      ?
????????????????????????????????????????????
```

---

## ?? Resultado Final

Após configurar:

**API:**
- Continua usando `railway.toml`
- Executa `Dockerfile.api`
- ? Funciona normalmente

**Worker:**
- **IGNORA** `railway.toml`
- Executa `Dockerfile.worker`
- ? Sobe o worker corretamente
- ? Processa recorrências na inicialização
- ? Agenda para 08:00 todos os dias

---

**TL;DR:** Configure "Dockerfile Path: Dockerfile.worker" no Dashboard do Railway para o serviço Worker. Ele vai ignorar o railway.toml e usar o Dockerfile correto.
