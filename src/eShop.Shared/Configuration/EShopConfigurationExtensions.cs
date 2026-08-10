using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace eShop.Shared.Configuration;

/// <summary>
/// Builds the configuration pipeline every modernized host shares:
/// <c>appsettings.json</c> → <c>appsettings.{Environment}.json</c> → environment variables →
/// command line (all supplied by the generic host) plus the legacy environment-variable overrides
/// carried over from the .NET Framework applications.
/// </summary>
public static class EShopConfigurationExtensions
{
    /// <summary>
    /// Legacy environment variable the WCF service read directly
    /// (<c>Environment.GetEnvironmentVariable("ConnectionString")</c>) to override the connection
    /// string configured in <c>Web.config</c>.
    /// </summary>
    public const string LegacyConnectionStringVariable = "ConnectionString";

    /// <summary>Configuration key the legacy override is mapped onto.</summary>
    public const string CatalogConnectionStringKey = "ConnectionStrings:Catalog";

    /// <summary>
    /// Applies the eShop configuration conventions to a host builder. Call this before
    /// <see cref="EShopServiceCollectionExtensions.AddEShopServices"/> so the options bind against
    /// the final configuration.
    /// </summary>
    public static TBuilder AddEShopConfiguration<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Configuration.AddEShopLegacyEnvironmentOverrides();

        return builder;
    }

    /// <summary>
    /// Maps the legacy <c>ConnectionString</c> environment variable onto
    /// <c>ConnectionStrings:Catalog</c>. The standard <c>ConnectionStrings__Catalog</c> variable
    /// (and any explicit configuration) wins: the legacy name is only a fallback.
    /// </summary>
    public static IConfigurationManager AddEShopLegacyEnvironmentOverrides(this IConfigurationManager configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (!string.IsNullOrWhiteSpace(configuration[CatalogConnectionStringKey]))
        {
            return configuration;
        }

        var legacyValue = Environment.GetEnvironmentVariable(LegacyConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(legacyValue))
        {
            return configuration;
        }

        configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { [CatalogConnectionStringKey] = legacyValue });

        return configuration;
    }
}
