# ?? SOLUÇÃO: Railway Está Subindo a API em Vez do Worker

## Problema Identificado

O log mostra que o Railway está executando a **API** (ASP.NET Core) e não o **Worker**:

```
? Now listening on: http://0.0.0.0:8080  ? Isso é a API
? Request starting HTTP/1.1 GET /health  ? Health check da API
? Falta: TransactionSchedulerWorker INICIADO ? Log esperado do worker
```

---

## ? Solução Rápida (2 minutos)

### **No Railway Dashboard:**

1. **Vá em:** Seu Projeto ? Serviço **Worker** ? **Settings**

2. **Procure:** "Docker" ou "Dockerfile Path"

3. **Configure:**
   ```
   Dockerfile Path: Dockerfile.worker
   ```

4. **Se não houver essa opção, vá em "Build":**
   - **Custom Build Command:**
     ```bash
     docker build -f Dockerfile.worker -t worker .
     ```

5. **Em "Deploy" ? Root Directory:**
   ```
   / 
   ```
   (deixe na raiz do repo)

6. **Salve e clique em "Redeploy"**

---

## ?? Alternativa: Separar os Serviços

Se o Railway continuar confundindo os dois projetos:

### **Opção A: Usar nixpacks.toml**

Crie na raiz:

```toml
[phases.setup]
nixPkgs = ["dotnet-sdk_9"]

[phases.build]
cmds = [
  "dotnet restore src/MoneyManager.Worker/MoneyManager.Worker.csproj",
  "dotnet publish src/MoneyManager.Worker/MoneyManager.Worker.csproj -c Release -o out"
]

[start]
cmd = "dotnet out/MoneyManager.Worker.dll"
```

Salve como `nixpacks.toml` e commit.

### **Opção B: Build Command Explícito**

No Railway ? Worker ? Settings ? Build:

```bash
Build Command:
   cd src/MoneyManager.Worker && dotnet publish -c Release -o /app/out

Start Command:
   dotnet /app/out/MoneyManager.Worker.dll
```

---

## ?? Validação

Após fazer o redeploy, os logs devem mostrar:

```
? TransactionSchedulerWorker INICIADO
? Agendado para 08:00 (TimeZone: E. South America Standard Time)
? STARTUP EXECUTION: Processando recorrências vencidas imediatamente...
```

Se continuar mostrando:
```
? Now listening on: http://0.0.0.0:8080
```

Então o Railway ainda está buildando o projeto errado.

---

## ?? Se Nada Funcionar

**Solução definitiva:** Criar 2 serviços separados no Railway:

1. **Serviço 1:** API
   - Root Directory: `src/MoneyManager.Presentation`
   - Dockerfile: `Dockerfile` (da raiz ou criar específico)

2. **Serviço 2:** Worker
   - Root Directory: `/` (raiz)
   - Dockerfile: `Dockerfile.worker`

Assim o Railway não confunde qual projeto buildar.

---

## ?? Checklist

- [ ] Railway ? Worker ? Settings ? Dockerfile Path = `Dockerfile.worker`
- [ ] OU Railway ? Worker ? Build Command ajustado
- [ ] Variáveis de ambiente configuradas (MongoDB, Schedule, etc.)
- [ ] Redeploy feito
- [ ] Logs mostram "TransactionSchedulerWorker INICIADO"

---

**IMPORTANTE:** O Railway precisa saber que o Worker é um projeto diferente da API. Use o `Dockerfile.worker` que já existe no repo!
