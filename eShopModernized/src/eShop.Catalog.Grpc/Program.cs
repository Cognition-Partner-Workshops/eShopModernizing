using eShop.Shared.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.AddEShopConfiguration();

builder.Services.AddGrpc();

var app = builder.Build();

// Catalog service registrations and Protos/catalog.proto are added by the WCF-to-gRPC ticket.
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();

/// <summary>Entry point; exposed as a partial class so integration tests can host the service.</summary>
public partial class Program
{
}
