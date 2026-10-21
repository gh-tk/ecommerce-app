// Tests du contrat introduit avec PostgreSQL : categories, filtres, et regles metier
// garanties par la base (SKU unique, categorie existante).
//
// Les assertions s'appuient sur le seed synthetique de la migration InitialCreate :
// categories 1 Peripherals, 2 Displays, 3 Accessories ; produits KB-MECH-001 et
// MS-WL-001 (categorie 1), MON-4K-27 (2), HUB-USBC-7 (3).
using System.Net;
using System.Net.Http.Json;
using ECommerce.Catalog.Api.Endpoints;
using ECommerce.Catalog.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ECommerce.Catalog.Tests;

[Collection(CatalogDatabaseCollection.Name)]
public class CatalogPostgresContractTests(CatalogApiFactory factory)
{
    // ---------------------------------------------------------------- categories

    [Fact]
    public async Task GetCategories_RetourneLesCategoriesDuSeedTrieesParNom()
    {
        var client = factory.CreateClient();

        var categories = await client.GetFromJsonAsync<List<Category>>("/api/categories");

        Assert.NotNull(categories);
        Assert.Equal(["Accessories", "Displays", "Peripherals"], categories!.Select(c => c.Name));
    }

    // ---------------------------------------------------------------- filtres

    [Fact]
    public async Task GetProducts_FiltreParCategorie_NeRetourneQueCetteCategorie()
    {
        var client = factory.CreateClient();

        var page = await client.GetFromJsonAsync<PagedResult<Product>>("/api/products?categoryId=1&pageSize=50");

        Assert.NotNull(page);
        Assert.All(page!.Items, p => Assert.Equal(1, p.CategoryId));
        var skus = page.Items.Select(p => p.Sku).ToList();
        Assert.Contains("KB-MECH-001", skus);
        Assert.Contains("MS-WL-001", skus);
        Assert.DoesNotContain("MON-4K-27", skus);
        Assert.Equal(page.Items.Count, page.TotalCount);
    }

    [Fact]
    public async Task GetProducts_Recherche_EstInsensibleALaCasse()
    {
        var client = factory.CreateClient();

        var page = await client.GetFromJsonAsync<PagedResult<Product>>("/api/products?search=MONITOR");

        Assert.NotNull(page);
        var produit = Assert.Single(page!.Items);
        Assert.Equal("4K Monitor", produit.Name);
    }

    // ---------------------------------------------------------------- regles metier

    [Fact]
    public async Task CreateProduct_SkuDuplique_Retourne409()
    {
        var client = factory.CreateClient();
        var sku = CatalogEndpointsTests.NouveauSku();
        var requete = new { Sku = sku, Name = "Souris", Price = 19.90m, AvailableStock = 5, CategoryId = 1 };

        var premiere = await client.PostAsJsonAsync("/api/products", requete);
        var seconde = await client.PostAsJsonAsync("/api/products", requete with { });

        Assert.Equal(HttpStatusCode.Created, premiere.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, seconde.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_SkuDuSeed_Retourne409()
    {
        var client = factory.CreateClient();
        var requete = new { Sku = "KB-MECH-001", Name = "Copie", Price = 1m, AvailableStock = 1, CategoryId = 1 };

        var response = await client.PostAsJsonAsync("/api/products", requete);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_CategorieInexistante_Retourne400()
    {
        var client = factory.CreateClient();
        var sku = CatalogEndpointsTests.NouveauSku();
        var requete = new { Sku = sku, Name = "Orphelin", Price = 10m, AvailableStock = 1, CategoryId = 999 };

        var response = await client.PostAsJsonAsync("/api/products", requete);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var probleme = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.True(probleme!.Errors.ContainsKey("categoryId"));

        // Rien ne doit avoir ete insere.
        var recherche = await client.GetFromJsonAsync<PagedResult<Product>>($"/api/products?search={sku}");
        Assert.Empty(recherche!.Items);
    }
}
