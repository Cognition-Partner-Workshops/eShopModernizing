var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Skeleton only: routing, DI, configuration and the catalog endpoints arrive in NET-61/NET-67.
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();

/// <summary>Entry point, made public so integration tests can host the API in-process.</summary>
public partial class Program;
