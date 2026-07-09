namespace Catalog.Shared.Configuration;

/// <summary>
/// Strongly-typed application settings for the modernized catalog.
/// These replace the legacy <c>Web.config</c>/<c>App.config</c> <c>&lt;appSettings&gt;</c>
/// and <c>&lt;connectionStrings&gt;</c> entries of the .NET Framework apps.
/// </summary>
/// <remarks>
/// Bound from configuration using the options pattern under the
/// <see cref="SectionName"/> section. Non-secret values live in
/// <c>appsettings.json</c>; the real connection string is a secret and must be
/// supplied at runtime via environment variables or a secret store
/// (e.g. Azure Key Vault) — never committed to source control.
/// </remarks>
public sealed class CatalogSettings
{
    /// <summary>Configuration section these settings bind to.</summary>
    public const string SectionName = "Catalog";

    /// <summary>
    /// When <c>true</c>, the app serves in-memory mock data instead of hitting
    /// the database. Migrated from the legacy <c>UseMockData</c> app setting.
    /// </summary>
    public bool UseMockData { get; set; }

    /// <summary>
    /// When <c>true</c>, seed/customization data is applied on startup.
    /// Migrated from the legacy <c>UseCustomizationData</c> app setting.
    /// </summary>
    public bool UseCustomizationData { get; set; }
}
