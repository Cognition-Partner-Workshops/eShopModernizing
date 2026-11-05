using eShopWcf.Catalog.Data;
using eShopWcf.Catalog.Grpc.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
if (string.IsNullOrEmpty(builder.Configuration["urls"]))
{
    builder.WebHost.UseUrls("http://localhost:5200");
}

builder.WebHost.ConfigureKestrel(options => options.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http2));
var connectionString = Environment.GetEnvironmentVariable("ConnectionString")
    ?? builder.Configuration.GetConnectionString("CatalogDb")
    ?? @"Server=(localdb)\MSSQLLocalDB;Database=eShopDatabase;Integrated Security=true;TrustServerCertificate=true";
builder.Services.AddDbContext<CatalogDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<CatalogService>();
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddHealthChecks();

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    await CatalogDatabaseInitializer.InitializeAsync(
        scope.ServiceProvider.GetRequiredService<CatalogDbContext>(),
        app.Configuration["Seed:Dataset"] ?? "Legacy");
}

app.MapGrpcService<CatalogGrpcService>();
app.MapGrpcReflectionService();
app.MapHealthChecks("/health");
await app.RunAsync();

public partial class Program;
