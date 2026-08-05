using System.Globalization;
using eShop.Catalog.Data.DependencyInjection;
using eShop.Shared.DependencyInjection;

using eShop.Shared.HealthChecks;
using eShop.Shared.Telemetry;

// Prices are rendered with {0:C}. The legacy application ran under en-US; pin the culture so the
// container locale cannot turn the amounts into the invariant "¤19.50".
var culture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

builder.AddEShopConfiguration();

builder.AddEShopObservability("eShop.Web");

builder.Services.AddCatalogData(builder.Configuration);

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

// Legacy RouteConfig default: /Catalog and / both land on the catalog list, and
// /Catalog/{action}/{id} keeps the legacy detail, create, edit and delete URLs.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");
app.MapEShopHealthChecks();

app.Run();

/// <summary>Entry point; exposed as a partial class so integration tests can host the UI.</summary>
public partial class Program
{
}
