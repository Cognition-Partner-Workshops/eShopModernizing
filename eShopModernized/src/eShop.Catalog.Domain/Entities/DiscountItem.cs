namespace eShop.Catalog.Domain.Entities;

/// <summary>
/// Discount applicable to a date range. Exists only in the legacy WCF model
/// (eShopWCFService.Models.DiscountItem) and backs GetDiscount.
/// </summary>
public class DiscountItem
{
    public double Size { get; set; }

    public DateTime Start { get; set; }

    public DateTime End { get; set; }

    public int Id { get; set; }
}
