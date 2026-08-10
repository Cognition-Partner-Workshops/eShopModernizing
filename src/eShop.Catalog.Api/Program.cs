using eShop.Shared.Diagnostics;
using eShop.Shared.Logging;
using eShop.Shared.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.UseEShopLogging("eShop.Catalog.Api");
builder.AddEShopTelemetry("eShop.Catalog.Api");
builder.Services.AddEShopHealthChecks();

var app = builder.Build();

app.UseEShopRequestLogging();

// Skeleton only: routing, DI, configuration and the catalog endpoints arrive in NET-61/NET-67.
app.MapEShopHealthChecks();

app.Run();

/// <summary>Entry point, made public so integration tests can host the API in-process.</summary>
public partial class Program;
