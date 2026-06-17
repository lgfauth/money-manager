# MoneyManager - Comandos Git

# Aviso importante
Jamais realize um commit, fetch, push ou pull ou execute qualquer outro comando git a não ser que seja solicitado por um humano.

# Inicializar repositório (se ainda não fez)
git init

# Adicionar todos os arquivos
git add .

# Commit inicial
git commit -m "feat: Preparado para deploy no Railway com Docker"

# Adicionar remote (substitua com seu repositório)
# git remote add origin https://github.com/lgfauth/money-manager.git

# Push para GitHub
# git push -u origin main

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
# Estrutura de Commits (Conventional Commits)
# ============================================

# Features:
# git commit -m "feat: adiciona nova funcionalidade X"

# Fixes:
# git commit -m "fix: corrige bug na página Y"

# Docs:
# git commit -m "docs: atualiza README"

# Style:
# git commit -m "style: ajusta formatação do código"

# Refactor:
# git commit -m "refactor: melhora estrutura do serviço Z"

# ============================================
# Ignorados pelo .gitignore
# ============================================
# - bin/
# - obj/
# - .vs/
# - appsettings.Development.json
# - node_modules/
# - .env
