namespace eShop.Catalog.Data.Sequences;

/// <summary>
/// Allocates the next value of a catalog sequence. The legacy code issued
/// <c>SELECT NEXT VALUE FOR catalog_hilo</c> directly against SQL Server; this abstraction keeps
/// that behaviour for SQL Server while letting the unattended Linux test suite run the same
/// allocation logic on SQLite, which has no sequence objects.
/// </summary>
public interface ICatalogSequenceProvider
{
    /// <summary>
    /// Returns the next value of <paramref name="sequenceName"/>, advancing it by
    /// <see cref="CatalogSequences.Increment"/>.
    /// </summary>
    long GetNextSequenceValue(CatalogDbContext db, string sequenceName);
}
