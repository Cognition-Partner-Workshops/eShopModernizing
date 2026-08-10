using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace eShop.Shared.Logging;

/// <summary>
/// Structured logging for the modernized estate: Microsoft.Extensions.Logging backed by Serilog.
/// Replaces log4net 2.0.10 (GHSA-4f7c-pmjv-c25w / NU1902) and the log4Net.xml appenders.
/// </summary>
/// <remarks>
/// Mapping from the legacy <c>log4Net.xml</c>:
/// <list type="bullet">
/// <item>root level <c>ALL</c> → <see cref="EShopLoggingOptions.MinimumLevel"/> (default
/// <see cref="LogEventLevel.Information"/>; set <c>Verbose</c> to reproduce <c>ALL</c>)</item>
/// <item><c>RollingFileAppender</c> (<c>logFiles\myapp.log</c>, 10 MB, 5 backups) → the Serilog file
/// sink with the same path, size limit and retained-file count</item>
/// <item><c>%property{activity}</c> / <c>%property{requestinfo}</c> → <see cref="ActivityCorrelationEnricher"/></item>
/// </list>
/// The console sink is the container-friendly default and always writes compact JSON.
/// </remarks>
public static class EShopLoggingExtensions
{
    /// <summary>Configuration section bound to <see cref="EShopLoggingOptions"/>.</summary>
    public const string ConfigurationSectionName = "EShopLogging";

    /// <summary>
    /// Replaces the default logging providers with Serilog: compact JSON to the console, an
    /// optional rolling file sink, and per-request correlation ids from <c>Activity</c>.
    /// </summary>
    public static WebApplicationBuilder UseEShopLogging(this WebApplicationBuilder builder, string applicationName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);

        var options = EShopLoggingOptions.FromConfiguration(builder.Configuration.GetSection(ConfigurationSectionName));

        // preserveStaticLogger: the logger is resolved through DI, so the process-wide
        // Serilog.Log.Logger is left alone and several hosts can coexist (integration tests).
        builder.Host.UseSerilog(preserveStaticLogger: true, configureLogger: (context, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Is(options.MinimumLevel)
                .MinimumLevel.Override("Microsoft", options.MicrosoftMinimumLevel)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", options.MinimumLevel)
                .MinimumLevel.Override("System", options.MicrosoftMinimumLevel)
                .Enrich.FromLogContext()
                .Enrich.With<ActivityCorrelationEnricher>()
                .Enrich.WithProperty("Application", applicationName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .WriteTo.Console(new CompactJsonFormatter());

            if (!string.IsNullOrWhiteSpace(options.FilePath))
            {
                loggerConfiguration.WriteTo.File(
                    new CompactJsonFormatter(),
                    options.FilePath,
                    fileSizeLimitBytes: options.FileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: options.RetainedFileCountLimit,
                    flushToDiskInterval: TimeSpan.FromSeconds(1));
            }
        });

        return builder;
    }

    /// <summary>
    /// Emits one structured completion log per HTTP request, carrying the correlation id.
    /// Call as early as possible in the pipeline.
    /// </summary>
    public static IApplicationBuilder UseEShopRequestLogging(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseSerilogRequestLogging();
    }
}
