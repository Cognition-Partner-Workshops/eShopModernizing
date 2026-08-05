using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace eShop.Shared.Logging;

/// <summary>
/// Guarantees that every request runs inside a W3C <see cref="Activity"/> and emits one structured
/// completion log event per request.
/// </summary>
/// <remarks>
/// Replaces the legacy <c>Application_BeginRequest</c> hook, which pushed <c>activityid</c> and
/// <c>requestinfo</c> into the log4net <c>LogicalThreadContext</c>. The correlation id now flows from
/// the inbound <c>traceparent</c> header when the caller supplies one, so it is shared across services.
/// </remarks>
public sealed class RequestCorrelationMiddleware
{
    private const string ActivityName = "eShop.Request";
    private const string TraceParentHeaderName = "traceparent";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestCorrelationMiddleware> _logger;

    /// <summary>Initializes the middleware.</summary>
    public RequestCorrelationMiddleware(RequestDelegate next, ILogger<RequestCorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Runs the middleware for the current request.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // ASP.NET Core only starts an activity when something is listening (for example the
        // OpenTelemetry instrumentation); start one manually otherwise so logs are always correlated.
        var ownedActivity = StartActivityIfNone(context);
        var timestamp = Stopwatch.GetTimestamp();

        // Mirrors the legacy "requestinfo" property (raw url + user agent).
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestInfo"] = $"{context.Request.GetEncodedPathAndQuery()}, {context.Request.Headers.UserAgent}",
        });

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            _logger.LogInformation(
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMilliseconds:0.0000} ms",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);

            ownedActivity?.Dispose();
        }
    }

    private static Activity? StartActivityIfNone(HttpContext context)
    {
        if (Activity.Current is not null)
        {
            return null;
        }

        var activity = new Activity(ActivityName);
        activity.SetIdFormat(ActivityIdFormat.W3C);

        if (context.Request.Headers.TryGetValue(TraceParentHeaderName, out var traceParent) &&
            traceParent.Count > 0 &&
            !string.IsNullOrEmpty(traceParent[0]))
        {
            activity.SetParentId(traceParent[0]!);
        }

        return activity.Start();
    }
}
