# ?? Fix: URL da API no Blazor WebAssembly

## Problema Identificado

O Blazor WebAssembly estava mostrando o erro:
```
AggregateException_ctor_DefaultMessage (net_uri_BadFormat)
```

**Causa:** A aplicação não estava conseguindo ler a URL da API corretamente porque:
1. Blazor WASM roda no **navegador do cliente** (não tem acesso a variáveis de ambiente do servidor)
2. O placeholder `#{API_URL}#` no `appsettings.Production.json` não estava sendo substituído

## ? Solução Implementada

### Arquivos Modificados

1. **`Dockerfile.web`** - Atualizado para usar script de inicialização
2. **`src/MoneyManager.Web/Program.cs`** - Atualizado para ler configuração corretamente
3. **`src/MoneyManager.Web/wwwroot/appsettings.json`** - Criado (novo arquivo)

### O que foi feito

**1. Dockerfile.web** - Agora usa Alpine Linux + script de inicialização:
```dockerfile
# Cria script que substitui #{API_URL}# pelo valor real da variável
COPY <<'EOF' /docker-entrypoint.sh
#!/bin/sh
API_URL=${API_URL:-"https://localhost:5001"}
sed -i "s|#{API_URL}#|$API_URL|g" /usr/share/nginx/html/appsettings.Production.json
exec nginx -g 'daemon off;'
EOF
```

**2. Program.cs** - Carrega configuração do arquivo JSON:
```csharp
// Tenta carregar appsettings.Production.json primeiro
var productionConfig = await http.GetFromJsonAsync<Dictionary<string, string>>("appsettings.Production.json");
if (productionConfig != null && productionConfig.ContainsKey("ApiUrl"))
{
    apiUrl = productionConfig["ApiUrl"];
}
```

**3. appsettings.json** - Arquivo base criado:
```json
{
  "ApiUrl": "https://localhost:5001"
}
```

---

## ?? Como Atualizar no Railway

### Passo 1: Commit e Push

```bash
git add .
git commit -m "fix: configurar URL da API no Blazor WebAssembly"
git push origin main
```

### Passo 2: Verificar Variável no Railway

1. Acesse o serviço **moneymanager-web** no Railway
2. Vá em **Variables**
3. Certifique-se de que existe:

```env
API_URL=https://sua-api.up.railway.app
```

**?? Importante:** Use a URL **COMPLETA** da API, sem barra no final:
```
? Correto:   https://moneymanager-api-production.up.railway.app
? Errado:    https://moneymanager-api-production.up.railway.app/
? Errado:    moneymanager-api-production.up.railway.app
```

### Passo 3: Redeploy

O Railway detectará as mudanças automaticamente e fará o rebuild. Se não:

```bash
# Usando Railway CLI
railway up

# Ou no Dashboard
Railway ? moneymanager-web ? Deployments ? Redeploy
```

---

## ? Verificação

### 1. Verificar Configuração no Browser

Após o deploy, acesse:
```
https://seu-app.up.railway.app/appsettings.Production.json
```

Deve mostrar:
```json
{
  "ApiUrl": "https://sua-api.up.railway.app"
}
```

**NÃO deve ter** `#{API_URL}#` !

### 2. Verificar Console do Navegador

Abra o DevTools (F12) ? Console

Deve aparecer:
```
API URL configurada: https://sua-api.up.railway.app
```

### 3. Testar Aplicação

1. Acesse o site
2. Deve carregar normalmente
3. Tente fazer login/registro
4. Verifique se as chamadas à API funcionam

---

## ?? Se o Problema Persistir

### Verificar Logs do Container

```bash
railway logs
```

Procure por:
```
Configuring Blazor with API_URL: https://...
Updated appsettings.Production.json
```

### Testar Localmente

```bash
# Build da imagem
docker build -f Dockerfile.web -t test-web .

# Rodar com a API URL
docker run -p 8080:8080 \
  -e API_URL="https://sua-api.up.railway.app" \
  test-web

# Acessar
http://localhost:8080
```

### Verificar se o arquivo foi atualizado no container

```bash
# Ver logs do Railway
railway logs

# Deve aparecer:
# Configuring Blazor with API_URL: https://...
# Updated appsettings.Production.json
```

---

## ?? Estrutura de Arquivos Final

```
src/MoneyManager.Web/
??? Program.cs                              ? Atualizado ?
??? wwwroot/
    ??? appsettings.json                    ? Novo ?
    ??? appsettings.Production.json         ? Já existe
        (contém: "ApiUrl": "#{API_URL}#")
```

```
Dockerfile.web                              ? Atualizado ?
```

---

## ?? Checklist de Correção

- [x] `Dockerfile.web` atualizado com script de inicialização
- [x] `Program.cs` atualizado para ler configuração
- [x] `appsettings.json` criado no wwwroot
- [ ] Commit e push das mudanças
- [ ] Variável `API_URL` configurada no Railway
- [ ] Redeploy realizado
- [ ] Aplicação carregando corretamente
- [ ] Login/Registro funcionando

---

## ?? Como Funciona Agora

```
???????????????????????????????????????????????????????
? 1. Container inicia                                 ?
???????????????????????????????????????????????????????
?    docker-entrypoint.sh executa                     ?
?    ?                                                ?
?    Lê variável API_URL do Railway                  ?
?    ?                                                ?
?    Substitui #{API_URL}# no appsettings.Production  ?
?    ?                                                ?
?    Inicia Nginx                                     ?
???????????????????????????????????????????????????????

???????????????????????????????????????????????????????
? 2. Usuário acessa o site                           ?
???????????????????????????????????????????????????????
?    Blazor WASM carrega no navegador                ?
?    ?                                                ?
?    Program.cs executa                               ?
?    ?                                                ?
?    Busca appsettings.Production.json                ?
?    ?                                                ?
?    Lê ApiUrl (já substituído!)                      ?
?    ?                                                ?
?    Configura HttpClient com a URL correta           ?
?    ?                                                ?
?    ? Aplicação funciona!                           ?
???????????????????????????????????????????????????????
```

---

## ?? Próximos Passos

Após corrigir o frontend:
1. Testar login/registro
2. Verificar se há erros na API (mencionado pelo usuário)
3. Configurar CORS se necessário

---

**Criado por:** Equipe MoneyManager  
**Data:** ${new Date().toLocaleDateString('pt-BR')}  
**Status:** ? Solução implementada, aguardando deploy
