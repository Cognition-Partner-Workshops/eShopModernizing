using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace eShop.Shared.Telemetry;

/// <summary>
/// OpenTelemetry tracing and metrics, replacing the Application Insights 2.9.1 System.Web HTTP
/// modules of the legacy applications.
/// </summary>
public static class EShopTelemetryExtensions
{
    /// <summary>Configuration section holding the optional Azure Monitor connection string.</summary>
    public const string ConfigurationSectionName = "Telemetry";

    /// <summary>Environment variable honoured by Azure App Service / Container Apps.</summary>
    public const string AzureMonitorConnectionStringEnvironmentVariable = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    /// <summary>
    /// Registers ASP.NET Core and HttpClient instrumentation. The Azure Monitor exporter is added
    /// only when a connection string is configured, so startup never fails without one.
    /// </summary>
    public static WebApplicationBuilder AddEShopTelemetry(this WebApplicationBuilder builder, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var connectionString = ResolveAzureMonitorConnectionString(builder.Configuration);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: serviceName,
                serviceVersion: typeof(EShopTelemetryExtensions).Assembly.GetName().Version?.ToString()))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (connectionString is not null)
                {
                    tracing.AddAzureMonitorTraceExporter(exporter => exporter.ConnectionString = connectionString);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (connectionString is not null)
                {
                    metrics.AddAzureMonitorMetricExporter(exporter => exporter.ConnectionString = connectionString);
                }
            });

        return builder;
    }

    /// <summary>
    /// Reads the Azure Monitor connection string from <c>Telemetry:AzureMonitorConnectionString</c>
    /// or <c>APPLICATIONINSIGHTS_CONNECTION_STRING</c>; returns <c>null</c> when telemetry export is
    /// not configured.
    /// </summary>
    public static string? ResolveAzureMonitorConnectionString(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var value = configuration.GetSection(ConfigurationSectionName)["AzureMonitorConnectionString"];
        if (string.IsNullOrWhiteSpace(value))
        {
            value = configuration[AzureMonitorConnectionStringEnvironmentVariable];
        }

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
