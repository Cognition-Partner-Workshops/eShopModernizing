using eShop.Catalog.Data.DependencyInjection;
using eShop.Catalog.Data.Seeding;
using eShop.Shared.DependencyInjection;

using eShop.Shared.HealthChecks;
using eShop.Shared.Serialization;
using eShop.Shared.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.AddEShopConfiguration();

builder.AddEShopObservability("eShop.Catalog.Api");

builder.Services.AddCatalogData(builder.Configuration);

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Legacy Web API 2 / MVC returned bodiless 400s and 404s; keep them bodiless rather than
        // wrapping them in ProblemDetails, which would change the recorded response shapes.
        options.SuppressMapClientErrors = true;
    })
    .AddJsonOptions(options =>
    {
        // The golden baseline payloads are PascalCase; camelCasing them is a silent parity break.
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonSerializing.Options.PropertyNamingPolicy;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = JsonSerializing.Options.PropertyNameCaseInsensitive;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonSerializing.Options.DefaultIgnoreCondition;
        options.JsonSerializerOptions.WriteIndented = JsonSerializing.Options.WriteIndented;
    });

builder.AddCatalogSeeding();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapEShopHealthChecks();

app.Run();

/// <summary>Entry point; exposed as a partial class so integration tests can host the API.</summary>
public partial class Program
{
}
