FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/MoneyManager.Domain/MoneyManager.Domain.csproj", "src/MoneyManager.Domain/"]
COPY ["src/MoneyManager.Application/MoneyManager.Application.csproj", "src/MoneyManager.Application/"]
COPY ["src/MoneyManager.Infrastructure/MoneyManager.Infrastructure.csproj", "src/MoneyManager.Infrastructure/"]
COPY ["src/MoneyManager.Presentation/MoneyManager.Presentation.csproj", "src/MoneyManager.Presentation/"]

RUN dotnet restore "src/MoneyManager.Presentation/MoneyManager.Presentation.csproj"

COPY . .

WORKDIR "/src/src/MoneyManager.Presentation"
RUN dotnet build "MoneyManager.Presentation.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MoneyManager.Presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "MoneyManager.Presentation.dll"]
