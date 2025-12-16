#!/bin/bash

# ?? Script de Deploy Railway - MoneyManager
# Este script automatiza o deploy das duas aplicações no Railway

set -e

echo "?? MoneyManager - Railway Deploy Script"
echo "========================================"
echo ""

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Função para printar com cor
print_success() {
    echo -e "${GREEN}? $1${NC}"
}

print_error() {
    echo -e "${RED}? $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}? $1${NC}"
}

print_info() {
    echo -e "${YELLOW}? $1${NC}"
}

# Verificar se Railway CLI está instalado
echo "Verificando Railway CLI..."
if ! command -v railway &> /dev/null; then
    print_error "Railway CLI não encontrado!"
    echo ""
    echo "Instale com: npm i -g @railway/cli"
    echo "Ou visite: https://docs.railway.app/develop/cli"
    exit 1
fi
print_success "Railway CLI instalado"

# Verificar se está logado
echo "Verificando autenticação..."
if ! railway whoami &> /dev/null; then
    print_warning "Não está logado no Railway"
    echo "Fazendo login..."
    railway login
fi
print_success "Autenticado no Railway"

echo ""
echo "========================================"
echo "?? Configuração"
echo "========================================"
echo ""

# Solicitar informações
read -p "MongoDB Connection String: " MONGODB_CONNECTION
read -p "JWT Secret Key (min 32 chars): " JWT_SECRET
read -p "Nome do projeto Railway (ex: moneymanager): " PROJECT_NAME

echo ""
echo "========================================"
echo "?? Iniciando Deploy"
echo "========================================"
echo ""

# Criar projeto (se não existir)
echo "1??  Criando/Selecionando projeto..."
railway link || railway init

# Deploy API
echo ""
echo "2??  Deploying API..."
echo "-----------------------------------"

# Definir variáveis de ambiente para API
railway variables set MONGODB__CONNECTIONSTRING="$MONGODB_CONNECTION"
railway variables set MONGODB__DATABASENAME="MoneyAgent"
railway variables set JWT__SECRETKEY="$JWT_SECRET"
railway variables set JWT__ISSUER="MoneyManager"
railway variables set JWT__AUDIENCE="MoneyManagerUsers"
railway variables set JWT__EXPIRATIONHOURS="24"
railway variables set ASPNETCORE_ENVIRONMENT="Production"
railway variables set ASPNETCORE_URLS="http://0.0.0.0:8080"

print_success "Variáveis de ambiente configuradas"

# Fazer deploy
echo "Fazendo deploy da API..."
railway up -d Dockerfile.api

print_success "API deployada!"

# Obter URL da API
API_URL=$(railway domain)
print_success "API URL: $API_URL"

# Deploy Frontend
echo ""
echo "3??  Deploying Frontend..."
echo "-----------------------------------"

# Definir variáveis de ambiente para Frontend
railway variables set API_URL="https://$API_URL"
railway variables set ASPNETCORE_ENVIRONMENT="Production"
railway variables set ASPNETCORE_URLS="http://0.0.0.0:8080"

print_success "Variáveis de ambiente configuradas"

# Fazer deploy
echo "Fazendo deploy do Frontend..."
railway up -d Dockerfile.web

print_success "Frontend deployado!"

# Obter URL do Frontend
WEB_URL=$(railway domain)
print_success "Frontend URL: $WEB_URL"

echo ""
echo "========================================"
echo "? Deploy Concluído!"
echo "========================================"
echo ""
echo "?? URLs da Aplicação:"
echo "   API:      https://$API_URL"
echo "   Frontend: https://$WEB_URL"
echo "   Swagger:  https://$API_URL/swagger"
echo "   Health:   https://$API_URL/health"
echo ""
echo "?? Próximos Passos:"
echo "   1. Acesse https://$WEB_URL para testar"
echo "   2. Verifique os logs: railway logs"
echo "   3. Monitore no dashboard: https://railway.app"
echo ""
print_success "Tudo pronto! ??"
