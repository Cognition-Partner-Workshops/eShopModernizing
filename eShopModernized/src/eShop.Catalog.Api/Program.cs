using eShop.Shared.DependencyInjection;

using eShop.Shared.HealthChecks;
using eShop.Shared.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.AddEShopConfiguration();

builder.AddEShopObservability("eShop.Catalog.Api");

builder.Services.AddControllers();
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
