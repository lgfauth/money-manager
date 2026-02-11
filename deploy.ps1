# Script de Deploy para Railway
# Money Manager - Blazor WebAssembly

Write-Host "?? Iniciando processo de deploy..." -ForegroundColor Green
Write-Host ""

# 1. Limpar builds anteriores
Write-Host "?? Limpando builds anteriores..." -ForegroundColor Yellow
dotnet clean
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Erro ao limpar projeto" -ForegroundColor Red
    exit 1
}
Write-Host "? Limpeza concluída" -ForegroundColor Green
Write-Host ""

# 2. Build do projeto Blazor primeiro
Write-Host "?? Buildando MoneyManager.Web (Blazor)..." -ForegroundColor Yellow
Push-Location src/MoneyManager.Web
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Erro ao buildar Blazor" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location
Write-Host "? Blazor buildado com sucesso" -ForegroundColor Green
Write-Host ""

# 3. Publish do Web.Host
Write-Host "?? Publicando MoneyManager.Web.Host..." -ForegroundColor Yellow
Push-Location src/MoneyManager.Web.Host
dotnet publish -c Release -o ./publish
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Erro ao publicar Web.Host" -ForegroundColor Red
    Pop-Location
    exit 1
}
Write-Host "? Publish concluído" -ForegroundColor Green
Write-Host ""

# 4. Verificar se wwwroot foi copiado
Write-Host "?? Verificando se wwwroot foi copiado..." -ForegroundColor Yellow

if (-not (Test-Path "./publish/wwwroot")) {
    Write-Host "? Pasta wwwroot não encontrada no publish!" -ForegroundColor Red
    Write-Host "   Verifique se os targets MSBuild estão corretos" -ForegroundColor Yellow
    Pop-Location
    exit 1
}

if (-not (Test-Path "./publish/wwwroot/index.html")) {
    Write-Host "? index.html não encontrado no publish/wwwroot!" -ForegroundColor Red
    Pop-Location
    exit 1
}

if (-not (Test-Path "./publish/wwwroot/_framework")) {
    Write-Host "? Pasta _framework não encontrada no publish/wwwroot!" -ForegroundColor Red
    Pop-Location
    exit 1
}

if (-not (Test-Path "./publish/wwwroot/i18n")) {
    Write-Host "? Pasta i18n não encontrada no publish/wwwroot!" -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host "? Todos os arquivos estáticos presentes:" -ForegroundColor Green
Write-Host "   - index.html: ?" -ForegroundColor Green
Write-Host "   - _framework/: ?" -ForegroundColor Green
Write-Host "   - i18n/: ?" -ForegroundColor Green
Write-Host ""

# 5. Listar tamanho do publish
Write-Host "?? Estatísticas do publish:" -ForegroundColor Cyan
$size = (Get-ChildItem -Path ./publish -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "   Tamanho total: $([math]::Round($size, 2)) MB" -ForegroundColor Cyan
Write-Host ""

Pop-Location

# 6. Commit e push (se solicitado)
$commit = Read-Host "?? Deseja fazer commit e push das mudanças? (s/N)"
if ($commit -eq 's' -or $commit -eq 'S') {
    Write-Host "?? Commit das mudanças..." -ForegroundColor Yellow
    
    git add .
    git commit -m "fix: copy Blazor wwwroot to Web.Host on publish

- Adicionado MSBuild targets para copiar wwwroot automaticamente
- Ajustado Program.cs para usar wwwroot local em produção
- Corrigido 404 em arquivos estáticos (i18n, _framework, etc)
- Página /accounts agora carrega corretamente"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Commit realizado" -ForegroundColor Green
        
        $push = Read-Host "?? Push para origin/main? (s/N)"
        if ($push -eq 's' -or $push -eq 'S') {
            git push origin main
            if ($LASTEXITCODE -eq 0) {
                Write-Host "? Push realizado com sucesso!" -ForegroundColor Green
                Write-Host "   Railway deve iniciar deploy automaticamente" -ForegroundColor Cyan
            } else {
                Write-Host "? Erro ao fazer push" -ForegroundColor Red
                exit 1
            }
        }
    } else {
        Write-Host "??  Nada para commitar ou erro no commit" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "?? Processo finalizado!" -ForegroundColor Green
Write-Host ""
Write-Host "?? Próximos passos:" -ForegroundColor Cyan
Write-Host "   1. Aguardar deploy no Railway" -ForegroundColor White
Write-Host "   2. Verificar logs para confirmar: '[PROD] Usando wwwroot local'" -ForegroundColor White
Write-Host "   3. Testar página /accounts em produção" -ForegroundColor White
Write-Host "   4. Verificar console do navegador (F12) - não deve ter 404" -ForegroundColor White
Write-Host ""
