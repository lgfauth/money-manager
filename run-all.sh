#!/bin/bash
echo "============================================"
echo "MoneyManager - Full Stack Application"
echo "============================================"
echo ""
echo "Starting API and Web servers..."
echo ""
echo "API will run on: https://localhost:5001"
echo "Web will run on: https://localhost:7001"
echo ""

# Start API in background
dotnet run --project src/MoneyManager.Presentation/MoneyManager.Presentation.csproj --launch-profile https &
API_PID=$!

# Wait a bit for API to start
sleep 3

# Start Web
dotnet run --project src/MoneyManager.Web/MoneyManager.Web.csproj --launch-profile https &
WEB_PID=$!

echo ""
echo "API PID: $API_PID"
echo "Web PID: $WEB_PID"
echo ""
echo "Press Ctrl+C to stop both applications"
echo ""

# Wait for both to finish
wait $API_PID $WEB_PID
