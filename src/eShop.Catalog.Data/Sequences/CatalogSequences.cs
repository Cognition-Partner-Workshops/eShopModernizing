namespace eShop.Catalog.Data.Sequences;

/// <summary>
/// The three SQL sequences the legacy estate created by hand
/// (<c>Models/Infrastructure/dbo.catalog_*.Sequence.sql</c>). They are now created by the EF Core
/// migration with the same start and increment values, and are the only source of catalog
/// identifiers: <c>Catalog.Id</c> stays <c>ValueGeneratedNever</c>, exactly as EF6's
/// <c>DatabaseGeneratedOption.None</c> did.
/// </summary>
public static class CatalogSequences
{
    /// <summary>Sequence the HiLo generator draws catalog item ids from.</summary>
    public const string CatalogItem = "catalog_hilo";

    /// <summary>Sequence the seeder draws catalog brand ids from.</summary>
    public const string CatalogBrand = "catalog_brand_hilo";

    /// <summary>Sequence the seeder draws catalog type ids from.</summary>
    public const string CatalogType = "catalog_type_hilo";

    /// <summary>Schema the legacy sequences live in.</summary>
    public const string Schema = "dbo";

    /// <summary>First value handed out, matching <c>START WITH 1</c>.</summary>
    public const long StartValue = 1;

    /// <summary>
    /// Sequence step, matching <c>INCREMENT BY 10</c>. It is also the HiLo block size: one
    /// <c>NEXT VALUE FOR</c> reserves this many item ids for the application.
    /// </summary>
    public const int Increment = 10;

    /// <summary>All sequence names, in the order the legacy initializer created them.</summary>
    public static IReadOnlyList<string> All { get; } = [CatalogItem, CatalogBrand, CatalogType];

    /// <summary>
    /// Guards against a sequence name reaching SQL as anything other than one of the three known
    /// sequences; sequence names cannot be parameterised in <c>NEXT VALUE FOR</c>.
    /// </summary>
    public static string Validate(string sequenceName) =>
        All.Contains(sequenceName)
            ? sequenceName
            : throw new ArgumentOutOfRangeException(nameof(sequenceName), sequenceName, "Unknown catalog sequence.");
}
