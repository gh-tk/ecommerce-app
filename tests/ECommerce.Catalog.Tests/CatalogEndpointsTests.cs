// Tests d'integration des endpoints du catalogue.
//
// On demarre l'application reelle (WebApplicationFactory) contre un vrai PostgreSQL
// (Testcontainers, voir CatalogApiFactory) et on l'interroge par HTTP. On teste des
// COMPORTEMENTS OBSERVABLES, pas des details d'implementation : c'est ce qui rend ces
// tests utiles quand le code interne change.
//
// La base est partagee entre les tests : chaque produit cree utilise un SKU unique, et
// aucun test ne suppose un nombre exact de produits hors seed.
using System.Net;
using System.Net.Http.Json;
using ECommerce.Catalog.Api.Endpoints;
using ECommerce.Catalog.Api.Models;
using Xunit;

namespace ECommerce.Catalog.Tests;

[Collection(CatalogDatabaseCollection.Name)]
public class CatalogEndpointsTests(CatalogApiFactory factory)
{
    // ---------------------------------------------------------------- chemin nominal
    // L'API pagine : la liste est dans `items`.

    [Fact]
    public async Task GetProducts_RetourneUneListe()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<Product>>();
        Assert.NotNull(page);
        Assert.NotNull(page!.Items);
    }

    // ---------------------------------------------------------------- cas d'erreur
    // Le cas qui compte vraiment : un id inexistant doit rendre 404, PAS 200 avec un
    // corps vide ni 500. C'est le contrat que le Gateway et Ordering.Api consomment.

    [Fact]
    public async Task GetProductById_IdInexistant_Retourne404()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------------------------------------------------------------- cas limite
    // Un id non entier ne doit pas atteindre le handler : la contrainte de route
    // `{id:int}` doit le rejeter en 404, et surtout pas provoquer une exception.

    [Fact]
    public async Task GetProductById_IdNonEntier_NeProvoquePasDErreurServeur()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products/abc");

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // ---------------------------------------------------------------- creation + relecture
    // On verifie le cycle complet : la creation rend 201 avec un Location exploitable,
    // et la ressource est reellement lisible a cette adresse.
    // Nouveau contrat : Sku et CategoryId sont obligatoires.

    [Fact]
    public async Task CreateProduct_PuisLecture_RetourneLeProduitCree()
    {
        var client = factory.CreateClient();
        var requete = new { Sku = NouveauSku(), Name = "Clavier", Description = "AZERTY", Price = 49.90m, AvailableStock = 12, CategoryId = 1 };

        var creation = await client.PostAsJsonAsync("/api/products", requete);

        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        Assert.NotNull(creation.Headers.Location);

        var relecture = await client.GetAsync(creation.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, relecture.StatusCode);

        var produit = await relecture.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(produit);
        Assert.Equal("Clavier", produit!.Name);
        Assert.Equal(49.90m, produit.Price);
        Assert.Equal(12, produit.AvailableStock);
    }

    // ---------------------------------------------------------------- health check
    // Les probes Kubernetes (lab 08) dependent de cet endpoint : on le teste ici,
    // pas seulement une fois le cluster monte. Il inclut le check de la base.

    [Fact]
    public async Task Health_RepondOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    internal static string NouveauSku() => $"TEST-{Guid.NewGuid():N}"[..20];
}
