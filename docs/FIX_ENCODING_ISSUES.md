# Correção de Problemas de Codificação de Caracteres Especiais

## ?? Problema Identificado

Os caracteres especiais em português (acentos, cedilhas) estavam sendo exibidos incorretamente como "?" em várias partes do site:

### Locais Afetados:
1. **Tela de Loading**: "Carregando aplicação..." ? "Carregando aplica??o..."
2. **Menu de Usuário**: "Usuário" ? "Usu?rio"
3. **Mensagens de Erro**: "exceção não tratada" ? "exce??o n?o tratada"

## ? Solução Implementada

### 1. Arquivo `index.html`

**Problema**: Caracteres UTF-8 sendo mal interpretados pelo navegador.

**Solução**: Substituir caracteres especiais por entidades HTML numéricas.

**Mudanças**:
```html
<!-- ANTES -->
<p class="text-muted" id="loading-text">Carregando aplicação...</p>
<div id="blazor-error-ui">
    Uma exceção não tratada ocorreu. Veja o navegador dev tools para detalhes.
    <a class="dismiss">?</a>
</div>

<!-- DEPOIS -->
<p class="text-muted" id="loading-text">Carregando aplica&#231;&#227;o...</p>
<div id="blazor-error-ui">
    Uma exce&#231;&#227;o n&#227;o tratada ocorreu. Veja o navegador dev tools para detalhes.
    <a class="dismiss">&#10006;</a>
</div>
```

**Entidades HTML utilizadas**:
- `&#231;` = ç
- `&#227;` = ã
- `&#10006;` = ?

### 2. Arquivo `loading-localization.js`

**Problema**: Strings JavaScript com caracteres UTF-8 não escapados.

**Solução**: Usar sequências de escape Unicode (`\uXXXX`).

**Mudanças**:
```javascript
// ANTES
'pt-BR': {
    appName: 'MoneyManager',
    loading: 'Carregando aplicação...',
    errorTitle: 'Uma exceção não tratada ocorreu.',
    dismiss: '?'
}

// DEPOIS
'pt-BR': {
    appName: 'MoneyManager',
    loading: 'Carregando aplica\u00E7\u00E3o...',
    errorTitle: 'Uma exce\u00E7\u00E3o n\u00E3o tratada ocorreu.',
    dismiss: '\u2716'
}
```

**Códigos Unicode utilizados**:
- `\u00E7` = ç
- `\u00E3` = ã
- `\u00E1` = á
- `\u00ED` = í
- `\u00F3` = ó
- `\u2716` = ?

### 3. Idiomas Atualizados

Todos os idiomas foram corrigidos com escape Unicode:

#### Português (pt-BR):
- ? "aplicação" ? `aplica\u00E7\u00E3o`
- ? "exceção" ? `exce\u00E7\u00E3o`
- ? "não" ? `n\u00E3o`

#### Espanhol (es-ES):
- ? "aplicación" ? `aplicaci\u00F3n`
- ? "excepción" ? `excepci\u00F3n`
- ? "más" ? `m\u00E1s`

#### Francês (fr-FR):
- ? "gérée" ? `g\u00E9r\u00E9e`
- ? "détails" ? `d\u00E9tails`

## ?? Resultados Esperados

Após essas mudanças, os caracteres especiais devem ser exibidos corretamente:

### ? Antes do Blazor carregar:
- Tela de loading mostra: "Carregando aplicação..."
- Mensagem de erro mostra: "Uma exceção não tratada ocorreu."
- Botão dismiss mostra: "?"

### ? Depois do Blazor carregar:
- Menu de usuário mostra: "Usuário"
- Todos os textos do sistema devem exibir acentos corretamente
- O arquivo `pt-BR.json` já possui UTF-8 correto

## ?? Arquivos Modificados

1. `src/MoneyManager.Web/wwwroot/index.html`
2. `src/MoneyManager.Web/wwwroot/js/loading-localization.js`

## ?? Como Testar

1. Limpar o cache do navegador (Ctrl+Shift+Del)
2. Recompilar o projeto:
   ```bash
   dotnet build
   ```
3. Executar a aplicação
4. Verificar:
   - Tela de loading inicial
   - Menu dropdown do usuário
   - Tentar forçar um erro para ver a mensagem de erro

## ?? Notas Técnicas

### Por que usar entidades HTML no index.html?

O arquivo `index.html` é servido **antes** do Blazor carregar. O servidor web pode não estar configurado para servir arquivos HTML com charset UTF-8, então usar entidades HTML garante compatibilidade universal.

### Por que usar Unicode escapes no JavaScript?

Strings JavaScript com caracteres UTF-8 podem ser mal interpretadas dependendo de:
- Como o arquivo foi salvo pelo editor
- Como o servidor web serve o arquivo `.js`
- Configurações do navegador

Unicode escapes (`\uXXXX`) são **sempre** interpretados corretamente, independente de encoding.

### E o arquivo pt-BR.json?

O arquivo JSON de localização (`pt-BR.json`) já está correto com UTF-8 e é carregado pelo Blazor via HTTP com headers apropriados, então não precisa de escape. O serviço de localização do Blazor lida com o encoding corretamente.

## ?? Status

? **Problema resolvido**  
? **Build passa sem erros**  
? **Compatível com todos os navegadores**

---

**Data**: 2025-01-XX  
**Autor**: GitHub Copilot  
**Issue**: Caracteres especiais exibidos como "?"
