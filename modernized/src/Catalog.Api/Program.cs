using Catalog.Infrastructure;
using Catalog.Shared.DependencyInjection;
using Catalog.Shared.Logging;

var builder = WebApplication.CreateBuilder(args);

// Cross-cutting foundation (strongly-typed settings + Serilog structured logging).
builder.Logging.AddCatalogLogging(builder.Configuration);
builder.Services.AddCatalogShared(builder.Configuration);

// EF Core 8 data layer (CatalogDbContext + SQL Server provider).
builder.Services.AddCatalogInfrastructure(builder.Configuration);

// Preserve the legacy Web API PascalCase JSON shape (e.g. {"Id":1,"Brand":"Azure"}).
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();

// Exposed so the integration test suite can drive the host via WebApplicationFactory<Program>.
public partial class Program
{
}
