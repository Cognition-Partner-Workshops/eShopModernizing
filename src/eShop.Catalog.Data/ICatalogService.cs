using eShop.Catalog.Domain;

namespace eShop.Catalog.Data;

/// <summary>
/// Catalog read/write abstraction shared by every front end. The union of the legacy MVC and
/// WCF contracts; the EF Core 8 implementation arrives in NET-64.
/// </summary>
public interface ICatalogService
{
    CatalogItem? FindCatalogItem(int id);

    IEnumerable<CatalogBrand> GetCatalogBrands();

    IEnumerable<CatalogType> GetCatalogTypes();

    IReadOnlyList<CatalogItem> GetCatalogItems(int brandIdFilter, int typeIdFilter);

    PaginatedItems<CatalogItem> GetCatalogItemsPaginated(int pageSize, int pageIndex);

    void CreateCatalogItem(CatalogItem catalogItem);

    void UpdateCatalogItem(CatalogItem catalogItem);

    void RemoveCatalogItem(CatalogItem catalogItem);
}
