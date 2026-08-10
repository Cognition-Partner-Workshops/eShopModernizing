using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace eShop.Catalog.Api.Tests;

/// <summary>
/// Verifies the log4net replacement: every request produces structured (JSON) log entries that
/// carry the W3C trace id of the request as their correlation id.
/// </summary>
public class RequestCorrelationLoggingTests
{
    [Fact]
    public async Task Request_WritesStructuredLogEntriesCarryingTheCorrelationId()
    {
        var logDirectory = Directory.CreateTempSubdirectory("eshop-logs-");
        var logFile = Path.Combine(logDirectory.FullName, "requests.log");
        const string TraceId = "0af7651916cd43dd8448eb211c80319c";

        try
        {
            using (var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(webHost =>
                {
                    webHost.UseSetting("EShopLogging:FilePath", logFile);
                    webHost.UseSetting("EShopLogging:MinimumLevel", "Information");
                }))
            {
                var client = factory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
                request.Headers.Add("traceparent", $"00-{TraceId}-b7ad6b7169203331-01");

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }

            var entries = ReadLogEvents(logFile);
            Assert.NotEmpty(entries);

            var requestEntry = entries.First(entry =>
                entry.TryGetProperty("CorrelationId", out var correlationId)
                && correlationId.GetString() == TraceId);

            Assert.Equal(TraceId, requestEntry.GetProperty("TraceId").GetString());
            Assert.Equal("eShop.Catalog.Api", requestEntry.GetProperty("Application").GetString());
            Assert.StartsWith($"00-{TraceId}-", requestEntry.GetProperty("TraceParent").GetString());

            // The per-request completion entry is correlated too.
            Assert.Contains(entries, entry =>
                entry.TryGetProperty("CorrelationId", out var correlationId)
                && correlationId.GetString() == TraceId
                && entry.TryGetProperty("StatusCode", out var statusCode)
                && statusCode.GetInt32() == 200);
        }
        finally
        {
            logDirectory.Delete(recursive: true);
        }
    }

    private static IReadOnlyList<JsonElement> ReadLogEvents(string logFile)
    {
        var file = Directory.EnumerateFiles(Path.GetDirectoryName(logFile)!, "requests*.log").Single();

        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        var events = new List<JsonElement>();
        while (reader.ReadLine() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                events.Add(JsonDocument.Parse(line).RootElement.Clone());
            }
        }

        return events;
    }
}
