namespace eShop.Catalog.Grpc.Client;

/// <summary>
/// Bound from the <c>CatalogService</c> configuration section. Replaces the
/// <c>&lt;system.serviceModel&gt;&lt;client&gt;&lt;endpoint address="..."&gt;</c> entry that the
/// legacy WinForms <c>App.config</c> carried.
/// </summary>
public sealed class CatalogServiceClientOptions
{
    public const string SectionName = "CatalogService";

    /// <summary>Base address of the gRPC service, e.g. <c>http://localhost:5200</c>.</summary>
    public string Address { get; set; } = "http://localhost:5200";

    /// <summary>
    /// The service speaks h2c (plaintext HTTP/2) by default, which .NET only allows when this is
    /// enabled for the channel's handler.
    /// </summary>
    public bool AllowUnencryptedHttp2 { get; set; } = true;
}
