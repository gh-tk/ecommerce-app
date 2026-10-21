# syntax=docker/dockerfile:1
# Build depuis la racine du dépôt :
#   docker build -t ecommerce-web:dev -f src/ECommerce.Web/Dockerfile .
# L'adresse du Gateway est injectée au démarrage via services__gateway__http__0.
#
# Assets statiques : wwwroot/ (Bootstrap, app.css, favicon) est copié avec le code source.
# C'est dotnet publish qui produit ce que MapStaticAssets() sert : wwwroot avec empreintes
# et variantes .gz/.br, le CSS isolé ECommerce.Web.styles.css, _framework/blazor.web.js et
# le manifeste ECommerce.Web.staticwebassets.endpoints.json. Ne pas lancer le conteneur en
# Development : ce mode lit les assets depuis les chemins sources de l'hôte, absents ici.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. Projets seuls d'abord : la couche restore reste en cache tant qu'ils ne changent pas.
COPY src/ECommerce.ServiceDefaults/ECommerce.ServiceDefaults.csproj src/ECommerce.ServiceDefaults/
COPY src/ECommerce.Web/ECommerce.Web.csproj src/ECommerce.Web/
# Sans fichier .razor visible, le SDK ne restaure pas Microsoft.AspNetCore.App.Internal.Assets,
# qui fournit _framework/blazor.web.js : on le force, sinon ce script répond 404 au runtime.
RUN dotnet restore src/ECommerce.Web/ECommerce.Web.csproj -p:RequiresAspNetWebAssets=true

# 2. Code source (wwwroot compris), puis publication.
COPY src/ECommerce.ServiceDefaults/ src/ECommerce.ServiceDefaults/
COPY src/ECommerce.Web/ src/ECommerce.Web/
RUN dotnet publish src/ECommerce.Web/ECommerce.Web.csproj \
      -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime chiselé : ni shell, ni gestionnaire de paquets, runtime ASP.NET uniquement.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "ECommerce.Web.dll"]
