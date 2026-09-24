using System;
using eShopLegacyMVC.Services;
using eShopLegacyMVC.Services.Catalog;
using Microsoft.Extensions.DependencyInjection;

namespace eShopLegacyMVC.Modules
{
    public class ApplicationModule
    {
        public ApplicationModule(bool useMockData, string catalogApiBaseUrl)
        {
            UseMockData = useMockData;
            CatalogApiBaseUrl = catalogApiBaseUrl;
        }

        public bool UseMockData { get; }
        public string CatalogApiBaseUrl { get; }

        public void Load(IServiceCollection services)
        {
            if (UseMockData)
            {
                services.AddSingleton<ICatalogService, CatalogServiceMock>();
                return;
            }

            if (string.IsNullOrWhiteSpace(CatalogApiBaseUrl))
            {
                throw new InvalidOperationException("CatalogApi:BaseUrl must be configured when UseMockData is false.");
            }

            services.AddHttpClient<CatalogApiClient>(client =>
            {
                client.BaseAddress = new Uri(CatalogApiBaseUrl.TrimEnd('/') + "/");
            });
            services.AddScoped<ICatalogService, CatalogHttpService>();
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
