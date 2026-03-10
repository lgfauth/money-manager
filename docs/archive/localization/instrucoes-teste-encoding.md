# ?? Instruções de Teste - Correção de Encoding

## ?? IMPORTANTE: Limpar Cache Antes de Testar

O navegador pode ter armazenado em cache as versões antigas dos arquivos. **OBRIGATÓRIO** limpar o cache:

### Chrome/Edge:
```
Ctrl + Shift + Del
? Selecionar "Imagens e arquivos em cache"
? Clicar "Limpar dados"
```

### Firefox:
```
Ctrl + Shift + Del
? Selecionar "Cache"
? Clicar "Limpar agora"
```

### Alternativa: Modo Anônimo/Privado
```
Ctrl + Shift + N (Chrome/Edge)
Ctrl + Shift + P (Firefox)
```

---

## ?? Passo a Passo de Teste

### 1. Recompilar o Projeto

```bash
# Limpar build anterior
dotnet clean

# Recompilar tudo
dotnet build
```

### 2. Executar o Web.Host

```bash
cd src/MoneyManager.Web.Host
dotnet run
```

Aguarde até ver:
```
? Diretório wwwroot encontrado
? Pasta _framework encontrada
? index.html encontrado
```

### 3. Abrir no Navegador

```
http://localhost:5000
```

---

## ? Checklist de Testes

### Teste 1: Tela de Loading (ANTES do Blazor)

**O que verificar**:
- [ ] Texto "Carregando aplicação..." (com ç e ã corretos)
- [ ] Spinner funcionando
- [ ] Título "MoneyManager" visível

**Resultado esperado**:
```
MoneyManager
Carregando aplicação...
```

**? Se aparecer**: "Carregando aplica??o..." ? Cache do navegador não foi limpo!

---

### Teste 2: Página de Login

**O que verificar**:
- [ ] Título "MoneyManager"
- [ ] Subtítulo "Faça login na sua conta" (com ç)
- [ ] Campos "Email" e "Senha"
- [ ] Botão "Entrar"
- [ ] Link "Não tem conta? Criar nova conta" (com ã)

**Resultado esperado**:
```
MoneyManager
Faça login na sua conta
```

---

### Teste 3: Menu de Usuário (APÓS LOGIN)

**Pré-requisito**: Fazer login ou criar conta

**O que verificar**:
1. **No botão do menu** (canto superior direito):
   - [ ] Deve mostrar "Usuário" ou nome do usuário (com á)

2. **Ao clicar no menu dropdown**:
   - [ ] "Meu Perfil"
   - [ ] "Configurações" (com õ)
   - [ ] "Sair"

**Resultado esperado**:
```
?? Usuário ?
   - Meu Perfil
   - Configurações
   - Sair
```

**? Se aparecer**: "Usu?rio" ou "Configura??es" ? **PROBLEMA PERSISTE**

---

### Teste 4: Menu de Navegação

**O que verificar**:
- [ ] Dashboard
- [ ] Categorias
- [ ] Contas
- [ ] Transações (com õ)
- [ ] Recorrentes
- [ ] Orçamentos (com ç)
- [ ] Relatórios (com ó)

---

### Teste 5: Seletor de Idioma

**O que verificar**:
1. Clicar no seletor de idioma (ícone de bandeira/globo)
2. Verificar opções:
   - [ ] "Português" (com ê)
   - [ ] "English"
   - [ ] "Español" (com ñ)

3. Trocar para "English" e verificar se muda
4. Voltar para "Português"

---

### Teste 6: Dashboard (Após Login)

**O que verificar**:
- [ ] "Dashboard Financeiro"
- [ ] "Visão geral das suas finanças" (com ã)
- [ ] "SALDO LÍQUIDO" (com Í, Í)
- [ ] "PATRIMÔNIO TOTAL" (com Ô)
- [ ] "Receitas do Mês" (com ê)
- [ ] "Despesas do Mês" (com ê)
- [ ] "Orçamento Utilizado" (com ç)

