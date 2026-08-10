using eShop.Catalog.Api.Pictures;
using eShop.Catalog.Data;
using eShop.Shared.Configuration;
using eShop.Shared.Diagnostics;
using eShop.Shared.Logging;
using eShop.Shared.Serialization;
using eShop.Shared.Telemetry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.UseEShopLogging("eShop.Catalog.Api");
builder.AddEShopTelemetry("eShop.Catalog.Api");
builder.AddEShopConfiguration();
builder.Services.AddEShopCatalogServices(builder.Configuration);
builder.Services.AddEShopHealthChecks();
builder.Services.AddCatalogPictures(builder.Configuration);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // The legacy Web API 2 surface emitted the property names as declared (PascalCase).
        var defaults = JsonDefaults.Options;
        options.JsonSerializerOptions.PropertyNamingPolicy = defaults.PropertyNamingPolicy;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = defaults.PropertyNameCaseInsensitive;
        options.JsonSerializerOptions.DefaultIgnoreCondition = defaults.DefaultIgnoreCondition;
        options.JsonSerializerOptions.NumberHandling = defaults.NumberHandling;
        options.JsonSerializerOptions.Encoder = defaults.Encoder;
        options.JsonSerializerOptions.WriteIndented = defaults.WriteIndented;
    });

// Legacy Web API 2 answered 400/404 with an empty body; ProblemDetails would change the contract.
builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressMapClientErrors = true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "eShop Catalog API",
    Version = "v1",
    Description = "Modernized port of the legacy eShop Web API 2 surface (brands, files and item pictures).",
}));

var app = builder.Build();

app.UseEShopRequestLogging();
app.UseRouting();

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "eShop Catalog API v1"));

// Port of WebApiConfig.Register: attribute routes plus the api/{controller}/{id} convention.
app.MapControllers();
app.MapControllerRoute(
    name: "DefaultApi",
    pattern: "api/{controller}/{id?}");

app.MapEShopHealthChecks();

app.Run();

/// <summary>Entry point, made public so integration tests can host the API in-process.</summary>
public partial class Program;
