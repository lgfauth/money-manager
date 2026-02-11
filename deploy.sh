#!/bin/bash

# Script de Deploy para Railway
# Money Manager - Blazor WebAssembly

echo "?? Iniciando processo de deploy..."
echo ""

# 1. Limpar builds anteriores
echo "?? Limpando builds anteriores..."
dotnet clean
if [ $? -ne 0 ]; then
    echo "? Erro ao limpar projeto"
    exit 1
fi
echo "? Limpeza concluída"
echo ""

# 2. Build do projeto Blazor primeiro
echo "?? Buildando MoneyManager.Web (Blazor)..."
cd src/MoneyManager.Web
dotnet build -c Release
if [ $? -ne 0 ]; then
    echo "? Erro ao buildar Blazor"
    exit 1
fi
cd ../..
echo "? Blazor buildado com sucesso"
echo ""

# 3. Publish do Web.Host
echo "?? Publicando MoneyManager.Web.Host..."
cd src/MoneyManager.Web.Host
dotnet publish -c Release -o ./publish
if [ $? -ne 0 ]; then
    echo "? Erro ao publicar Web.Host"
    exit 1
fi
echo "? Publish concluído"
echo ""

# 4. Verificar se wwwroot foi copiado
echo "?? Verificando se wwwroot foi copiado..."
if [ ! -d "./publish/wwwroot" ]; then
    echo "? Pasta wwwroot não encontrada no publish!"
    echo "   Verifique se os targets MSBuild estão corretos"
    exit 1
fi

if [ ! -f "./publish/wwwroot/index.html" ]; then
    echo "? index.html não encontrado no publish/wwwroot!"
    exit 1
fi

if [ ! -d "./publish/wwwroot/_framework" ]; then
    echo "? Pasta _framework não encontrada no publish/wwwroot!"
    exit 1
fi

if [ ! -d "./publish/wwwroot/i18n" ]; then
    echo "? Pasta i18n não encontrada no publish/wwwroot!"
    exit 1
fi

echo "? Todos os arquivos estáticos presentes:"
echo "   - index.html: ?"
echo "   - _framework/: ?"
echo "   - i18n/: ?"
echo ""

# 5. Listar tamanho do publish
echo "?? Estatísticas do publish:"
du -sh ./publish
echo ""

# 6. Commit e push (se solicitado)
read -p "?? Deseja fazer commit e push das mudanças? (s/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Ss]$ ]]; then
    cd ../..
    
    echo "?? Commit das mudanças..."
    git add .
    git commit -m "fix: copy Blazor wwwroot to Web.Host on publish

- Adicionado MSBuild targets para copiar wwwroot automaticamente
- Ajustado Program.cs para usar wwwroot local em produção
- Corrigido 404 em arquivos estáticos (i18n, _framework, etc)
- Página /accounts agora carrega corretamente"
    
    if [ $? -eq 0 ]; then
        echo "? Commit realizado"
        
        read -p "?? Push para origin/main? (s/N): " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Ss]$ ]]; then
            git push origin main
            if [ $? -eq 0 ]; then
                echo "? Push realizado com sucesso!"
                echo "   Railway deve iniciar deploy automaticamente"
            else
                echo "? Erro ao fazer push"
                exit 1
            fi
        fi
    else
        echo "??  Nada para commitar ou erro no commit"
    fi
fi

echo ""
echo "?? Processo finalizado!"
echo ""
echo "?? Próximos passos:"
echo "   1. Aguardar deploy no Railway"
echo "   2. Verificar logs para confirmar: '[PROD] Usando wwwroot local'"
echo "   3. Testar página /accounts em produção"
echo "   4. Verificar console do navegador (F12) - não deve ter 404"
echo ""
