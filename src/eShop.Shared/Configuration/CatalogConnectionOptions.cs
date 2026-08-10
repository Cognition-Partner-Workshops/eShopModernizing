namespace eShop.Shared.Configuration;

/// <summary>
/// Typed view over the <c>ConnectionStrings</c> section. Replaces the legacy
/// <c>&lt;connectionStrings&gt;</c> entries (<c>CatalogDBContext</c> in MVC/Web Forms,
/// <c>EntityModel</c> in the WCF service) with a single logical name, <c>Catalog</c>.
/// </summary>
/// <remarks>
/// The value never lives in source control: it comes from the environment, either as
/// <c>ConnectionStrings__Catalog</c> (standard) or as the legacy <c>ConnectionString</c> variable
/// the WCF service honoured.
/// </remarks>
public sealed class CatalogConnectionOptions
{
    /// <summary>Configuration section these options bind to.</summary>
    public const string SectionName = "ConnectionStrings";

    /// <summary>Connection string for the catalog database.</summary>
    public string? Catalog { get; set; }
}
