using eShop.Catalog.Domain;

namespace eShop.Catalog.Data;

/// <summary>
/// Database-backed <see cref="ICatalogService"/> placeholder registered when
/// <c>Catalog:UseMockData</c> is false. It keeps the container resolvable — and therefore the host
/// bootable — while the persistence layer is still being migrated; every operation fails fast.
/// </summary>
// TODO(NET-64): replace with the EF Core 8 implementation backed by CatalogDbContext.
public sealed class PendingDatabaseCatalogService : ICatalogService
{
    private const string Message =
        "The database-backed catalog service is not implemented yet (NET-64). " +
        "Set 'Catalog:UseMockData' to true to run against the in-memory catalog.";

    public CatalogItem? FindCatalogItem(int id) => throw new NotSupportedException(Message);

    public IEnumerable<CatalogBrand> GetCatalogBrands() => throw new NotSupportedException(Message);

    public IEnumerable<CatalogType> GetCatalogTypes() => throw new NotSupportedException(Message);

    public IReadOnlyList<CatalogItem> GetCatalogItems(int brandIdFilter, int typeIdFilter)
        => throw new NotSupportedException(Message);

    public PaginatedItems<CatalogItem> GetCatalogItemsPaginated(int pageSize, int pageIndex)
        => throw new NotSupportedException(Message);

    public void CreateCatalogItem(CatalogItem catalogItem) => throw new NotSupportedException(Message);

    public void UpdateCatalogItem(CatalogItem catalogItem) => throw new NotSupportedException(Message);

    public void RemoveCatalogItem(CatalogItem catalogItem) => throw new NotSupportedException(Message);
}
