#!/usr/bin/env bash
# Sobe os ambientes de desenvolvimento do MoneyManager com um único comando.
#
# Uso:
#   ./dev.sh admin         -> API Backoffice + Frontend Backoffice
#   ./dev.sh operational   -> API Operacional + Frontend Web
#   ./dev.sh all           -> as 4 aplicações acima
#
# Pré-requisitos:
#   - dotnet SDK e node/npm instalados
#   - MongoDB acessível em mongodb://localhost:27017 (ex.: `docker compose up -d mongodb`)
#   - `npm ci` já executado em src/Frontends/MoneyManager.Web e MoneyManager.Backoffice

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

API_OPERATIONAL_PROJ="src/APIs/MoneyManager.Api.Operational/MoneyManager.Api.Operational.csproj"
API_BACKOFFICE_PROJ="src/APIs/MoneyManager.Api.Backoffice/MoneyManager.Api.Backoffice.csproj"
WEB_OPERATIONAL_DIR="src/Frontends/MoneyManager.Web"
WEB_BACKOFFICE_DIR="src/Frontends/MoneyManager.Backoffice"

PIDS=()

cleanup() {
  echo ""
  echo "Encerrando processos..."
  for pid in "${PIDS[@]:-}"; do
    kill "$pid" >/dev/null 2>&1 || true
  done
}
trap cleanup EXIT INT TERM

check_prereqs() {
  command -v dotnet >/dev/null 2>&1 || { echo "Erro: dotnet SDK não encontrado no PATH."; exit 1; }
  command -v npm >/dev/null 2>&1 || { echo "Erro: npm não encontrado no PATH."; exit 1; }
}

check_frontend_deps() {
  local dir="$1"
  if [ ! -d "$dir/node_modules" ]; then
    echo "Aviso: $dir/node_modules não existe. Rodando 'npm ci'..."
    npm ci --prefix "$dir"
  fi
}

start_api_backoffice() {
  echo "-> Iniciando API Backoffice (http://localhost:5243)"
  dotnet run --project "$API_BACKOFFICE_PROJ" --launch-profile http &
  PIDS+=($!)
}

start_api_operational() {
  echo "-> Iniciando API Operacional (http://localhost:5000)"
  dotnet run --project "$API_OPERATIONAL_PROJ" --launch-profile http &
  PIDS+=($!)
}

start_web_backoffice() {
  check_frontend_deps "$WEB_BACKOFFICE_DIR"
  echo "-> Iniciando Frontend Backoffice (http://localhost:3010)"
  npm run dev --prefix "$WEB_BACKOFFICE_DIR" &
  PIDS+=($!)
}

start_web_operational() {
  check_frontend_deps "$WEB_OPERATIONAL_DIR"
  echo "-> Iniciando Frontend Web (http://localhost:3000)"
  npm run dev --prefix "$WEB_OPERATIONAL_DIR" &
  PIDS+=($!)
}

usage() {
  cat <<EOF
Uso: ./dev.sh <cenario>

Cenários disponíveis:
  admin         Ambiente administrativo: API Backoffice + Frontend Backoffice
  operational   Ambiente operativo: API Operacional + Frontend Web
  all           Sobe as 4 aplicações (administrativo + operativo)

Exemplos:
  ./dev.sh admin
  ./dev.sh operational
  ./dev.sh all
EOF
}

check_prereqs

case "${1:-}" in
  admin)
    start_api_backoffice
    start_web_backoffice
    ;;
  operational)
    start_api_operational
    start_web_operational
    ;;
  all)
    start_api_backoffice
    start_api_operational
    start_web_backoffice
    start_web_operational
    ;;
  -h|--help|"")
    usage
    exit 0
    ;;
  *)
    echo "Cenário inválido: $1"
    echo ""
    usage
    exit 1
    ;;
esac

echo ""
echo "Todas as aplicações do cenário '$1' foram iniciadas. Pressione Ctrl+C para encerrar."
wait
