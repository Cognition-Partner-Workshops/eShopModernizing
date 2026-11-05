namespace eShop.Catalog.Domain;

/// <summary>
/// Discount valid for a date range. Only the WCF service exposes this today.
/// </summary>
public class DiscountItem
{
    public int Id { get; set; }

    public double Size { get; set; }

    public DateTime Start { get; set; }

    public DateTime End { get; set; }
}
