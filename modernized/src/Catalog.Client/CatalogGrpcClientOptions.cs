namespace Catalog.Client;

/// <summary>
/// Configuration for <see cref="CatalogGrpcClient"/>. The address points at the
/// modernized Catalog gRPC service (the WCF endpoint replacement).
/// </summary>
public sealed class CatalogGrpcClientOptions
{
    /// <summary>Configuration section name callers can bind these options from.</summary>
    public const string SectionName = "CatalogService";

    /// <summary>
    /// Absolute address of the Catalog gRPC service, e.g. <c>https://localhost:5001</c>.
    /// </summary>
    public string Address { get; set; } = "https://localhost:5001";
}
