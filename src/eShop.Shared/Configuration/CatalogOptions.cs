namespace eShop.Shared.Configuration;

/// <summary>
/// Typed replacement for the legacy <c>&lt;appSettings&gt;</c> switches that every legacy front end
/// read through <c>ConfigurationManager.AppSettings</c> (MVC, Web Forms and the WCF service).
/// </summary>
public sealed class CatalogOptions
{
    /// <summary>Configuration section these options bind to.</summary>
    public const string SectionName = "Catalog";

    /// <summary>
    /// Legacy <c>UseMockData</c> app setting: serve the in-memory catalog instead of the database.
    /// </summary>
    public bool UseMockData { get; set; }

    /// <summary>
    /// Legacy <c>UseCustomizationData</c> app setting: seed the database from the CSV files under
    /// <c>Setup/</c> instead of the hard-coded preconfigured data.
    /// </summary>
    public bool UseCustomizationData { get; set; }
}
