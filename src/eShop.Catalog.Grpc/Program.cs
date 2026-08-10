using eShop.Catalog.Data;
using eShop.Catalog.Grpc.Services;
using eShop.Shared.Configuration;
using eShop.Shared.Diagnostics;
using eShop.Shared.Logging;
using eShop.Shared.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.UseEShopLogging("eShop.Catalog.Grpc");
builder.AddEShopTelemetry("eShop.Catalog.Grpc");
builder.AddEShopConfiguration();

// Catalog CRUD (ICatalogService) plus the WCF-only stock/discount operations
// (ICatalogStockService). Runs entirely in memory when Catalog:UseMockData is true.
builder.Services.AddEShopCatalogWithStockServices(builder.Configuration);
builder.Services.AddEShopHealthChecks();
builder.Services.AddGrpc();

// Server reflection so grpcurl (and any other dynamic client) can list and describe the service
// the way `?wsdl` / the `mex` endpoint did for the legacy WCF service.
builder.Services.AddGrpcReflection();

var app = builder.Build();

app.UseEShopRequestLogging();

app.MapGrpcService<CatalogGrpcService>();
app.MapGrpcReflectionService();
app.MapEShopHealthChecks();

app.Run();

/// <summary>Entry point, made public so integration tests can host the service in-process.</summary>
public partial class Program;
