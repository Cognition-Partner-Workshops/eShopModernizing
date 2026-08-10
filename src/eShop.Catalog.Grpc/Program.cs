using eShop.Shared.Diagnostics;
using eShop.Shared.Logging;
using eShop.Shared.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.UseEShopLogging("eShop.Catalog.Grpc");
builder.AddEShopTelemetry("eShop.Catalog.Grpc");
builder.Services.AddEShopHealthChecks();
builder.Services.AddGrpc();

var app = builder.Build();

app.UseEShopRequestLogging();

// Skeleton only: the catalog .proto contract and its service implementation arrive in NET-66.
app.MapEShopHealthChecks();

app.Run();
