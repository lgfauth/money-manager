# ? CORREÇÃO COMPLETA: Página de Accounts Não Carrega

## ?? PROBLEMA
Página `/accounts` ficava totalmente em branco no Railway (produção).

## ?? CAUSA
Arquivos estáticos do Blazor WebAssembly (`wwwroot/`) não eram copiados durante o publish.

## ? SOLUÇÃO APLICADA

### **1. Arquivos Modificados:**
- ? `src/MoneyManager.Web.Host/MoneyManager.Web.Host.csproj`
- ? `src/MoneyManager.Web.Host/Program.cs`

### **2. O que foi feito:**
- **MSBuild Targets:** Copia automática do `wwwroot` após build e publish
- **Program.cs:** Lógica para usar wwwroot local em produção e relativo em dev
- **Logs detalhados:** Para debug de problemas de caminho

---

## ?? COMO FAZER DEPLOY

### **Opção 1: Script Automático (RECOMENDADO)**

**Windows (PowerShell):**
```powershell
.\deploy.ps1
```

**Linux/Mac (Bash):**
```bash
chmod +x deploy.sh
./deploy.sh
```

### **Opção 2: Manual**

```bash
# 1. Limpar
dotnet clean

# 2. Build Blazor
cd src/MoneyManager.Web
dotnet build -c Release
cd ../..

# 3. Publish Host
cd src/MoneyManager.Web.Host
dotnet publish -c Release -o ./publish

# 4. Verificar wwwroot foi copiado
ls ./publish/wwwroot

# 5. Commit e push
git add .
git commit -m "fix: copy Blazor wwwroot to Web.Host on publish"
git push origin main
```

---

## ?? VALIDAÇÃO

### **Local:**
```bash
cd src/MoneyManager.Web.Host/publish
dotnet MoneyManager.Web.Host.dll

# Abrir http://localhost:5000/accounts
# Deve carregar sem erros 404
```

### **Produção (Railway):**

**Logs esperados:**
```
[PROD] Usando wwwroot local: /app/wwwroot
? Diretório wwwroot encontrado: /app/wwwroot
? Pasta _framework encontrada
? index.html encontrado

GET /i18n/pt-BR.json ? 200 ?
GET /_framework/blazor.webassembly.js ? 200 ?
```

**Console do navegador (F12):**
- ? Sem erros 404
- ? Página renderiza

---

## ?? DOCUMENTAÇÃO COMPLETA

Ver: `docs/FIX_ACCOUNTS_PAGE_404.md`

---

## ? STATUS

**Compilação:** ? Sucesso  
**Targets MSBuild:** ? Funcionando  
**Scripts de Deploy:** ? Criados  
**Documentação:** ? Completa  
**Pronto para Deploy:** ? SIM

---

## ?? PRÓXIMO PASSO

**Executar:**
```powershell
.\deploy.ps1
```

Ou simplesmente:
```bash
git add .
git commit -m "fix: copy Blazor wwwroot to Web.Host on publish"
git push origin main
```

**Railway fará deploy automático e a página /accounts vai funcionar! ??**
