using eShop.Catalog.Data;
using eShop.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddEShopConfiguration();
builder.Services.AddEShopCatalogServices(builder.Configuration);

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Port of FilterConfig.RegisterGlobalFilters: HandleErrorAttribute becomes the exception handler
// middleware (the developer exception page stays on in Development).
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// Port of BundleConfig.RegisterBundles: bundling/minification is replaced by static file serving;
// the catalog views and their assets are ported in NET-69.
app.UseStaticFiles();
app.UseRouting();

// Port of RouteConfig.RegisterRoutes. The legacy default route is Catalog/Index; the catalog
// controller itself arrives with the UI port (NET-69), so Home stays the default until then.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

/// <summary>Entry point, made public so integration tests can host the web app in-process.</summary>
public partial class Program;
