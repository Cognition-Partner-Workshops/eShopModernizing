using eShop.Shared.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.AddEShopConfiguration();

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
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();

/// <summary>Entry point; exposed as a partial class so integration tests can host the API.</summary>
public partial class Program
{
}
