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

    /// <summary>
    /// Items matching the legacy WCF filters, where a filter value of 0 means "no filter".
    /// The navigation properties are not populated, as in the legacy list operation.
    /// </summary>
    Task<IEnumerable<CatalogItem>> GetCatalogItemsAsync(int brandIdFilter, int typeIdFilter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stock recorded for <paramref name="catalogItemId" /> on <paramref name="date" />, comparing
    /// the date component only. Returns 0 when there is no row, as the legacy service did.
    /// </summary>
    Task<int> GetAvailableStockAsync(DateTime date, int catalogItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Overwrites the stock recorded for that item and date if there is one, otherwise records a
    /// new entry with <c>MAX(StockId) + 1</c>.
    /// </summary>
    Task CreateAvailableStockAsync(CatalogItemsStock catalogItemsStock, CancellationToken cancellationToken = default);

    /// <summary>
    /// The first discount whose inclusive date range covers <paramref name="day" />, or
    /// <see langword="null" /> when no discount is running.
    /// </summary>
    Task<DiscountItem?> GetDiscountAsync(DateTime day, CancellationToken cancellationToken = default);
}
