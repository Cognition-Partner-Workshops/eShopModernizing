using System.Globalization;
using eShopModernizedMVC.Models;
using eShopModernizedMVC.Models.Infrastructure;
using eShopModernizedMVC.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");
builder.Services.AddControllersWithViews().AddJsonOptions(options =>
    options.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
var useMockData = builder.Configuration.GetValue<bool>("UseMockData");
if (useMockData)
{
    builder.Services.AddSingleton<ICatalogService, CatalogServiceMock>();
}
else
{
    builder.Services.AddDbContext<CatalogDBContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDBContext")));
    builder.Services.AddScoped<ICatalogService, CatalogService>();
    builder.Services.AddScoped<CatalogDBInitializer>();
}

var app = builder.Build();
if (!useMockData)
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<CatalogDBInitializer>()
        .Initialize(scope.ServiceProvider.GetRequiredService<CatalogDBContext>());
}
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Catalog/Error");
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.Use(async (context, next) =>
{
    app.Logger.LogDebug("WebApplication_BeginRequest");
    if (context.Session.GetString("MachineName") == null)
    {
        context.Session.SetString("MachineName", Environment.MachineName);
        context.Session.SetString("SessionStartTime", DateTime.Now.ToString());
    }
    await next();
});
app.MapControllers();
app.MapControllerRoute("Default", "{controller=Catalog}/{action=Index}/{id?}");
app.Run();

public partial class Program { }
