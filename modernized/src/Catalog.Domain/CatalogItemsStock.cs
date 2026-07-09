namespace Catalog.Domain;

/// <summary>
/// Date-stamped stock level for a <see cref="CatalogItem"/>. Ported from the
/// legacy WCF N-Tier <c>CatalogItemsStock</c> DataContract, which tracks
/// available stock per item per calendar day (separate from the aggregate
/// <see cref="CatalogItem.AvailableStock"/> used by the web apps).
/// </summary>
public class CatalogItemsStock
{
    public int StockId { get; set; }

    public int CatalogItemId { get; set; }

    public DateTime Date { get; set; }

    public int AvailableStock { get; set; }
}
