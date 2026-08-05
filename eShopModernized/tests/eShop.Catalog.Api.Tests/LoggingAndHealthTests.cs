using System.Diagnostics;
using System.Net;
using eShop.Shared.Logging;
using Serilog.Events;

namespace eShop.Catalog.Api.Tests;

public class LoggingAndHealthTests : IClassFixture<LoggingTestHost>
{
    private readonly LoggingTestHost _host;

    public LoggingAndHealthTests(LoggingTestHost host) => _host = host;

    [Fact]
    public async Task Health_returns_200_Healthy()
    {
        using var client = _host.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Ready_returns_200_Healthy()
    {
        using var client = _host.CreateClient();

        var response = await client.GetAsync(new Uri("/ready", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Request_produces_a_structured_log_event_carrying_a_correlation_id()
    {
        using var client = _host.CreateClient();

        await client.GetAsync(new Uri("/health", UriKind.Relative));

        var requestLog = _host.Sink.Events.Last(e =>
            e.MessageTemplate.Text.StartsWith("HTTP ", StringComparison.Ordinal) &&
            Rendered(e, "RequestPath") == "/health");

        Assert.Equal("200", Rendered(requestLog, "StatusCode"));
        Assert.Equal("GET", Rendered(requestLog, "RequestMethod"));
        Assert.Equal("eShop.Catalog.Api", Rendered(requestLog, "Application"));
        Assert.Contains("/health", Rendered(requestLog, "RequestInfo"), StringComparison.Ordinal);

        var correlationId = Rendered(requestLog, ActivityCorrelationEnricher.CorrelationIdPropertyName);
        Assert.Matches("^[0-9a-f]{32}$", correlationId);
    }

    [Fact]
    public async Task Correlation_id_follows_the_inbound_traceparent_header()
    {
        const string TraceId = "0af7651916cd43dd8448eb211c80319c";
        using var client = _host.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/ready", UriKind.Relative));
        request.Headers.Add("traceparent", $"00-{TraceId}-b7ad6b7169203331-01");

        await client.SendAsync(request);

        var requestLog = _host.Sink.Events.Last(e => Rendered(e, "RequestPath") == "/ready");

        Assert.Equal(TraceId, Rendered(requestLog, ActivityCorrelationEnricher.CorrelationIdPropertyName));
        Assert.StartsWith(
            $"00-{TraceId}-",
            Rendered(requestLog, ActivityCorrelationEnricher.TraceParentPropertyName),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Enricher_adds_no_properties_when_no_activity_is_running()
    {
        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            var logEvent = new LogEvent(
                DateTimeOffset.UtcNow,
                LogEventLevel.Information,
                exception: null,
                new Serilog.Events.MessageTemplate([]),
                []);

            new ActivityCorrelationEnricher().Enrich(logEvent, new NoOpPropertyFactory());

            Assert.Empty(logEvent.Properties);
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    private static string Rendered(LogEvent logEvent, string propertyName) =>
        logEvent.Properties.TryGetValue(propertyName, out var value) && value is ScalarValue scalar
            ? scalar.Value?.ToString() ?? string.Empty
            : string.Empty;

    private sealed class NoOpPropertyFactory : Serilog.Core.ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false) =>
            new(name, new ScalarValue(value));
    }
}
