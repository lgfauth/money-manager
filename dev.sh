#!/usr/bin/env bash
# Sobe os ambientes de desenvolvimento do MoneyManager com um único comando.
#
# Uso:
#   ./dev.sh admin         -> API Backoffice + Frontend Backoffice
#   ./dev.sh operational   -> API Operacional + Frontend Web
#   ./dev.sh full          -> API Operacional + Frontend Web + Worker
#   ./dev.sh worker        -> só o Worker
#   ./dev.sh all           -> todas as aplicações (administrativo + operativo + Worker)
#   ./dev.sh setup         -> só prepara MongoDB e configuração local, sem iniciar nada
#
# Pré-requisitos:
#   - dotnet SDK e node/npm instalados
#   - MongoDB em mongodb://localhost:27017 — os cenários operativos sobem o contêiner
#     Docker `mongodb` (mongo:7) automaticamente quando a porta está livre
#   - `npm ci` em src/Frontends/MoneyManager.Web e MoneyManager.Backoffice (feito
#     automaticamente se `node_modules` não existir)
#
# Os cenários operativos (operational, full, worker, all) rodam antes scripts/dev-setup.mjs,
# que completa appsettings.Development.json (API e Worker) e o .env.local do Web sem
# sobrescrever valores já configurados. Esses arquivos são ignorados pelo git.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

API_OPERATIONAL_PROJ="src/APIs/MoneyManager.Api.Operational/MoneyManager.Api.Operational.csproj"
WORKER_PROJ="src/Workers/MoneyManager.Worker.Operational/MoneyManager.Worker.Operational.csproj"
# A API Operacional escuta em $PORT (padrão 8080 no Railway); o Web local espera a 5000
API_OPERATIONAL_PORT="${API_OPERATIONAL_PORT:-5000}"
MONGO_CONTAINER="mongodb"
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

port_in_use() {
  (exec 3<>"/dev/tcp/127.0.0.1/$1") 2>/dev/null
}

ensure_mongo() {
  if port_in_use 27017; then
    echo "-> MongoDB já disponível em localhost:27017"
    return
  fi
  if ! command -v docker >/dev/null 2>&1; then
    echo "Erro: MongoDB não está em localhost:27017 e o Docker não foi encontrado para subi-lo."
    exit 1
  fi
  if docker container inspect "$MONGO_CONTAINER" >/dev/null 2>&1; then
    echo "-> Iniciando contêiner Docker existente '$MONGO_CONTAINER'"
    docker start "$MONGO_CONTAINER" >/dev/null
  else
    echo "-> Criando contêiner Docker '$MONGO_CONTAINER' (mongo:7) na porta 27017"
    docker run -d --name "$MONGO_CONTAINER" -p 27017:27017 mongo:7 >/dev/null
  fi
  for _ in $(seq 1 30); do
    port_in_use 27017 && return
    sleep 1
  done
  echo "Erro: MongoDB não respondeu em localhost:27017 após 30 s."
  exit 1
}

prepare_operational() {
  ensure_mongo
  node scripts/dev-setup.mjs
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
  echo "-> Iniciando API Operacional (http://localhost:$API_OPERATIONAL_PORT)"
  PORT="$API_OPERATIONAL_PORT" dotnet run --project "$API_OPERATIONAL_PROJ" --launch-profile http &
  PIDS+=($!)
}

start_worker() {
  echo "-> Iniciando Worker Operacional (jobs agendados e importação de fatura)"
  dotnet run --project "$WORKER_PROJ" &
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
  full          Ambiente operativo completo: API Operacional + Frontend Web + Worker
  worker        Só o Worker Operacional
  all           Todas as aplicações (administrativo + operativo + Worker)
  setup         Só prepara MongoDB e a configuração local de dev, sem iniciar nada

Exemplos:
  ./dev.sh full
  ./dev.sh operational
  ./dev.sh setup
EOF
}

check_prereqs

case "${1:-}" in
  admin)
    start_api_backoffice
    start_web_backoffice
    ;;
  operational)
    prepare_operational
    start_api_operational
    start_web_operational
    ;;
  full)
    prepare_operational
    start_api_operational
    start_web_operational
    start_worker
    ;;
  worker)
    prepare_operational
    start_worker
    ;;
  all)
    prepare_operational
    start_api_backoffice
    start_api_operational
    start_web_backoffice
    start_web_operational
    start_worker
    ;;
  setup)
    prepare_operational
    exit 0
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
