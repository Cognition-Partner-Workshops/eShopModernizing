using eShop.Catalog.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var useMockData = builder.Configuration.GetValue<bool>("UseMockData");
var useCustomizationData = builder.Configuration.GetValue<bool>("UseCustomizationData");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (useMockData)
{
    builder.Services.AddDbContext<CatalogContext>(o => o.UseInMemoryDatabase("Catalog"));
}
else
{
    builder.Services.AddDbContext<CatalogContext>(o =>
        o.UseSqlServer(builder.Configuration.GetConnectionString("CatalogContext"),
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", CatalogContext.Schema)));
}

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    if (context.Database.IsRelational())
    {
        context.Database.Migrate();
    }
    else
    {
        context.Database.EnsureCreated();
    }
    CatalogContextSeed.Seed(context, app.Environment, useCustomizationData, logger);
}

app.Run();

public partial class Program { }
