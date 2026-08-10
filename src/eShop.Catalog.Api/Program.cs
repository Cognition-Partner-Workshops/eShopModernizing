using eShop.Catalog.Data;
using eShop.Shared.Configuration;
using eShop.Shared.Diagnostics;
using eShop.Shared.Logging;
using eShop.Shared.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.UseEShopLogging("eShop.Catalog.Api");
builder.AddEShopTelemetry("eShop.Catalog.Api");
builder.AddEShopConfiguration();
builder.Services.AddEShopCatalogServices(builder.Configuration);
builder.Services.AddEShopHealthChecks();
builder.Services.AddControllers();

var app = builder.Build();

app.UseEShopRequestLogging();
app.UseRouting();

// Port of WebApiConfig.Register: attribute routes plus the api/{controller}/{id} convention.
app.MapControllers();
app.MapControllerRoute(
    name: "DefaultApi",
    pattern: "api/{controller}/{id?}");

app.MapEShopHealthChecks();

app.Run();

/// <summary>Entry point, made public so integration tests can host the API in-process.</summary>
public partial class Program;
