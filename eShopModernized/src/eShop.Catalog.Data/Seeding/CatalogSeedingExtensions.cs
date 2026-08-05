using eShop.Catalog.Data.DependencyInjection;
using eShop.Shared.Configuration;
using eShop.Shared.DependencyInjection;
using eShop.Shared.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eShop.Catalog.Data.Seeding;

/// <summary>
/// Registers the startup migration + seeding path and the catalog database readiness check.
/// </summary>
public static class CatalogSeedingExtensions
{
    /// <summary>
    /// Adds the catalog data layer (when the host has not registered it already) plus the
    /// <see cref="IDataInitializer"/> that migrates and seeds the database at startup. A no-op when
    /// <see cref="CatalogOptions.UseMockData"/> is enabled, which is also when NET-61's hosted
    /// service skips initialization.
    /// </summary>
    public static TBuilder AddCatalogSeeding<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (bool.TryParse(builder.Configuration[CatalogDataServiceCollectionExtensions.UseMockDataKey], out var useMockData) && useMockData)
        {
            return builder;
        }

        var services = builder.Services;

        // AddDbContext is not idempotent, and sibling hosts may already have added the data layer.
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(CatalogDbContext)))
        {
            services.AddCatalogData(builder.Configuration);
        }

        services.TryAddSingleton<ICatalogHiLoSequence, CatalogHiLoSequence>();
        services.TryAddSingleton<CatalogItemHiLoGenerator>();
        services.TryAddSingleton<CatalogSeedingState>();

        services.TryAddSingleton(serviceProvider => CatalogSeedFolders.Resolve(
            serviceProvider.GetRequiredService<IHostEnvironment>().ContentRootPath,
            serviceProvider.GetRequiredService<IOptions<CatalogOptions>>().Value.SetupFolder,
            serviceProvider.GetRequiredService<IOptions<CatalogOptions>>().Value.PicsFolder));

        services.TryAddScoped(serviceProvider => new CatalogSeeder(
            serviceProvider.GetRequiredService<CatalogDbContext>(),
            serviceProvider.GetRequiredService<CatalogItemHiLoGenerator>(),
            serviceProvider.GetRequiredService<CatalogSeedFolders>(),
            serviceProvider.GetRequiredService<IOptions<CatalogOptions>>().Value.UseCustomizationData,
            serviceProvider.GetRequiredService<ILogger<CatalogSeeder>>()));

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IDataInitializer, CatalogDataInitializer>());

        services.AddHealthChecks()
            .AddCheck<CatalogDatabaseHealthCheck>(
                CatalogDatabaseHealthCheck.Name,
                tags: [HealthCheckExtensions.ReadyTag]);

        return builder;
    }
}
