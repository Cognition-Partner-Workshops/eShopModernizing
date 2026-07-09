using Catalog.Infrastructure;
using Catalog.Shared.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Cross-cutting foundation (options + Serilog structured logging), replacing the
// legacy Autofac/Global.asax wiring and log4net configuration.
builder.Services.AddCatalogShared(builder.Configuration);

// EF Core 8 CatalogDbContext, replacing the EF6 CatalogDBContext.
builder.Services.AddCatalogInfrastructure(builder.Configuration);

builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

// Preserve the legacy MVC listing at the site root ("/" -> Catalog/Index),
// then the conventional {controller}/{action}/{id?} route for the CRUD actions.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

app.MapHealthChecks("/health");

app.Run();

public partial class Program;
