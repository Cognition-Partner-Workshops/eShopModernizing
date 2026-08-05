using eShop.Catalog.Data.DependencyInjection;
using eShop.Catalog.Grpc.Services;
using eShop.Shared.DependencyInjection;

using eShop.Shared.HealthChecks;
using eShop.Shared.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.AddEShopConfiguration();

builder.AddEShopObservability("eShop.Catalog.Grpc");

builder.Services.AddCatalogData(builder.Configuration);

builder.Services.AddGrpc();

// Reflection lets grpcurl list and invoke the service without a local copy of catalog.proto,
// which is how the NET-73 parity gate drives it.
builder.Services.AddGrpcReflection();

var app = builder.Build();

app.MapGrpcService<CatalogGrpcService>();
app.MapGrpcReflectionService();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");
app.MapEShopHealthChecks();

app.Run();

/// <summary>Entry point; exposed as a partial class so integration tests can host the service.</summary>
public partial class Program
{
}
