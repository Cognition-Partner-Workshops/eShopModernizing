using eShop.Catalog.Data;
using eShop.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddEShopConfiguration();
builder.Services.AddEShopCatalogServices(builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

app.UseRouting();

// Port of WebApiConfig.Register: attribute routes plus the api/{controller}/{id} convention.
app.MapControllers();
app.MapControllerRoute(
    name: "DefaultApi",
    pattern: "api/{controller}/{id?}");

// TODO(NET-62): replaced by the real health checks endpoint.
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();

/// <summary>Entry point, made public so integration tests can host the API in-process.</summary>
public partial class Program;