---

### Teste 7: Página de Transações

**O que verificar**:
- [ ] "Transações" (com õ)
- [ ] "Nova Transação" (com ã)
- [ ] "Descrição" (com ç, ã)
- [ ] "Categoria"
- [ ] Botões "Editar" e "Deletar"

---

### Teste 8: Console do Navegador

**Importante**: Abrir DevTools (F12) e verificar console

**O que procurar**:
```
[LocalizationService] BaseAddress: http://localhost:5000/
[LocalizationService] Carregando: i18n/pt-BR.json
[LocalizationService] ? Carregado 13 seções
[LocalizationService] ? Teste Login.Title = MoneyManager
```

**? Se aparecer**:
```
[LocalizationService] ? Erro ao carregar
```
? Verificar se arquivo `pt-BR.json` existe em `wwwroot/i18n/`

---

## ?? Debug de Problemas

### Problema 1: Ainda aparece "?" no menu

**Solução**:
1. Limpar cache do navegador novamente (Ctrl+Shift+Del)
2. Fechar completamente o navegador
3. Abrir em modo anônimo
4. Testar novamente

### Problema 2: Tela de loading OK, mas menu errado

**Causa**: O arquivo `pt-BR.json` está com encoding incorreto

**Solução**:
```powershell
cd src/MoneyManager.Web/wwwroot/i18n/
$content = Get-Content "pt-BR.json" -Raw -Encoding UTF8
$content | Out-File "pt-BR.json" -Encoding UTF8 -NoNewline
```

### Problema 3: Console mostra erro 404 no pt-BR.json

**Causa**: Arquivo não está sendo copiado para wwwroot

**Solução**:
```bash
# Verificar se arquivo existe
ls src/MoneyManager.Web/wwwroot/i18n/

# Se não existir, criar o diretório
mkdir src/MoneyManager.Web/wwwroot/i18n/

# Executar build novamente
dotnet build
```

### Problema 4: Funciona em dev, mas não em produção

**Causa**: Arquivo não foi incluído no publish

**Solução**:
```bash
# Fazer publish explícito
dotnet publish -c Release

# Verificar se JSON está no output
ls src/MoneyManager.Web.Host/bin/Release/net9.0/publish/wwwroot/i18n/
```

---

## ?? Matriz de Testes

| Local | Caractere | Deve Aparecer | ? Errado |
|-------|-----------|---------------|-----------|
| Loading | ção | aplicação | aplica??o |
| Menu | á | Usuário | Usu?rio |
| Menu | õ | Configurações | Configura??es |
| Nav | õ | Transações | Transa??es |
| Nav | ç | Orçamentos | Or?amentos |
| Nav | ó | Relatórios | Relat?rios |
| Dashboard | Í | LÍQUIDO | L?QUIDO |
| Dashboard | Ô | PATRIMÔNIO | PATRIM?NIO |

---

## ? Critérios de Sucesso

O teste é **BEM-SUCEDIDO** se:

1. ? Tela de loading mostra "Carregando aplicação..."
2. ? Menu de usuário mostra "Usuário" (não "Usu?rio")
3. ? Menu de navegação mostra "Configurações" (não "Configura??es")
4. ? Todas as páginas mostram acentos corretamente
5. ? Console não mostra erros de localização
6. ? Trocar idioma funciona sem problemas

---

## ?? Reportar Problema

Se após seguir todos os passos o problema persistir:

1. **Capturar screenshot** da área com problema
2. **Abrir Console** (F12) e copiar qualquer erro
3. **Verificar encoding** do arquivo:
   ```powershell
   Get-Content src/MoneyManager.Web/wwwroot/i18n/pt-BR.json | Select-Object -First 5
   ```
4. **Informar**:
   - Sistema operacional
   - Navegador e versão
   - Se é desenvolvimento ou produção
   - Screenshot e logs

---

**Última atualização**: Janeiro 2025  
**Versão do documento**: 1.0
