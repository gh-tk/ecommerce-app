// Fabrique de test : demarre l'application reelle contre un VRAI PostgreSQL.
//
// Un conteneur postgres:17-alpine est lance par Testcontainers, sa chaine de connexion est
// injectee sous ConnectionStrings:catalogdb (la cle que lit AddNpgsqlDbContext), et
// l'application applique elle-meme les migrations au demarrage (environnement Development).
// Les tests rejouent donc toute la chaine de migrations sur une base vierge.
//
// Pre-requis : Docker doit tourner sur le poste ou l'agent CI.
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Xunit;

namespace ECommerce.Catalog.Tests;

public sealed class CatalogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("catalogdb")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:catalogdb", _postgres.GetConnectionString());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

// Un seul conteneur pour toutes les classes de test du catalogue : demarrer PostgreSQL
// coute quelques secondes, on ne le paie qu'une fois.
[CollectionDefinition(Name)]
public sealed class CatalogDatabaseCollection : ICollectionFixture<CatalogApiFactory>
{
    public const string Name = "catalog-db";
}
