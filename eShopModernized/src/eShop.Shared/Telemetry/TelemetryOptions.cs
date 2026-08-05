namespace eShop.Shared.Telemetry;

/// <summary>
/// Options bound from the <c>OpenTelemetry</c> configuration section. Exporters stay disabled until
/// they are configured, so a container without a collector produces no export errors.
/// </summary>
public sealed class TelemetryOptions
{
    /// <summary>Name of the configuration section holding these options.</summary>
    public const string ConfigurationSectionName = "OpenTelemetry";

    /// <summary>Enables tracing and metrics collection. Defaults to <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>OTLP exporter settings.</summary>
    public OtlpOptions Otlp { get; set; } = new();

    /// <summary>Azure Monitor exporter settings (the successor of Application Insights 2.9.1).</summary>
    public AzureMonitorOptions AzureMonitor { get; set; } = new();
}

/// <summary>OTLP exporter settings.</summary>
public sealed class OtlpOptions
{
    /// <summary>Collector endpoint, for example <c>http://otel-collector:4317</c>. Empty disables the exporter.</summary>
    public string? Endpoint { get; set; }

    /// <summary>Either <c>grpc</c> (default) or <c>httpprotobuf</c>.</summary>
    public string Protocol { get; set; } = "grpc";
}

/// <summary>Azure Monitor exporter settings.</summary>
public sealed class AzureMonitorOptions
{
    /// <summary>Application Insights connection string. Empty disables the exporter.</summary>
    public string? ConnectionString { get; set; }
}
