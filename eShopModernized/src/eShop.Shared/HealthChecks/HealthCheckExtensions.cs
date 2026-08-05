using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace eShop.Shared.HealthChecks;

/// <summary>
/// Liveness and readiness probes for the container story.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>Tag identifying checks served by the liveness endpoint.</summary>
    public const string LiveTag = "live";

    /// <summary>Tag identifying checks served by the readiness endpoint.</summary>
    public const string ReadyTag = "ready";

    /// <summary>Path of the liveness endpoint.</summary>
    public const string LivenessPath = "/health";

    /// <summary>Path of the readiness endpoint.</summary>
    public const string ReadinessPath = "/ready";

    /// <summary>
    /// Registers the process-level <c>self</c> check and lets the caller add readiness checks.
    /// </summary>
    /// <param name="builder">The host builder being configured.</param>
    /// <param name="configureReadinessChecks">
    /// Extension point for dependency checks that must pass before the host accepts traffic — the
    /// catalog database check is registered here once EF Core lands. Checks added by the callback
    /// must be tagged <see cref="ReadyTag"/> to be served by <see cref="ReadinessPath"/>.
    /// </param>
    public static TBuilder AddEShopHealthChecks<TBuilder>(
        this TBuilder builder,
        Action<IHealthChecksBuilder>? configureReadinessChecks = null)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var healthChecks = builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: [LiveTag, ReadyTag]);

        configureReadinessChecks?.Invoke(healthChecks);

        return builder;
    }

    /// <summary>
    /// Maps <c>/health</c> (liveness: process is up) and <c>/ready</c> (readiness: dependencies are up).
    /// </summary>
    public static IEndpointRouteBuilder MapEShopHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapHealthChecks(LivenessPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(LiveTag),
        });

        endpoints.MapHealthChecks(ReadinessPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadyTag),
        });

        return endpoints;
    }
}
