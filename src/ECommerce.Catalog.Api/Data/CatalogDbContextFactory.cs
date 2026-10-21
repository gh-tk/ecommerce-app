using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Catalog.Api.Data;

/// <summary>
/// Used only by the dotnet-ef tooling (migrations add/script), where Aspire does not inject
/// a connection string. Generating a migration never connects, so no credentials are needed.
/// </summary>
public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql("Host=localhost;Database=catalogdb")
            .Options;

        return new CatalogDbContext(options);
    }
}
