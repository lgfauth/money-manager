# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Supports/MoneyManager.Domain/MoneyManager.Domain.csproj", "src/Supports/MoneyManager.Domain/"]
COPY ["src/Supports/MoneyManager.Application/MoneyManager.Application.csproj", "src/Supports/MoneyManager.Application/"]
COPY ["src/Supports/MoneyManager.Infrastructure/MoneyManager.Infrastructure.csproj", "src/Supports/MoneyManager.Infrastructure/"]
COPY ["src/Workers/MoneyManager.Worker.Operational/MoneyManager.Worker.Operational.csproj", "src/Workers/MoneyManager.Worker.Operational/"]

RUN dotnet restore "src/Workers/MoneyManager.Worker.Operational/MoneyManager.Worker.Operational.csproj"

COPY . .
WORKDIR "/src/src/Workers/MoneyManager.Worker.Operational"
RUN dotnet build "MoneyManager.Worker.Operational.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MoneyManager.Worker.Operational.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Railway injects PORT for HTTP services; this worker does not expose HTTP.
# Keep PORT unused; the process just needs to keep running.
ENTRYPOINT ["dotnet", "MoneyManager.Worker.Operational.dll"]
