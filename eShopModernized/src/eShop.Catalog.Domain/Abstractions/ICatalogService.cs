using eShop.Catalog.Domain.Entities;

namespace eShop.Catalog.Domain.Abstractions;

/// <summary>
/// Catalog read/write surface ported from the legacy eShopLegacyMVC ICatalogService.
/// The synchronous members keep the legacy signature set; the asynchronous members are
/// the preferred entry points for new ASP.NET Core callers.
/// </summary>
public interface ICatalogService
{
    CatalogItem? FindCatalogItem(int id);

    Task<CatalogItem?> FindCatalogItemAsync(int id, CancellationToken cancellationToken = default);

    IEnumerable<CatalogBrand> GetCatalogBrands();

    Task<IEnumerable<CatalogBrand>> GetCatalogBrandsAsync(CancellationToken cancellationToken = default);

    IEnumerable<CatalogType> GetCatalogTypes();

    Task<IEnumerable<CatalogType>> GetCatalogTypesAsync(CancellationToken cancellationToken = default);

    PaginatedItemsViewModel<CatalogItem> GetCatalogItemsPaginated(int pageSize = 10, int pageIndex = 0);

    Task<PaginatedItemsViewModel<CatalogItem>> GetCatalogItemsPaginatedAsync(int pageSize = 10, int pageIndex = 0, CancellationToken cancellationToken = default);

    void CreateCatalogItem(CatalogItem catalogItem);

    Task CreateCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default);

    void UpdateCatalogItem(CatalogItem catalogItem);

    Task UpdateCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default);

    void RemoveCatalogItem(CatalogItem catalogItem);

    Task RemoveCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default);
}
