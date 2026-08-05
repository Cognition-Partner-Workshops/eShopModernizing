namespace eShop.Catalog.Data.Seeding;

/// <summary>
/// Reads the next value of the <c>dbo.catalog_hilo</c> sequence created by the initial migration.
/// Abstracted so the HiLo allocation can be exercised without a SQL Server instance.
/// </summary>
public interface ICatalogHiLoSequence
{
    /// <summary>Returns the next value of the catalog HiLo sequence.</summary>
    Task<long> GetNextValueAsync(CatalogDbContext context, CancellationToken cancellationToken);
}
