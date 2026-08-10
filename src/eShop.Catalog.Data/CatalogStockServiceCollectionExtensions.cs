using eShop.Shared.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace eShop.Catalog.Data;

/// <summary>
/// Composition root for hosts that also need the WCF-only stock and discount operations
/// (<see cref="ICatalogStockService"/>) on top of <see cref="ICatalogService"/> — today that is the
/// gRPC service, tomorrow anything that replaces the WinForms client's SOAP endpoint.
/// </summary>
/// <remarks>
/// It is a superset of <c>AddEShopCatalogServices</c>: that call brings the shared options and the
/// catalog services (mock or the EF Core 8 stack, including the HiLo id generator and the seeding
/// initializer), and only the stock and discount services are added on top.
/// </remarks>
public static class CatalogStockServiceCollectionExtensions
{
    public static IServiceCollection AddEShopCatalogWithStockServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddEShopCatalogServices(configuration);

        if (configuration.UseMockData())
        {
            services.TryAddSingleton<ICatalogStockService, MockCatalogStockService>();
        }
        else
        {
            services.TryAddScoped<ICatalogStockService, CatalogStockService>();
        }

        return services;
    }
}
