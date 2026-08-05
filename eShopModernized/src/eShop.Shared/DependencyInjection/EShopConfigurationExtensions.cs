using eShop.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace eShop.Shared.DependencyInjection;

/// <summary>
/// Registers the eShop configuration surface: typed options, their start-up validation and the
/// data-initialization hook. Replaces the Autofac <c>ApplicationModule</c> and the
/// <c>Global.asax</c> <c>Application_Start</c> configuration work.
/// </summary>
public static class EShopConfigurationExtensions
{
    /// <summary>Binds and validates the eShop options for a host builder.</summary>
    public static IHostApplicationBuilder AddEShopConfiguration(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddEShopConfiguration(builder.Configuration);

        return builder;
    }

    /// <summary>Binds and validates the eShop options against the supplied configuration.</summary>
    public static IServiceCollection AddEShopConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<CatalogOptions>()
            .Bind(configuration.GetSection(CatalogOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CatalogConnectionOptions>()
            .Bind(configuration.GetSection(CatalogConnectionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<CatalogConnectionOptions>, CatalogConnectionOptionsValidator>());

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, DataInitializationHostedService>());

        return services;
    }
}
