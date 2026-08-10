using eShop.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace eShop.Catalog.Data;

/// <summary>
/// Composition root for the catalog. This is the direct replacement for the Autofac
/// <c>ApplicationModule</c> of the MVC and Web Forms applications and preserves its lifetimes:
/// the mock service was <c>SingleInstance</c>, the database-backed service and the database
/// initializer were <c>InstancePerLifetimeScope</c>.
/// </summary>
/// <remarks>
/// It lives in <c>eShop.Catalog.Data</c> rather than <c>eShop.Shared</c> because the shared library
/// sits below the data layer in the reference direction and must not know about
/// <see cref="ICatalogService"/>. Hosts still wire up with a single call.
/// </remarks>
public static class CatalogServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared eShop options plus the catalog services, choosing the in-memory or the
    /// database-backed <see cref="ICatalogService"/> from <c>Catalog:UseMockData</c>.
    /// </summary>
    public static IServiceCollection AddEShopCatalogServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddEShopServices(configuration);

        if (configuration.UseMockData())
        {
            services.TryAddSingleton<ICatalogService, MockCatalogService>();
        }
        else
        {
            // TODO(NET-64): replace with the EF Core 8 CatalogService + CatalogDbContext.
            services.TryAddScoped<ICatalogService, PendingDatabaseCatalogService>();
            services.TryAddScoped<ICatalogDatabaseInitializer, NoOpCatalogDatabaseInitializer>();
            services.AddHostedService<CatalogDatabaseInitializerHostedService>();
        }

        return services;
    }
}
