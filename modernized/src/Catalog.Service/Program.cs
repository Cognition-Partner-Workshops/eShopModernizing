using Catalog.Infrastructure;
using Catalog.Service.Services;
using Catalog.Shared.DependencyInjection;
using Catalog.Shared.Logging;

var builder = WebApplication.CreateBuilder(args);

// Structured logging (Serilog) replacing the legacy log4net setup.
builder.Logging.AddCatalogLogging(builder.Configuration);

// gRPC transport replacing the legacy WCF/SOAP host.
builder.Services.AddGrpc();

// Reuse the modernized foundation and EF Core 8 data layer.
builder.Services.AddCatalogShared(builder.Configuration);
builder.Services.AddCatalogInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGrpcService<CatalogGrpcService>();
app.MapHealthChecks("/health");
app.MapGet("/", () =>
    "Catalog gRPC service is running. Communicate with it using a gRPC client.");

app.Run();

// Exposed so integration/host-based tests can reference the entry point.
public partial class Program;
