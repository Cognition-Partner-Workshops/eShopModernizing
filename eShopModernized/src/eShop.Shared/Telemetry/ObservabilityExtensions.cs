using eShop.Shared.HealthChecks;
using eShop.Shared.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace eShop.Shared.Telemetry;

/// <summary>
/// Single entry point wiring logging, tracing, metrics and health checks into a host.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Adds Serilog logging with per-request correlation, OpenTelemetry tracing and metrics, and the
    /// liveness/readiness checks. Call <see cref="HealthCheckExtensions.MapEShopHealthChecks"/> to
    /// expose the probes.
    /// </summary>
    /// <param name="builder">The host builder being configured.</param>
    /// <param name="serviceName">Logical service name used for logs and the OpenTelemetry resource.</param>
    /// <param name="configureReadinessChecks">Optional readiness checks; see <see cref="HealthCheckExtensions.AddEShopHealthChecks{TBuilder}"/>.</param>
    public static TBuilder AddEShopObservability<TBuilder>(
        this TBuilder builder,
        string serviceName,
        Action<IHealthChecksBuilder>? configureReadinessChecks = null)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddEShopLogging(serviceName)
            .AddEShopTelemetry(serviceName)
            .AddEShopHealthChecks(configureReadinessChecks);
    }
}
