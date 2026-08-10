using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace eShop.Shared.Diagnostics;

/// <summary>
/// Liveness (<c>/health</c>) and readiness (<c>/ready</c>) endpoints for the container story.
/// </summary>
public static class EShopHealthCheckExtensions
{
    /// <summary>Tag marking a health check as part of the readiness probe.</summary>
    public const string ReadinessTag = "ready";

    /// <summary>Adds health-check services with a always-on liveness check.</summary>
    public static IHealthChecksBuilder AddEShopHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live", ReadinessTag });
    }

    /// <summary>
    /// Maps <c>/health</c> (liveness: process is up) and <c>/ready</c> (readiness: every check
    /// tagged <see cref="ReadinessTag"/> passes). Both return a small JSON document.
    /// </summary>
    public static IEndpointRouteBuilder MapEShopHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = WriteResponseAsync,
        });

        endpoints.MapHealthChecks("/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadinessTag),
            ResponseWriter = WriteResponseAsync,
        });

        return endpoints;
    }

    private static Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new { status = entry.Value.Status.ToString(), description = entry.Value.Description }),
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
