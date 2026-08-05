namespace eShop.Shared.Configuration;

/// <summary>
/// Connection strings for the consolidated catalog database, bound from the
/// <c>ConnectionStrings</c> section. Replaces the legacy <c>CatalogDBContext</c> (MVC / Web Forms)
/// and <c>EntityModel</c> (WCF) connection strings.
/// </summary>
public sealed class CatalogConnectionOptions
{
    /// <summary>Configuration section these options are bound from.</summary>
    public const string SectionName = "ConnectionStrings";

    /// <summary>
    /// Connection string for the catalog database. Overridden in deployment with the
    /// <c>ConnectionStrings__Catalog</c> environment variable.
    /// </summary>
    public string? Catalog { get; set; }
}
