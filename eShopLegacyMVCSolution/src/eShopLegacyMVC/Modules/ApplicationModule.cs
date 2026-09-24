using eShopLegacyMVC.Models;
using eShopLegacyMVC.Models.Infrastructure;
using eShopLegacyMVC.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eShopLegacyMVC.Modules
{
    public class ApplicationModule
    {
        public ApplicationModule(bool useMockData, bool useCustomizationData, string connectionString)
        {
            UseMockData = useMockData;
            UseCustomizationData = useCustomizationData;
            ConnectionString = connectionString;
        }

        public bool UseMockData { get; }
        public bool UseCustomizationData { get; }
        public string ConnectionString { get; }

        public void Load(IServiceCollection services)
        {
            if (UseMockData)
            {
                services.AddSingleton<ICatalogService, CatalogServiceMock>();
            }
            else
            {
                services.AddScoped<ICatalogService, CatalogService>();
            }

            services.AddDbContext<CatalogDBContext>(options => options.UseSqlServer(ConnectionString));
            services.AddScoped(sp => new CatalogDBInitializer(
                sp.GetRequiredService<CatalogDBContext>(),
                sp.GetRequiredService<CatalogItemHiLoGenerator>(),
                sp.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(),
                UseCustomizationData));
            services.AddSingleton<CatalogItemHiLoGenerator>();
        }
    }

    public static class ApplicationModuleExtensions
    {
        public static IServiceCollection AddApplicationModule(this IServiceCollection services, ApplicationModule module)
        {
            module.Load(services);
            return services;
        }
    }
}
