using System.IO;
using System.Globalization;
using System.IO.Compression;
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
                builder.Configuration["CatalogApi:BaseUrl"]));

            var app = builder.Build();

            var culture = CultureInfo.GetCultureInfo(builder.Configuration["Culture"] ?? "en-US");
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(culture),
                SupportedCultures = new[] { culture },
                SupportedUICultures = new[] { culture },
            });
            app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

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

            if (useCustomizationData)
            {
                ExtractCustomPictures(app.Environment.ContentRootPath);
            }

            app.Run();
        }

        private static void ExtractCustomPictures(string contentRootPath)
        {
            var zip = Path.Combine(contentRootPath, "Setup", "CatalogItems.zip");
            if (!File.Exists(zip))
            {
                return;
            }

            var pics = Path.Combine(contentRootPath, "Pics");
            Directory.CreateDirectory(pics);
            foreach (var file in Directory.GetFiles(pics))
            {
                File.Delete(file);
            }
            ZipFile.ExtractToDirectory(zip, pics);
        }
    }
}
