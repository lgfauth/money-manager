@echo off
echo ============================================
echo MoneyManager - Full Stack Application
echo ============================================
echo.
echo Starting API and Web servers...
echo.
echo API will run on: https://localhost:5001
echo Web will run on: https://localhost:7001
echo.
start "MoneyManager API" cmd /k "cd /d %~dp0 && dotnet run --project src/MoneyManager.Presentation/MoneyManager.Presentation.csproj --launch-profile https"
timeout /t 3
start "MoneyManager Web" cmd /k "cd /d %~dp0 && dotnet run --project src/MoneyManager.Web/MoneyManager.Web.csproj --launch-profile https"
echo.
echo Applications starting. Check the opened windows for details.
pause
