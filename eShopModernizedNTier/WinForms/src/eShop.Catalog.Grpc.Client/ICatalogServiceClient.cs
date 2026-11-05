using eShop.Catalog.Domain;

namespace eShop.Catalog.Grpc.Client;

/// <summary>
/// Client-side view of the catalog service, one member per operation of the legacy WCF
/// <c>ICatalogService</c> contract. The legacy generated SOAP proxy returned <c>null</c> for a
/// missing item or discount; the gRPC service reports <c>NOT_FOUND</c> instead, and this wrapper
/// translates it back to <c>null</c> so callers keep the legacy semantics.
/// </summary>
public interface ICatalogServiceClient
{
    Task<CatalogItem?> FindCatalogItemAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogBrand>> GetCatalogBrandsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogType>> GetCatalogTypesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogItem>> GetCatalogItemsAsync(
        int brandIdFilter,
        int typeIdFilter,
        CancellationToken cancellationToken = default);

    Task<int> GetAvailableStockAsync(DateTime date, int catalogItemId, CancellationToken cancellationToken = default);

    Task CreateAvailableStockAsync(CatalogItemsStock stock, CancellationToken cancellationToken = default);

    Task CreateCatalogItemAsync(CatalogItem item, CancellationToken cancellationToken = default);

    Task UpdateCatalogItemAsync(CatalogItem item, CancellationToken cancellationToken = default);

    Task RemoveCatalogItemAsync(CatalogItem item, CancellationToken cancellationToken = default);

    Task<DiscountItem?> GetDiscountAsync(DateTime day, CancellationToken cancellationToken = default);
}
