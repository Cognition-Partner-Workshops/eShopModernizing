using eShop.Catalog.Data;
using eShop.Shared.Configuration;
using eShop.Shared.Diagnostics;
using eShop.Shared.Logging;
using eShop.Shared.Telemetry;
using eShop.Web.Configuration;
using eShop.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.UseEShopLogging("eShop.Web");
builder.AddEShopTelemetry("eShop.Web");
builder.AddEShopConfiguration();
builder.Services.AddEShopCatalogServices(builder.Configuration);
builder.Services.AddEShopHealthChecks();
builder.Services.Configure<CatalogWebOptions>(builder.Configuration.GetSection(CatalogWebOptions.SectionName));

// Replaces the InProc session state: Session["MachineName"] / Session["SessionStartTime"] were
// only rendered in the footer, so they become process-wide values instead of per-session ones.
builder.Services.AddSingleton<HostInfo>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseEShopRequestLogging();

// Port of FilterConfig.RegisterGlobalFilters: HandleErrorAttribute becomes the exception handler
// middleware (the developer exception page stays on in Development).
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// Port of BundleConfig.RegisterBundles: bundling/minification is replaced by plain static files
// under wwwroot (css/, js/, images/, fonts/), served by the static-file middleware.
app.UseStaticFiles();
app.UseRouting();

app.MapEShopHealthChecks();

// Port of RouteConfig.RegisterRoutes: the catalog index is the application root, which is what
// makes the post-redirects resolve to "/" exactly as the legacy app did.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

// Aliases for the two retired Web Forms URLs that have no identical MVC counterpart
// (webforms-retirement.md); the other Web Forms routes already match the MVC ones.
string[] readMethods = ["GET", "HEAD"];
app.MapMethods("/Default", readMethods, () => Results.Redirect("/Catalog/Index", permanent: true));
app.MapMethods(
    "/Default/index/{pageIndex:int}/size/{pageSize:int}",
    readMethods,
    (int pageIndex, int pageSize) => Results.Redirect(
        $"/Catalog/Index?pageIndex={pageIndex}&pageSize={pageSize}",
        permanent: true));

app.Run();

/// <summary>Entry point, made public so integration tests can host the web app in-process.</summary>
public partial class Program;
