using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Catalog.Shared.Logging;

/// <summary>
/// Serilog-based structured logging setup for the modernized catalog.
/// Replaces the legacy <c>log4net</c> <c>RollingFileAppender</c> configuration
/// (<c>log4Net.xml</c>) with structured console logging.
/// </summary>
public static class CatalogLoggingExtensions
{
    /// <summary>
    /// Builds a configured Serilog <see cref="Serilog.ILogger"/> writing structured
    /// output to the console. Reads optional overrides from the <c>Serilog</c>
    /// configuration section when present.
    /// </summary>
    public static Serilog.ILogger CreateSerilogLogger(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}");

        // Honor a "Serilog" section if the host supplies one (min level, overrides, etc.).
        // Kept minimal here: read the default minimum level if configured.
        var configuredLevel = configuration.GetSection("Serilog:MinimumLevel:Default").Value;
        if (!string.IsNullOrWhiteSpace(configuredLevel)
            && Enum.TryParse<LogEventLevel>(configuredLevel, ignoreCase: true, out var level))
        {
            loggerConfiguration.MinimumLevel.Is(level);
        }

        // Optional: wire OpenTelemetry / Application Insights here (kept off by default
        // so the library requires no external endpoints):
        //   loggerConfiguration.WriteTo.OpenTelemetry(...);

        return loggerConfiguration.CreateLogger();
    }

    /// <summary>
    /// Routes <c>Microsoft.Extensions.Logging</c> through Serilog structured logging.
    /// Use from a host builder, e.g. <c>builder.Logging.AddCatalogLogging(builder.Configuration)</c>.
    /// </summary>
    public static ILoggingBuilder AddCatalogLogging(this ILoggingBuilder builder, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        var logger = CreateSerilogLogger(configuration);
        builder.ClearProviders();
        builder.AddSerilog(logger, dispose: true);
        return builder;
    }

    /// <summary>
    /// Registers Serilog structured logging into the service collection for hosts
    /// that configure logging through <see cref="IServiceCollection"/>.
    /// </summary>
    public static IServiceCollection AddCatalogLogging(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddLogging(builder => builder.AddCatalogLogging(configuration));
        return services;
    }
}
