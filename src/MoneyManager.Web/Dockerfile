# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/MoneyManager.Domain/MoneyManager.Domain.csproj", "src/MoneyManager.Domain/"]
COPY ["src/MoneyManager.Application/MoneyManager.Application.csproj", "src/MoneyManager.Application/"]
COPY ["src/MoneyManager.Web/MoneyManager.Web.csproj", "src/MoneyManager.Web/"]

RUN dotnet restore "src/MoneyManager.Web/MoneyManager.Web.csproj"

COPY . .
WORKDIR "/src/src/MoneyManager.Web"
RUN dotnet build "MoneyManager.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MoneyManager.Web.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM base AS final
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MoneyManager.Web.dll"]
