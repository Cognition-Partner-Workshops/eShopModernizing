using eShop.Shared.HealthChecks;
using eShop.Shared.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.AddEShopObservability("eShop.Web");

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapEShopHealthChecks();

app.Run();

/// <summary>Entry point; exposed as a partial class so integration tests can host the UI.</summary>
public partial class Program
{
}
