FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /app

# Kopier alt ind først, så NLog.config kommer med i build
COPY . .

RUN dotnet restore ServiceWorker.csproj
RUN dotnet publish ServiceWorker.csproj -c Release -o /app/published-app

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Kopier den færdige publishede app
COPY --from=build /app/published-app /app

ENTRYPOINT ["dotnet", "ServiceWorker.dll"]
