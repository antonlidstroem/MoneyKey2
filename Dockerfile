# STAGE 1: THE BUILDER
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# 1. Kopiera projektfilen
COPY ["MoneyKey.API/MoneyKey.API.csproj", "MoneyKey.API/"]
RUN dotnet restore "MoneyKey.API/MoneyKey.API.csproj"

# 2. Kopiera resten
COPY . .

# 3. Bygg
WORKDIR "/app/MoneyKey.API"
RUN dotnet publish -c Release -o /app/out

# STAGE 2: THE RUNNER
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# --- HÄR ÄR ÄNDRINGEN ---
# Vi sätter port-inställningarna INNAN vi startar appen
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Starta API:et (Sista raden!)
ENTRYPOINT ["dotnet", "MoneyKey.API.dll"]