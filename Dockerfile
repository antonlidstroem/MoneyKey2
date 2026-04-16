# STAGE 1: THE BUILDER
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# 1. Kopiera projektfilen från undermappen till containern
# Vi anger [källmapp/fil] [målmapp/]
COPY ["MoneyKey.API/MoneyKey.API.csproj", "MoneyKey.API/"]
RUN dotnet restore "MoneyKey.API/MoneyKey.API.csproj"

# 2. Kopiera ALLT (alla mappar och filer i MoneyKey2)
COPY . .

# 3. Gå in i mappen där API-koden finns och bygg
WORKDIR "/app/MoneyKey.API"
RUN dotnet publish -c Release -o /app/out

# STAGE 2: THE RUNNER
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Hämta den färdiga koden från 'out'-mappen
COPY --from=build-env /app/out .

EXPOSE 8080

# Starta API:et
ENTRYPOINT ["dotnet", "MoneyKey.API.dll"]