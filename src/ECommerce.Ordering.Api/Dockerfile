# syntax=docker/dockerfile:1
# Build depuis la racine du dépôt :
#   docker build -t ecommerce-ordering:dev -f src/ECommerce.Ordering.Api/Dockerfile .
# Base en mémoire : aucune chaîne de connexion. L'adresse de Catalog est injectée au démarrage
#   via services__catalog__http__0 (découverte de services Aspire).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. Projets seuls d'abord : la couche restore reste en cache tant qu'ils ne changent pas.
COPY src/ECommerce.ServiceDefaults/ECommerce.ServiceDefaults.csproj src/ECommerce.ServiceDefaults/
COPY src/ECommerce.Ordering.Api/ECommerce.Ordering.Api.csproj src/ECommerce.Ordering.Api/
RUN dotnet restore src/ECommerce.Ordering.Api/ECommerce.Ordering.Api.csproj

# 2. Code source, puis publication.
COPY src/ECommerce.ServiceDefaults/ src/ECommerce.ServiceDefaults/
COPY src/ECommerce.Ordering.Api/ src/ECommerce.Ordering.Api/
RUN dotnet publish src/ECommerce.Ordering.Api/ECommerce.Ordering.Api.csproj \
      -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime chiselé : ni shell, ni gestionnaire de paquets, runtime ASP.NET uniquement.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "ECommerce.Ordering.Api.dll"]
