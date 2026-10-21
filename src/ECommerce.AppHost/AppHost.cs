var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL for the Catalog — fixed host port and a named volume so data survives restarts.
var postgres = builder.AddPostgres("postgres", port: 5433)
    .WithDataVolume("ecommerce-catalog-pgdata");

var catalogDb = postgres.AddDatabase("catalogdb");

// Catalog microservice — starts once its database is ready.
var catalog = builder.AddProject<Projects.ECommerce_Catalog_Api>("catalog")
    .WithReference(catalogDb)
    .WaitFor(catalogDb);

// Ordering microservice — depends on Catalog (resolved via service discovery).
var ordering = builder.AddProject<Projects.ECommerce_Ordering_Api>("ordering")
    .WithReference(catalog)
    .WaitFor(catalog);

// API Gateway (YARP) — the single entry point for the services.
var gateway = builder.AddProject<Projects.ECommerce_Gateway>("gateway")
    .WithReference(catalog)
    .WithReference(ordering)
    .WaitFor(catalog)
    .WaitFor(ordering);

// Blazor web frontend — talks to the services through the gateway.
builder.AddProject<Projects.ECommerce_Web>("web")
    .WithReference(gateway)
    .WaitFor(gateway)
    .WithExternalHttpEndpoints();

builder.Build().Run();
