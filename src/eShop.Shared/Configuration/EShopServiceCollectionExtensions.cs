using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace eShop.Shared.Configuration;

/// <summary>
/// Cross-cutting service registrations shared by every modernized host. This is the
/// <c>Microsoft.Extensions.DependencyInjection</c> replacement for the Autofac
/// <c>ApplicationModule</c> plumbing (and, for Web Forms, the property-injection module) that each
/// legacy application configured on its own.
/// </summary>
public static class EShopServiceCollectionExtensions
{
    /// <summary>
    /// Binds and validates the eShop options. Hosts normally reach this through the catalog
    /// composition root (<c>AddEShopCatalogServices</c>), which calls it first.
    /// </summary>
    public static IServiceCollection AddEShopServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<CatalogOptions>()
            .Bind(configuration.GetSection(CatalogOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<CatalogConnectionOptions>()
            .Bind(configuration.GetSection(CatalogConnectionOptions.SectionName))
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<CatalogConnectionOptions>, CatalogConnectionOptionsValidator>());

        // TODO(NET-62): logging/telemetry registrations hook in here.
        // TODO(NET-63): System.Text.Json serialization defaults hook in here.

        return services;
    }

    /// <summary>
    /// Reads <c>Catalog:UseMockData</c> directly from configuration. Needed at registration time,
    /// before the container (and therefore <see cref="IOptions{TOptions}"/>) exists.
    /// </summary>
    public static bool UseMockData(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration.GetSection(CatalogOptions.SectionName).GetValue<bool>(nameof(CatalogOptions.UseMockData));
    }
}
