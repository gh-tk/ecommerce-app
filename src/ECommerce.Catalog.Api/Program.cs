using ECommerce.Catalog.Api.Data;
using ECommerce.Catalog.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Aspire: telemetry, health checks, resilience, service discovery.
builder.AddServiceDefaults();

builder.Services.AddOpenApi();

// Aspire PostgreSQL integration: pooling, retries, DbContext health check, Npgsql/EF telemetry.
// "catalogdb" matches the database resource declared in the AppHost.
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

var app = builder.Build();

// Aspire default endpoints (/health, /alive).
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Apply pending migrations on the local database. Outside Development,
    // migrations go through a reviewed pipeline.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.MigrateAsync();
}

app.MapCatalogEndpoints();

app.Run();

// Exposes the generated Program class to WebApplicationFactory in the test project.
public partial class Program { }
