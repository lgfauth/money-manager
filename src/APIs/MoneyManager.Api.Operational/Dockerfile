FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Supports/MoneyManager.Domain/MoneyManager.Domain.csproj", "src/Supports/MoneyManager.Domain/"]
COPY ["src/Supports/MoneyManager.Application/MoneyManager.Application.csproj", "src/Supports/MoneyManager.Application/"]
COPY ["src/Supports/MoneyManager.Infrastructure/MoneyManager.Infrastructure.csproj", "src/Supports/MoneyManager.Infrastructure/"]
COPY ["src/APIs/MoneyManager.Api.Operational/MoneyManager.Api.Operational.csproj", "src/APIs/MoneyManager.Api.Operational/"]

RUN dotnet restore "src/APIs/MoneyManager.Api.Operational/MoneyManager.Api.Operational.csproj"

COPY . .

WORKDIR "/src/src/APIs/MoneyManager.Api.Operational"
RUN dotnet build "MoneyManager.Api.Operational.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MoneyManager.Api.Operational.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "MoneyManager.Api.Operational.dll"]
