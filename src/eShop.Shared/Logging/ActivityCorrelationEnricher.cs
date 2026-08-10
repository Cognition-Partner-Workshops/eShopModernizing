using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace eShop.Shared.Logging;

/// <summary>
/// Adds the W3C trace context of the ambient <see cref="Activity"/> to every log event.
/// This replaces the legacy log4net <c>LogicalThreadContext.Properties["activityid"]</c> /
/// <c>["requestinfo"]</c> correlation: ASP.NET Core starts an <see cref="Activity"/> per request
/// and propagates it through async continuations and outgoing HttpClient calls.
/// </summary>
public sealed class ActivityCorrelationEnricher : ILogEventEnricher
{
    /// <summary>Property carrying the per-request correlation id (the W3C trace id).</summary>
    public const string CorrelationIdPropertyName = "CorrelationId";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        var traceId = activity.TraceId.ToString();
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(CorrelationIdPropertyName, traceId));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", traceId));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString()));

        if (activity.ParentSpanId != default)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ParentSpanId", activity.ParentSpanId.ToString()));
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceParent", activity.Id ?? string.Empty));
    }
}
