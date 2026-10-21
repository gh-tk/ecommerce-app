# syntax=docker/dockerfile:1
# Build depuis la racine du dépôt :
#   docker build -t ecommerce-catalog:dev -f src/ECommerce.Catalog.Api/Dockerfile .
# La chaîne de connexion n'est PAS dans l'image : injectée au démarrage via
#   ConnectionStrings__catalogdb (variable d'environnement / Secret Kubernetes).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. Projets seuls d'abord : la couche restore reste en cache tant qu'ils ne changent pas.
COPY src/ECommerce.ServiceDefaults/ECommerce.ServiceDefaults.csproj src/ECommerce.ServiceDefaults/
COPY src/ECommerce.Catalog.Api/ECommerce.Catalog.Api.csproj src/ECommerce.Catalog.Api/
RUN dotnet restore src/ECommerce.Catalog.Api/ECommerce.Catalog.Api.csproj

# 2. Code source, puis publication.
COPY src/ECommerce.ServiceDefaults/ src/ECommerce.ServiceDefaults/
COPY src/ECommerce.Catalog.Api/ src/ECommerce.Catalog.Api/
RUN dotnet publish src/ECommerce.Catalog.Api/ECommerce.Catalog.Api.csproj \
      -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime chiselé : ni shell, ni gestionnaire de paquets, runtime ASP.NET uniquement.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "ECommerce.Catalog.Api.dll"]
