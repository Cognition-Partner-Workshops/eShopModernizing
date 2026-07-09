using Catalog.Shared.Configuration;
using Catalog.Shared.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Shared.DependencyInjection;

/// <summary>
/// Composition-root wiring for the cross-cutting foundation, replacing the legacy
/// Autofac / <c>Global.asax</c> container setup with the built-in
/// <see cref="IServiceCollection"/> DI container.
/// </summary>
public static class CatalogSharedServiceCollectionExtensions
{
    /// <summary>Configuration key for the catalog database connection string.</summary>
    public const string CatalogDbConnectionName = "CatalogDb";

    /// <summary>
    /// Registers the shared cross-cutting foundation: strongly-typed
    /// <see cref="CatalogSettings"/> options and Serilog structured logging.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="configuration">Application configuration to bind from.</param>
    public static IServiceCollection AddCatalogShared(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddCatalogOptions(configuration);
        services.AddCatalogLogging(configuration);

        return services;
    }

    /// <summary>
    /// Binds <see cref="CatalogSettings"/> from the <c>Catalog</c> configuration
    /// section using the options pattern.
    /// </summary>
    public static IServiceCollection AddCatalogOptions(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<CatalogSettings>()
            .Bind(configuration.GetSection(CatalogSettings.SectionName));

        return services;
    }
}
