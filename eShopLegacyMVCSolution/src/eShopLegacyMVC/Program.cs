using eShopLegacyMVC.Models;
using eShopLegacyMVC.Models.Infrastructure;
using eShopLegacyMVC.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Add MVC
builder.Services.AddControllersWithViews();

// Configuration
var useMockData = bool.Parse(builder.Configuration["UseMockData"] ?? "true");
var useCustomizationData = bool.Parse(builder.Configuration["UseCustomizationData"] ?? "false");

// Register services
if (useMockData)
{
    builder.Services.AddSingleton<ICatalogService, CatalogServiceMock>();
}
else
{
    builder.Services.AddScoped<ICatalogService, CatalogService>();
}

builder.Services.AddDbContext<CatalogDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDBContext")));

builder.Services.AddScoped<CatalogDBInitializer>();
builder.Services.AddSingleton<CatalogItemHiLoGenerator>();

// Add IHttpContextAccessor for session access in views
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Initialize database if not using mock data
if (!useMockData)
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDBContext>();
        var initializer = scope.ServiceProvider.GetRequiredService<CatalogDBInitializer>();
        dbContext.Database.EnsureCreated();
        initializer.Seed(dbContext);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

app.MapControllers(); // For attribute-routed controllers (PicController, API controllers)

app.Run();
