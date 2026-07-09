namespace Catalog.Domain;

/// <summary>
/// A time-bounded discount. Ported from the legacy WCF N-Tier
/// <c>DiscountItem</c> DataContract; a discount applies to any day within the
/// inclusive <see cref="Start"/>..<see cref="End"/> range.
/// </summary>
public class DiscountItem
{
    public int Id { get; set; }

    public double Size { get; set; }

    public DateTime Start { get; set; }

    public DateTime End { get; set; }
}
