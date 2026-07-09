using Catalog.Infrastructure;
using Catalog.Shared.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Cross-cutting foundation (options + Serilog structured logging), replacing the
// legacy Autofac/Global.asax wiring and log4net configuration.
builder.Services.AddCatalogShared(builder.Configuration);

// EF Core 8 CatalogDbContext, replacing the EF6 CatalogDBContext.
builder.Services.AddCatalogInfrastructure(builder.Configuration);

builder.Services.AddRazorPages(options =>
{
    // Serve the catalog listing at the site root, mirroring the legacy
    // Web Forms Default.aspx "/" route.
    options.Conventions.AddPageRoute("/Catalog/Index", "");
});
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapHealthChecks("/health");

app.Run();
