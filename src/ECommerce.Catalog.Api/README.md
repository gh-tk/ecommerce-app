# ECommerce.Catalog.Api

**Type:** ASP.NET Core minimal API
**Depends on:** ECommerce.ServiceDefaults, EF Core + PostgreSQL (Aspire `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`)

The Catalog microservice. Owns products and categories. Completely standalone — it knows
nothing about Ordering or the Gateway.

## Files

| File | Responsibility |
|---|---|
| `Models/Category.cs` | The `Category` entity: Id, Name (unique). |
| `Models/Product.cs` | The `Product` entity: Id, Sku (unique), Name, Description, Price, AvailableStock, CategoryId. |
| `Data/CatalogDbContext.cs` | EF Core context. Configures the schema (unique indexes, FK `ON DELETE RESTRICT`, `CHECK` constraints) and **seeds 3 categories and 4 demo products** (`HasData`). |
| `Data/CatalogDbContextFactory.cs` | Design-time factory used only by `dotnet ef` (no credentials). |
| `Data/Migrations/` | EF Core migrations. A committed migration is immutable — add a new one to fix it. |
| `Endpoints/CatalogEndpoints.cs` | REST routes for `/api/categories` and `/api/products` + the `CreateProductRequest` and `PagedResult<T>` DTOs. |
| `Program.cs` | Composition root: `AddServiceDefaults`, `AddNpgsqlDbContext`, OpenAPI, apply migrations in Development, map endpoints. |

## Endpoints

| Method | Route | Description |
|---|---|---|
| GET | `/api/categories` | All categories, sorted by name |
| GET | `/api/products?page=1&pageSize=10&categoryId=&search=` | Paged product list (`pageSize` capped at 50). `categoryId` filters by category; `search` matches name or SKU (case-insensitive). Returns `{ items, page, pageSize, totalCount }` |
| GET | `/api/products/{id}` | Get one product (404 if missing) |
| POST | `/api/products` | Create a product. `sku` (≤ 40 chars) and `categoryId` are required. 400 on invalid input or unknown category, 409 if the SKU already exists |

## Patterns

- **Minimal API** with `MapGroup("/api/products")` and named endpoints.
- **DTO separation** — `CreateProductRequest` is the API contract, kept distinct from the
  `Product` entity.
- `AsNoTracking()` on read queries for efficiency.
- Business rules enforced **in the database** too (unique SKU, FK, `CHECK`); the POST
  pre-checks them for clean 400/409 responses and maps Postgres violations for races.

## Storage

PostgreSQL, declared in the AppHost (`AddPostgres("postgres", port: 5433)` + volume
`ecommerce-catalog-pgdata`, database `catalogdb`). `AddNpgsqlDbContext` adds pooling,
retries, the `CatalogDbContext` health check and Npgsql/EF Core telemetry.

Schema changes (see the `ef-migration` skill in `.claude/skills/`):

```bash
dotnet ef migrations add <ExplicitName> --project src/ECommerce.Catalog.Api --output-dir Data/Migrations
dotnet ef migrations script <Previous> <ExplicitName> --project src/ECommerce.Catalog.Api -o migration.sql
```

Migrations are applied on startup in Development only.

## Tests

`tests/ECommerce.Catalog.Tests` runs the real app (`WebApplicationFactory<Program>`) against a
real PostgreSQL started by Testcontainers (`postgres:17-alpine`, see `CatalogApiFactory`). Every
run replays all migrations on a blank database. Docker must be running:

```bash
dotnet test tests/ECommerce.Catalog.Tests
```
