using ECommerce.Catalog.Api.Data;
using ECommerce.Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ECommerce.Catalog.Api.Endpoints;

public static class CatalogEndpoints
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;
    private const int SkuMaxLength = 40;

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/categories", async (CatalogDbContext db) =>
            await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync())
            .WithTags("Categories")
            .WithName("GetCategories");

        var group = routes.MapGroup("/api/products").WithTags("Catalog");

        group.MapGet("/", async (
            CatalogDbContext db,
            int page = 1,
            int pageSize = DefaultPageSize,
            int? categoryId = null,
            string? search = null) =>
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = db.Products.AsNoTracking();

            if (categoryId is not null)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{EscapeLikePattern(search.Trim())}%";
                query = query.Where(p => EF.Functions.ILike(p.Name, pattern) || EF.Functions.ILike(p.Sku, pattern));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Product>(items, page, pageSize, totalCount);
        })
        .WithName("GetProducts");

        group.MapGet("/{id:int}", async (int id, CatalogDbContext db) =>
            await db.Products.FindAsync(id) is { } product
                ? Results.Ok(product)
                : Results.NotFound())
            .WithName("GetProductById");

        group.MapPost("/", async (CreateProductRequest request, CatalogDbContext db) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var sku = request.Sku!.Trim();

            if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            {
                return UnknownCategory(request.CategoryId);
            }

            if (await db.Products.AnyAsync(p => p.Sku == sku))
            {
                return DuplicateSku(sku);
            }

            var product = new Product
            {
                Sku = sku,
                Name = request.Name!.Trim(),
                Description = request.Description,
                Price = request.Price,
                AvailableStock = request.AvailableStock,
                CategoryId = request.CategoryId!.Value
            };

            db.Products.Add(product);

            try
            {
                await db.SaveChangesAsync();
            }
            // A concurrent request may have won the race between the checks above and the insert.
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return DuplicateSku(sku);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
            {
                return UnknownCategory(request.CategoryId);
            }

            return Results.CreatedAtRoute("GetProductById", new { id = product.Id }, product);
        })
        .WithName("CreateProduct");

        return routes;
    }

    private static Dictionary<string, string[]> Validate(CreateProductRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            errors["sku"] = ["Sku is required."];
        }
        else if (request.Sku.Trim().Length > SkuMaxLength)
        {
            errors["sku"] = [$"Sku must be at most {SkuMaxLength} characters."];
        }

        if (request.CategoryId is null)
        {
            errors["categoryId"] = ["CategoryId is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["name"] = ["Name is required."];
        }

        if (request.Price < 0)
        {
            errors["price"] = ["Price must be greater than or equal to 0."];
        }

        if (request.AvailableStock < 0)
        {
            errors["availableStock"] = ["AvailableStock must be greater than or equal to 0."];
        }

        return errors;
    }

    private static IResult UnknownCategory(int? categoryId) =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["categoryId"] = [$"Category {categoryId} does not exist."]
        });

    private static IResult DuplicateSku(string sku) =>
        Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Duplicate SKU",
            detail: $"A product with SKU '{sku}' already exists.");

    // ILIKE treats '%', '_' and '\' (the default escape character) as special.
    private static string EscapeLikePattern(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}

public record CreateProductRequest(
    string? Name,
    string? Description,
    decimal Price,
    int AvailableStock,
    string? Sku,
    int? CategoryId);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
