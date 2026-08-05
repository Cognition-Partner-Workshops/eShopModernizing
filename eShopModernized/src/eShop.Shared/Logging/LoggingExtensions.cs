using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;

namespace eShop.Shared.Logging;

/// <summary>
/// Registers Serilog behind <c>Microsoft.Extensions.Logging</c>, replacing legacy log4net.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>Configuration section read by <see cref="Serilog"/>.</summary>
    public const string ConfigurationSectionName = "Serilog";

    /// <summary>
    /// Adds Serilog as the only logging provider, reading sinks, levels and enrichers from the
    /// <c>Serilog</c> configuration section and falling back to a structured JSON console sink.
    /// Sinks and enrichers registered in DI are picked up as well, which is how tests capture output.
    /// </summary>
    /// <param name="builder">The host builder being configured.</param>
    /// <param name="applicationName">Value of the <c>Application</c> log property.</param>
    public static TBuilder AddEShopLogging<TBuilder>(this TBuilder builder, string applicationName)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);

        builder.Logging.ClearProviders();

        var hasConfiguredSinks = builder.Configuration
            .GetSection($"{ConfigurationSectionName}:WriteTo")
            .GetChildren()
            .Any();

        builder.Services.AddSerilog((services, logger) =>
        {
            logger
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.With(new ActivityCorrelationEnricher())
                .Enrich.WithProperty("Application", applicationName);

            if (!hasConfiguredSinks)
            {
                logger.WriteTo.Console(new CompactJsonFormatter());
            }
        });

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IStartupFilter, RequestCorrelationStartupFilter>());

        return builder;
    }
}
