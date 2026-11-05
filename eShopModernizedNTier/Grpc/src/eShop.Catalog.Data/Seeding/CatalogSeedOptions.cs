namespace eShop.Catalog.Data.Seeding;

/// <summary>
/// Seeding switches, bound from the <c>Catalog</c> configuration section. <see cref="UseCustomizationData"/>
/// is the legacy <c>UseCustomizationData</c> app setting; the two paths replace the legacy
/// <c>HostingEnvironment.ApplicationPhysicalPath</c> lookups, which have no equivalent outside
/// System.Web.
/// </summary>
public sealed class CatalogSeedOptions
{
    /// <summary>Configuration section these options bind to.</summary>
    public const string SectionName = "Catalog";

    /// <summary>Default directory name holding <c>CatalogTypes.csv</c>, <c>CatalogBrands.csv</c>,
    /// <c>CatalogItems.csv</c> and <c>CatalogItems.zip</c>.</summary>
    public const string DefaultSetupDirectoryName = "Setup";

    /// <summary>Default directory the catalog item pictures are extracted into.</summary>
    public const string DefaultPicturesDirectoryName = "Pics";

    /// <summary>
    /// Apply the migrations and seed the catalog while the host starts. The legacy applications
    /// installed their EF6 initializer in <c>Application_Start</c> and it ran lazily on first use;
    /// hosts that must boot without a reachable database (tests, health-probe-only deployments)
    /// can switch this off and call <see cref="ICatalogDatabaseInitializer"/> themselves.
    /// </summary>
    public bool InitializeDatabaseOnStartup { get; set; } = true;

    /// <summary>
    /// Seed from the CSV files under <see cref="SetupDirectory"/> instead of the hard-coded
    /// preconfigured data, and extract the picture archive.
    /// </summary>
    public bool UseCustomizationData { get; set; }

    /// <summary>Absolute or relative path to the setup directory. Defaults to
    /// <c>&lt;base directory&gt;/Setup</c>.</summary>
    public string? SetupDirectory { get; set; }

    /// <summary>Absolute or relative path the pictures are extracted into. Defaults to
    /// <c>&lt;base directory&gt;/Pics</c>.</summary>
    public string? PicturesDirectory { get; set; }

    /// <summary>Resolves <see cref="SetupDirectory"/> against the application base directory.</summary>
    public string ResolveSetupDirectory() => Resolve(SetupDirectory, DefaultSetupDirectoryName);

    /// <summary>Resolves <see cref="PicturesDirectory"/> against the application base directory.</summary>
    public string ResolvePicturesDirectory() => Resolve(PicturesDirectory, DefaultPicturesDirectoryName);

    private static string Resolve(string? configured, string defaultName) =>
        Path.GetFullPath(
            string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(AppContext.BaseDirectory, defaultName)
                : configured);
}
