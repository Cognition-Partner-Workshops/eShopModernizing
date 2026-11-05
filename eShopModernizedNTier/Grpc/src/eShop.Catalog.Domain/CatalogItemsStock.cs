namespace eShop.Catalog.Domain;

/// <summary>
/// Per-day stock snapshot for a catalog item. Only the WCF service exposes this today.
/// </summary>
public class CatalogItemsStock
{
    public int StockId { get; set; }

    public DateTime Date { get; set; }

    public int CatalogItemId { get; set; }

    public int AvailableStock { get; set; }
}
