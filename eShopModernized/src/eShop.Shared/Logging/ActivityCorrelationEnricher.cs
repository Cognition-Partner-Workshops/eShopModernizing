using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace eShop.Shared.Logging;

/// <summary>
/// Enriches every log event with the W3C trace context of the ambient <see cref="Activity"/>.
/// This replaces the legacy log4net <c>LogicalThreadContext</c> <c>activityid</c> property, which
/// was populated from <see cref="Trace.CorrelationManager"/>.
/// </summary>
public sealed class ActivityCorrelationEnricher : ILogEventEnricher
{
    /// <summary>Property carrying the id shared by every log event of a single request.</summary>
    public const string CorrelationIdPropertyName = "CorrelationId";

    /// <summary>Property carrying the W3C <c>traceparent</c> of the ambient activity.</summary>
    public const string TraceParentPropertyName = "TraceParent";

    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        if (activity.IdFormat == ActivityIdFormat.W3C)
        {
            Add(logEvent, propertyFactory, CorrelationIdPropertyName, activity.TraceId.ToString());
            Add(logEvent, propertyFactory, "SpanId", activity.SpanId.ToString());

            if (activity.Id is { Length: > 0 } traceParent)
            {
                Add(logEvent, propertyFactory, TraceParentPropertyName, traceParent);
            }
        }
        else if (activity.RootId is { Length: > 0 } rootId)
        {
            Add(logEvent, propertyFactory, CorrelationIdPropertyName, rootId);
        }
    }

    private static void Add(LogEvent logEvent, ILogEventPropertyFactory factory, string name, string value) =>
        logEvent.AddPropertyIfAbsent(factory.CreateProperty(name, value));
}
