---
name: containerize
description: Écrit ou optimise les Dockerfiles de chaque microservice de l'ecommerce-app (Catalog, Ordering, Gateway, Web — build multi-étapes, runtime chiselé non-root), génère le docker-compose de développement avec PostgreSQL, lance la pile avec docker compose up et scanne chaque image avec Trivy (HIGH, CRITICAL). À utiliser pour conteneuriser un service ou toute l'application, réduire une image, corriger un build Docker ou lancer la pile en conteneurs. Ne met aucun secret dans une couche et demande confirmation avant toute suppression.
---

# Skill : containerize

Objectif : des images **petites, non-root, sans secret et scannées**, et une pile qui démarre
avec une seule commande.

## Microservices concernés

| Projet                     | Service compose | Image                   |
|----------------------------|-----------------|-------------------------|
| `ECommerce.Catalog.Api`    | `catalog`       | `ecommerce-catalog:dev` |
| `ECommerce.Ordering.Api`   | `ordering`      | `ecommerce-ordering:dev`|
| `ECommerce.Gateway`        | `gateway`       | `ecommerce-gateway:dev` |
| `ECommerce.Web`            | `web`           | `ecommerce-web:dev`     |

`ECommerce.AppHost` (orchestrateur Aspire) et `ECommerce.ServiceDefaults` (bibliothèque) ne
sont **pas** conteneurisés.

## Procédure

1. **Identifier** les dépendances de projet de chaque service (toujours `ECommerce.ServiceDefaults`).
2. **Écrire un Dockerfile par microservice** — `src/<Projet>/Dockerfile` :
   - étape `build` sur `mcr.microsoft.com/dotnet/sdk:10.0` ; les `.csproj` copiés **avant**
     le code (cache du `restore`), puis `dotnet publish -c Release -o /app/publish` ;
   - étape finale sur `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` : ni shell, ni
     gestionnaire de paquets ;
   - `USER $APP_UID`, `EXPOSE 8080`.
3. **Vérifier `.dockerignore`** à la racine : il exclut `bin/` et `obj/`, sinon le `restore`
   du conteneur casse.
4. **Générer `docker-compose.yml`** à la racine :
   - `postgres:17-alpine` avec `healthcheck` `pg_isready` et un volume nommé ; aucun port publié ;
   - le mot de passe en `${POSTGRES_PASSWORD:?message}` lu dans `.env` (non versionné) : compose
     **refuse de démarrer** s'il est absent ou vide. `.env.example` (valeur vide) est versionné ;
   - `catalog` avec `depends_on: postgres: condition: service_healthy` et
     `ConnectionStrings__catalogdb` construite avec ce même `${POSTGRES_PASSWORD:?…}` ;
   - un service par ligne du tableau, `build.context: .` et `build.dockerfile: src/<Projet>/Dockerfile` ;
   - la découverte de services Aspire (`https+http://catalog`…) est résolue par variables
     d'environnement, par exemple `services__catalog__http__0: http://catalog:8080` pour
     `ordering` et `gateway`, `services__ordering__http__0` pour `gateway`,
     `services__gateway__http__0` pour `web` ;
   - `depends_on` : `ordering` → `catalog`, `gateway` → `catalog` + `ordering`, `web` → `gateway` ;
   - **seul `web` publie un port** (`8080:8080`) ; les autres restent sur le réseau interne.
5. **Construire et lancer** depuis la racine du dépôt :
   `docker compose up -d --build`, puis `docker compose ps` : tous les services `running`.
   Lire `docker compose logs <service>` pour tout service qui redémarre.
6. **Vérifier non-root** — pour chaque image, `docker inspect --format '{{.Config.User}}' <image>` :
   un UID numérique, jamais vide ni `0`. (L'image chiselée n'a pas de commande `id`.)
7. **Scanner chaque image** — `trivy image --severity HIGH,CRITICAL <image>` sur les quatre
   images, puis qualifier chaque résultat : exploitable **ici** ou théorique ? Présenter un
   tableau image / CVE / sévérité / verdict.

## Base de données

Catalog n'applique ses migrations EF (schéma + seed) qu'en `Development` : dans compose, il
tourne avec `ASPNETCORE_ENVIRONMENT=Development`. `web` reste en `Production` — en
`Development`, ses assets statiques seraient cherchés sur l'hôte et répondraient 404.

## Garde-fous

- **Aucun secret** dans un `Dockerfile` ni dans `docker-compose.yml` : ni `ENV`, ni `ARG`, ni
  fichier copié. Une valeur sensible est lue dans `.env` (non versionné).
- Image finale **runtime uniquement** (jamais le SDK) et **non-root**.
- `docker system prune`, `docker volume rm`, `docker compose down -v` (supprime le volume
  PostgreSQL) : bloqués par le hook
  `guard-destructive` — les proposer, ne pas les lancer.
- Images de base **épinglées** (tag explicite, idéalement digest en environnement bancaire).
