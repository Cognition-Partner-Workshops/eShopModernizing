using eShopWinForms.Grpc.Client.Models;

namespace eShopWinForms.Grpc.Client;

public interface ICatalogServiceClient
{
    CatalogItem? FindCatalogItem(int id);
    IReadOnlyList<CatalogBrand> GetCatalogBrands();
    IReadOnlyList<CatalogType> GetCatalogTypes();
    IReadOnlyList<CatalogItem> GetCatalogItems(int brandIdFilter, int typeIdFilter);
    int GetAvailableStock(DateTime date, int catalogItemId);
    void CreateAvailableStock(CatalogItemsStock stock);
    void CreateCatalogItem(CatalogItem item);
    void UpdateCatalogItem(CatalogItem item);
    void RemoveCatalogItem(CatalogItem item);
    DiscountItem? GetDiscount(DateTime day);
}
