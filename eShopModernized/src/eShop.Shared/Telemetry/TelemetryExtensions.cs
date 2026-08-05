using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace eShop.Shared.Telemetry;

/// <summary>
/// Registers OpenTelemetry tracing and metrics, replacing the Application Insights 2.9.1
/// <c>System.Web</c> HTTP modules.
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// Adds ASP.NET Core, HttpClient and EF Core instrumentation with an OTLP exporter, plus an Azure
    /// Monitor exporter that only activates when a connection string is configured.
    /// </summary>
    /// <param name="builder">The host builder being configured.</param>
    /// <param name="serviceName">Value of the <c>service.name</c> resource attribute.</param>
    public static TBuilder AddEShopTelemetry<TBuilder>(this TBuilder builder, string serviceName)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var section = builder.Configuration.GetSection(TelemetryOptions.ConfigurationSectionName);
        builder.Services.Configure<TelemetryOptions>(section);

        var options = section.Get<TelemetryOptions>() ?? new TelemetryOptions();
        if (!options.Enabled)
        {
            return builder;
        }

        var otlpEndpoint = ParseEndpoint(options.Otlp.Endpoint);
        var azureMonitorConnectionString = options.AzureMonitor.ConnectionString;
        var exportToAzureMonitor = !string.IsNullOrWhiteSpace(azureMonitorConnectionString);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: serviceName,
                serviceVersion: typeof(TelemetryExtensions).Assembly.GetName().Version?.ToString(),
                serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                if (otlpEndpoint is not null)
                {
                    tracing.AddOtlpExporter(exporter => Configure(exporter, otlpEndpoint, options.Otlp.Protocol));
                }

                if (exportToAzureMonitor)
                {
                    tracing.AddAzureMonitorTraceExporter(exporter =>
                        exporter.ConnectionString = azureMonitorConnectionString);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (otlpEndpoint is not null)
                {
                    metrics.AddOtlpExporter(exporter => Configure(exporter, otlpEndpoint, options.Otlp.Protocol));
                }

                if (exportToAzureMonitor)
                {
                    metrics.AddAzureMonitorMetricExporter(exporter =>
                        exporter.ConnectionString = azureMonitorConnectionString);
                }
            });

        return builder;
    }

    private static void Configure(OtlpExporterOptions exporter, Uri endpoint, string protocol)
    {
        exporter.Endpoint = endpoint;
        exporter.Protocol = protocol.Equals("httpprotobuf", StringComparison.OrdinalIgnoreCase)
            ? OtlpExportProtocol.HttpProtobuf
            : OtlpExportProtocol.Grpc;
    }

    private static Uri? ParseEndpoint(string? endpoint) =>
        Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ? uri : null;
}
