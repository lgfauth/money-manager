# ?? SOLUÇÃO FINAL - URL da API no Blazor WASM

## ? Problema Resolvido

O erro `net_uri_BadFormat` foi causado porque o Blazor WebAssembly não conseguia carregar a URL da API antes de inicializar.

## ?? Solução Implementada

**Abordagem:** Injetar a URL da API diretamente no `index.html` durante a inicialização do container.

### Arquivos Modificados:

1. ? **`src/MoneyManager.Web/Program.cs`** - Simplificado para ler de JS
2. ? **`src/MoneyManager.Web/wwwroot/index.html`** - Placeholder adicionado
3. ? **`Dockerfile.web`** - Script que substitui placeholder

---

## ?? Mudanças Detalhadas

### 1. index.html - Placeholder JavaScript

Adicionado antes dos scripts do Blazor:

```html
<!-- Configuration - API URL injected at runtime -->
<script>
    window.blazorConfig = {
        apiUrl: '__API_URL__'
    };
</script>
```

### 2. Program.cs - Leitura Simplificada

```csharp
// Build temporário para obter JSRuntime
var tempHost = builder.Build();
var jsRuntime = tempHost.Services.GetRequiredService<IJSRuntime>();

// Ler da variável JS injetada
var apiUrl = "https://localhost:5001"; // Default
try
{
    var configApiUrl = await jsRuntime.InvokeAsync<string>("eval", "window.blazorConfig?.apiUrl || ''");
    if (!string.IsNullOrEmpty(configApiUrl) && configApiUrl != "__API_URL__")
    {
        apiUrl = configApiUrl;
    }
}
catch { /* usa default */ }
```

### 3. Dockerfile.web - Substituição em Runtime

O script de inicialização agora:
```sh
sed -i "s|__API_URL__|$API_URL|g" /usr/share/nginx/html/index.html
```

---

## ?? Deploy no Railway

### Passo 1: Commit e Push

```bash
git add .
git commit -m "fix: injetar URL da API no index.html"
git push origin main
```

### Passo 2: Configurar Variável (Se ainda não fez)

No Railway ? **moneymanager-web** ? **Variables**:

```env
API_URL=https://moneymanager-api-production.up.railway.app
```

?? **Importante:** Use a URL completa da sua API!

### Passo 3: Aguardar Deploy

O Railway fará o rebuild automaticamente (~3-5 minutos).

---

## ? Verificação

### 1. Ver Logs do Container

No Railway ? **moneymanager-web** ? **Logs**

Deve aparecer:
```
================================
Configuring Blazor WebAssembly
API_URL: https://moneymanager-api-production.up.railway.app
================================
Updated index.html
Starting nginx...
```

### 2. Inspecionar index.html

Acesse no navegador:
```
view-source:https://seu-app.up.railway.app
```

Procure por:
```html
<script>
    window.blazorConfig = {
        apiUrl: 'https://moneymanager-api-production.up.railway.app'
    };
</script>
```

**NÃO deve ter** `__API_URL__`!

### 3. Console do Navegador

Pressione **F12** ? **Console**

Deve aparecer:
```
[MoneyManager] API URL configurada: https://moneymanager-api-production.up.railway.app
```

### 4. Testar Aplicação

1. O site deve carregar normalmente
2. Não mais erro de `net_uri_BadFormat`
3. Login/Registro devem funcionar (se a API estiver OK)

---

## ?? Como Funciona

```
?????????????????????????????????????????
? 1. Container Inicia                   ?
?????????????????????????????????????????
?   docker-entrypoint.sh executa        ?
?   ?                                   ?
?   Lê $API_URL do Railway              ?
?   ?                                   ?
?   Substitui __API_URL__ no index.html ?
?   ?                                   ?
?   Inicia Nginx                        ?
?????????????????????????????????????????

?????????????????????????????????????????
? 2. Usuário Acessa o Site              ?
?????????????????????????????????????????
?   Navegador carrega index.html        ?
?   ?                                   ?
?   JavaScript define window.blazorConfig ?
?   ?                                   ?
?   Blazor WASM inicia                  ?
?   ?                                   ?
?   Program.cs lê window.blazorConfig   ?
?   ?                                   ?
?   HttpClient configurado com URL      ?
?   ?                                   ?
?   ? Aplicação funciona!              ?
?????????????????????????????????????????
```

---

## ?? Se o Problema Persistir

### Verificar Substituição

1. Ver logs do container (deve mostrar "Updated index.html")
2. Inspecionar código-fonte no navegador
3. Verificar console do navegador

### Testar Localmente

```bash
# Build
docker build -f Dockerfile.web -t test-web .

# Run
docker run -p 8080:8080 -e API_URL="https://sua-api.url" test-web

# Acessar
http://localhost:8080
```

### Forçar Rebuild

Se o Railway não detectou as mudanças:

```bash
railway up
```

Ou no Dashboard:
```
Railway ? moneymanager-web ? Deployments ? Redeploy
```

---

## ?? Checklist Final

- [x] `index.html` com placeholder `__API_URL__`
- [x] `Program.cs` lendo de `window.blazorConfig`
- [x] `Dockerfile.web` substituindo placeholder
- [ ] Commit e push realizados
- [ ] Deploy automático concluído
- [ ] Logs mostram configuração correta
- [ ] Site carrega sem erros
- [ ] Console mostra URL correta

---

## ?? Próximos Passos

Após corrigir o frontend:

1. ? Verificar se a **API também está funcionando**
2. ? Testar **Login/Registro**
3. ? Verificar **CORS** se houver erro de conexão
4. ? Testar funcionalidades principais

---

## ?? Vantagens Desta Solução

? **Simples:** Apenas um placeholder no HTML  
? **Confiável:** Substituição em runtime  
? **Flexível:** Fácil mudar a URL sem rebuild  
? **Visível:** Logs mostram o que está acontecendo  
? **Testável:** Fácil verificar se funcionou  

---

**Status:** ? Solução implementada e pronta para deploy  
**Data:** ${new Date().toLocaleDateString('pt-BR')}  
**Versão:** 2.0 (Solução Final)

---

**FAÇA O COMMIT E PUSH AGORA! ??**
