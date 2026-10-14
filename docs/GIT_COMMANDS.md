# MoneyManager - Comandos Git

# Aviso importante
Jamais realize um commit, fetch, push ou pull ou execute qualquer outro comando git a não ser que seja solicitado por um humano.

# ============================================
# Repositório
# ============================================

# Remote já configurado:
# origin  https://github.com/lgfauth/money-manager.git
# Branch principal: main

# ============================================
# Comandos úteis para Desenvolvimento
# ============================================

# Status do repositório
# git status

# Ver commits
# git log --oneline

# Criar branch para features
# git checkout -b feature/nova-funcionalidade

# Voltar para main
# git checkout main

# Merge de branch
# git merge feature/nova-funcionalidade

# Push de todas as mudanças
# git push

# Pull das mudanças remotas
# git pull

# ============================================
# Mensagens de Commit
# ============================================

# Mensagens em português do Brasil, descrevendo o que foi feito/corrigido.
# Commits com alteração de código em src/ devem incluir a atualização do changelog
# (src/Frontends/MoneyManager.Web/public/changelog.md). Para mudanças apenas internas
# (refatoração, testes, CI), inclua "[skip changelog]" na mensagem.
# Exemplos do histórico do projeto:
# git commit -m "Correção de associação de transações a faturas quando vindas de open banking"
# git commit -m "Ajustes e novo botão no fluxo de saúde financeira"
# git commit -m "Remoção de arquivos desnecessários"

# ============================================
# Ignorados pelo .gitignore (principais)
# ============================================
# - bin/ e obj/
# - .vs/ e .vscode/
# - appsettings.*.json (exceto appsettings.json e appsettings.Production.json)
# - node_modules/
# - .env, .env.local e .env.*.local

# ============================================
# Hooks e verificações automáticas
# ============================================

# Ative os hooks versionados uma vez por clone:
# git config core.hooksPath .githooks
#
# .githooks/commit-msg bloqueia commits com alteração de código em src/ sem
# atualização do changelog (exceto com "[skip changelog]") e avisa quando código
# .NET muda sem alteração em tests/.
#
# No GitHub, os workflows fazem o mesmo nos pushes e PRs para main/develop:
# - .github/workflows/dotnet-tests.yml  → build + dotnet test
# - .github/workflows/change-guard.yml  → changelog obrigatório + type-check do frontend Web
