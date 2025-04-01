FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /app
COPY . .
RUN dotnet restore ServiceWorker.csproj
RUN dotnet publish ServiceWorker.csproj -c Release -o /app/published-ap
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/published-app /app
ENTRYPOINT ["dotnet", "ServiceWorker.dll"]
