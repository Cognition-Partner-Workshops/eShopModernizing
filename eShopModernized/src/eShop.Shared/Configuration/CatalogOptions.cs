using System.ComponentModel.DataAnnotations;

namespace eShop.Shared.Configuration;

/// <summary>
/// Catalog behaviour settings, bound from the <c>Catalog</c> configuration section.
/// Replaces the legacy <c>appSettings</c> keys read through static configuration access.
/// </summary>
public sealed class CatalogOptions
{
    /// <summary>Configuration section these options are bound from.</summary>
    public const string SectionName = "Catalog";

    /// <summary>
    /// Serve the catalog from the in-memory mock implementation instead of the database.
    /// Legacy key: <c>UseMockData</c>.
    /// </summary>
    public bool UseMockData { get; set; }

    /// <summary>
    /// Seed the database from the CSV files under <see cref="SetupFolder"/> rather than from the
    /// hard-coded preconfigured data. Legacy key: <c>UseCustomizationData</c>.
    /// </summary>
    public bool UseCustomizationData { get; set; }

    /// <summary>
    /// Content-root-relative folder holding the catalog item pictures.
    /// Replaces the legacy <c>Server.MapPath("~/Pics")</c> lookup.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string PicsFolder { get; set; } = "Pics";

    /// <summary>
    /// Content-root-relative folder holding the customization CSV files consumed when
    /// <see cref="UseCustomizationData"/> is enabled. Replaces the legacy <c>~/Setup</c> lookup.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string SetupFolder { get; set; } = "Setup";
}
