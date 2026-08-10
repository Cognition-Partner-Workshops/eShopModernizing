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
/// It is a superset of <c>AddEShopCatalogServices</c>: the EF Core 8 registrations from NET-64 are
/// applied first so that in database mode the real <see cref="CatalogService"/> wins over the
/// placeholder <c>TryAdd</c> registrations, and <c>AddEShopCatalogServices</c> is then called to
/// bind the shared options.
/// </remarks>
public static class CatalogStockServiceCollectionExtensions
{
    public static IServiceCollection AddEShopCatalogWithStockServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (configuration.UseMockData())
        {
            services.TryAddSingleton<ICatalogStockService, MockCatalogStockService>();
        }
        else
        {
            var connectionString = configuration.GetConnectionString(CatalogDataServiceCollectionExtensions.ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{CatalogDataServiceCollectionExtensions.ConnectionStringName}' is not configured. " +
                    $"Set ConnectionStrings__{CatalogDataServiceCollectionExtensions.ConnectionStringName} or enable " +
                    $"{CatalogOptions.SectionName}:{nameof(CatalogOptions.UseMockData)} to run against the mock data set.");

            services.AddDbContext<CatalogDbContext>(options => options.UseSqlServer(connectionString));
            services.TryAddScoped<ICatalogItemIdGenerator, MaxCatalogItemIdGenerator>();
            services.TryAddScoped<ICatalogService, CatalogService>();
            services.TryAddScoped<ICatalogStockService, CatalogStockService>();
        }

        services.AddEShopCatalogServices(configuration);

        return services;
    }
}
