namespace eShop.Catalog.Data;

/// <summary>
/// Interim <see cref="ICatalogItemIdGenerator"/>: the WCF service's "max(Id) + 1" strategy, which
/// works on any provider (including the SQLite databases used by the tests). NET-65 replaces the
/// registration with the HiLo sequence generator.
/// </summary>
public class MaxCatalogItemIdGenerator : ICatalogItemIdGenerator
{
    public int GetNextId(CatalogDbContext db) =>
        db.CatalogItems.Any() ? db.CatalogItems.Max(i => i.Id) + 1 : 1;
}
