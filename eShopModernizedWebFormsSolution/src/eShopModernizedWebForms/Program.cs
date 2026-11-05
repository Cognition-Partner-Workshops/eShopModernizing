using System.Globalization;
using eShopModernizedWebForms.Models;
using eShopModernizedWebForms.Models.Infrastructure;
using eShopModernizedWebForms.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var useMockData = builder.Configuration.GetValue<bool>("UseMockData");
var useCustomizationData = builder.Configuration.GetValue<bool>("UseCustomizationData");

builder.Services.AddRazorPages(options =>
{
    // Public Web Forms URLs from the legacy RouteConfig plus the physical .aspx paths.
    options.Conventions.AddPageRoute("/Index", "Default");
    options.Conventions.AddPageRoute("/Index", "Default.aspx");
    options.Conventions.AddPageRoute("/Index", "Default/index/{index}/size/{size}");
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

if (useMockData)
{
    builder.Services.AddSingleton<ICatalogService, CatalogServiceMock>();
}
else
{
    builder.Services.AddDbContext<CatalogDBContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDBContext")));
    builder.Services.AddSingleton<CatalogItemHiLoGenerator>();
    builder.Services.AddScoped<ICatalogService, CatalogService>();
}

var app = builder.Build();

if (!useMockData)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<CatalogDBContext>();
    var initializer = new CatalogDBInitializer(
        scope.ServiceProvider.GetRequiredService<CatalogItemHiLoGenerator>(),
        useCustomizationData,
        app.Environment.ContentRootPath,
        app.Environment.WebRootPath);
    initializer.InitializeDatabase(context);
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Legacy Global.Session_Start: machine name and start time shown in the Site.Master footer.
app.Use(async (context, next) =>
{
    if (context.Session.GetString("MachineName") == null)
    {
        context.Session.SetString("MachineName", Environment.MachineName);
        context.Session.SetString("SessionStartTime", DateTime.Now.ToString(CultureInfo.CurrentCulture));
    }

    await next(context);
});

app.MapRazorPages();

app.Run();

public partial class Program
{
}
