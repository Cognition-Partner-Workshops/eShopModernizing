using eShop.Shared.Diagnostics;
using eShop.Shared.Logging;
using eShop.Shared.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.UseEShopLogging("eShop.Web");
builder.AddEShopTelemetry("eShop.Web");
builder.Services.AddEShopHealthChecks();
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseEShopRequestLogging();
app.UseStaticFiles();
app.UseRouting();

app.MapEShopHealthChecks();

// Skeleton only: the catalog controllers and views are ported in NET-69.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
