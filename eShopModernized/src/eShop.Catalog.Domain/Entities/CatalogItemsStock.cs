namespace eShop.Catalog.Domain.Entities;

/// <summary>
/// Daily available-stock record. Exists only in the legacy WCF model
/// (eShopWCFService.CatalogItemsStock) and backs GetAvailableStock/CreateAvailableStock.
/// </summary>
public class CatalogItemsStock
{
    public DateTime Date { get; set; }

    public int CatalogItemId { get; set; }

    public int AvailableStock { get; set; }

    public int StockId { get; set; }
}
