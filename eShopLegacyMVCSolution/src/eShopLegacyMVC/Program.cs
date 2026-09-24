using System.IO;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.Models.Infrastructure;
using eShopLegacyMVC.Modules;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eShopLegacyMVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Logging.AddLog4Net(Path.Combine(builder.Environment.ContentRootPath, "log4Net.xml"));

            var useMockData = builder.Configuration.GetValue<bool>("UseMockData");
            var useCustomizationData = builder.Configuration.GetValue<bool>("UseCustomizationData");

            builder.Services.AddControllersWithViews()
                .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null);
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddApplicationModule(new ApplicationModule(
                useMockData,
                useCustomizationData,
                builder.Configuration.GetConnectionString("CatalogDBContext")));

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Catalog/Error");
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.Use(async (context, next) =>
            {
                SessionInfo.EnsureInitialized(context.Session);
                LogicalThreadContext.Properties["activityid"] = new ActivityIdHelper();
                LogicalThreadContext.Properties["requestinfo"] = new WebRequestInfo(context);
                await next();
            });

            app.MapControllers();
            app.MapControllerRoute(
                name: "Default",
                pattern: "{controller=Catalog}/{action=Index}/{id?}");

            if (!useMockData)
            {
                using var scope = app.Services.CreateScope();
                scope.ServiceProvider.GetRequiredService<CatalogDBInitializer>().Initialize();
            }

            app.Run();
        }
    }
}
