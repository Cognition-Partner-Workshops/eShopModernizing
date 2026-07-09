using Catalog.Client.Protos;

namespace Catalog.Client;

/// <summary>
/// Client-side abstraction over the catalog gRPC service. Mirrors the ten
/// operations of the legacy WCF <c>ICatalogService</c> contract so callers can be
/// re-pointed from the SOAP/WCF <c>CatalogServiceClient</c> to gRPC with the same
/// call shape. Date parameters are surfaced as <see cref="DateTime"/> (the WCF
/// shape); entity payloads use the protobuf message types.
/// </summary>
public interface ICatalogClient
{
    /// <summary>Returns the item with the given id, or <c>null</c> when none matches.</summary>
    CatalogItem? FindCatalogItem(int id);

    IReadOnlyList<CatalogBrand> GetCatalogBrands();

    /// <summary>
    /// Returns items matching the filters. A filter value of <c>0</c> means
    /// "no filter" (matching the legacy nullable-sentinel semantics).
    /// </summary>
    IReadOnlyList<CatalogItem> GetCatalogItems(int brandIdFilter, int typeIdFilter);

    IReadOnlyList<CatalogType> GetCatalogTypes();

    int GetAvailableStock(DateTime date, int catalogItemId);

    void CreateAvailableStock(CatalogItemsStock stock);

    void CreateCatalogItem(CatalogItem item);

    void UpdateCatalogItem(CatalogItem item);

    void RemoveCatalogItem(CatalogItem item);

    /// <summary>Returns the discount covering the given day, or <c>null</c> when none applies.</summary>
    DiscountItem? GetDiscount(DateTime day);
}
