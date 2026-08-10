var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

var app = builder.Build();

// Skeleton only: the catalog .proto contract and its service implementation arrive in NET-66.
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();
