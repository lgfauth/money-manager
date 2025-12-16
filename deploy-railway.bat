@echo off
REM ?? Script de Deploy Railway - MoneyManager (Windows)
REM Este script automatiza o deploy das duas aplicações no Railway

echo.
echo ?? MoneyManager - Railway Deploy Script
echo ========================================
echo.

REM Verificar se Railway CLI está instalado
where railway >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ? Railway CLI não encontrado!
    echo.
    echo Instale com: npm i -g @railway/cli
    echo Ou visite: https://docs.railway.app/develop/cli
    exit /b 1
)
echo ? Railway CLI instalado
echo.

REM Verificar se está logado
railway whoami >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ? Não está logado no Railway
    echo Fazendo login...
    railway login
)
echo ? Autenticado no Railway
echo.

echo ========================================
echo ?? Configuração
echo ========================================
echo.

REM Solicitar informações
set /p MONGODB_CONNECTION="MongoDB Connection String: "
set /p JWT_SECRET="JWT Secret Key (min 32 chars): "
set /p PROJECT_NAME="Nome do projeto Railway: "

echo.
echo ========================================
echo ?? Iniciando Deploy
echo ========================================
echo.

REM Criar/selecionar projeto
echo 1??  Criando/Selecionando projeto...
railway link || railway init
echo.

REM Deploy API
echo 2??  Deploying API...
echo -----------------------------------

REM Configurar variáveis de ambiente para API
railway variables set MONGODB__CONNECTIONSTRING="%MONGODB_CONNECTION%"
railway variables set MONGODB__DATABASENAME="MoneyAgent"
railway variables set JWT__SECRETKEY="%JWT_SECRET%"
railway variables set JWT__ISSUER="MoneyManager"
railway variables set JWT__AUDIENCE="MoneyManagerUsers"
railway variables set JWT__EXPIRATIONHOURS="24"
railway variables set ASPNETCORE_ENVIRONMENT="Production"
railway variables set ASPNETCORE_URLS="http://0.0.0.0:8080"

echo ? Variáveis de ambiente configuradas

REM Fazer deploy
echo Fazendo deploy da API...
railway up -d Dockerfile.api

echo ? API deployada!

REM Obter URL da API
for /f "delims=" %%i in ('railway domain') do set API_URL=%%i
echo ? API URL: %API_URL%
echo.

REM Deploy Frontend
echo 3??  Deploying Frontend...
echo -----------------------------------

REM Configurar variáveis de ambiente para Frontend
railway variables set API_URL="https://%API_URL%"
railway variables set ASPNETCORE_ENVIRONMENT="Production"
railway variables set ASPNETCORE_URLS="http://0.0.0.0:8080"

echo ? Variáveis de ambiente configuradas

REM Fazer deploy
echo Fazendo deploy do Frontend...
railway up -d Dockerfile.web

echo ? Frontend deployado!

REM Obter URL do Frontend
for /f "delims=" %%i in ('railway domain') do set WEB_URL=%%i
echo ? Frontend URL: %WEB_URL%
echo.

echo ========================================
echo ? Deploy Concluído!
echo ========================================
echo.
echo ?? URLs da Aplicação:
echo    API:      https://%API_URL%
echo    Frontend: https://%WEB_URL%
echo    Swagger:  https://%API_URL%/swagger
echo    Health:   https://%API_URL%/health
echo.
echo ?? Próximos Passos:
echo    1. Acesse https://%WEB_URL% para testar
echo    2. Verifique os logs: railway logs
echo    3. Monitore no dashboard: https://railway.app
echo.
echo ? Tudo pronto! ??
echo.

pause
