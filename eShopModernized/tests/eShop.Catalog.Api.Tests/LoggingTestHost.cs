using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog.Core;
using Serilog.Events;

namespace eShop.Catalog.Api.Tests;

/// <summary>Serilog sink that keeps every log event in memory for assertions.</summary>
public sealed class InMemoryLogSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _events = new();

    /// <summary>Log events captured so far.</summary>
    public IReadOnlyCollection<LogEvent> Events => _events.ToArray();

    /// <inheritdoc />
    public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);
}

/// <summary>
/// Hosts the API in memory with the Serilog pipeline writing to <see cref="Sink"/> as well, and with
/// the rolling file sink redirected out of the source tree.
/// </summary>
public sealed class LoggingTestHost : WebApplicationFactory<Program>
{
    private readonly string _logDirectory =
        Path.Combine(Path.GetTempPath(), "eshop-logging-tests", Guid.NewGuid().ToString("N"));

    /// <summary>Captured log events.</summary>
    public InMemoryLogSink Sink { get; } = new();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSetting(
            "Serilog:WriteTo:1:Args:path",
            Path.Combine(_logDirectory, "myapp.log"));

        // Picked up by Serilog's ReadFrom.Services(...).
        builder.ConfigureServices(services => services.AddSingleton<ILogEventSink>(Sink));
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(_logDirectory))
        {
            Directory.Delete(_logDirectory, recursive: true);
        }
    }
}
