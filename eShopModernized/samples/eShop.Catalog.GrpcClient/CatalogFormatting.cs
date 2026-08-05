using System.Globalization;
using eShop.Catalog.Grpc;

namespace eShop.Catalog.GrpcClient;

/// <summary>
/// Console rendering of the catalog messages. These are the text equivalents of the retired
/// WinForms screens: the catalog grid, the discount banner, the shipment picker and the stock
/// availability result list. All output is invariant-culture, where the WinForms client rendered
/// prices and dates with the operator's current culture.
/// </summary>
public static class CatalogFormatting
{
    /// <summary>Printed where the WinForms client simply left the discount banner blank.</summary>
    public const string NoDiscountMessage = "No discount is running.";

    public static string FormatBrand(CatalogBrand brand)
    {
        ArgumentNullException.ThrowIfNull(brand);

        return FormattableString.Invariant($"{brand.Id,4}  {brand.Brand}");
    }

    public static string FormatType(CatalogType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return FormattableString.Invariant($"{type.Id,4}  {type.Type}");
    }

    /// <summary>One catalog row, in the columns the WinForms grid showed plus the foreign keys.</summary>
    public static string FormatItem(CatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return FormattableString.Invariant(
            $"{item.Id,4}  {item.Name,-32}  {FormatPrice(ReadPrice(item)),10}  brand={item.CatalogBrandId,-3} type={item.CatalogTypeId,-3} {item.PictureFileName}");
    }

    /// <summary>
    /// A catalog row with the running discount applied to the price, which is what the WinForms
    /// grid displayed (<c>price * (1 - discount)</c> formatted as currency).
    /// </summary>
    public static string FormatDiscountedItem(CatalogItem item, double discountFraction)
    {
        ArgumentNullException.ThrowIfNull(item);

        return FormattableString.Invariant(
            $"{item.Id,4}  {item.Name,-32}  {FormatPrice(ApplyDiscount(ReadPrice(item), discountFraction)),10}  brand={item.CatalogBrandId,-3} type={item.CatalogTypeId,-3} {item.PictureFileName}");
    }

    /// <summary>The WinForms shipment picker entry: <c>"{id} - {name}"</c>.</summary>
    public static string FormatShipmentChoice(CatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return FormattableString.Invariant($"{item.Id} - {item.Name}");
    }

    /// <summary>
    /// The WinForms banner text, reworded: the legacy string was
    /// <c>"{0}% sale endson {1}!"</c> (missing space) with a short date in the operator's culture.
    /// </summary>
    public static string FormatDiscountBanner(double size, DateTime end)
        => FormattableString.Invariant(
            $"{FormatPercentage(size)}% sale ends on {end.ToString(ValueParsing.DateFormat, CultureInfo.InvariantCulture)}!");

    /// <summary>A row of the WinForms stock-availability result list (date, item id, availability).</summary>
    public static string FormatStockAvailability(DateTime date, int catalogItemId, int availableStock)
        => FormattableString.Invariant(
            $"{date.ToString(ValueParsing.DateFormat, CultureInfo.InvariantCulture)}  item {catalogItemId}  available {availableStock}");

    /// <summary>
    /// Discount percentages were rounded to whole numbers for the banner, with the legacy
    /// <see cref="Math.Round(double, int)" /> midpoint-to-even behaviour (12.5% shows as 12%).
    /// </summary>
    public static double FormatPercentage(double size) => Math.Round(size * 100, 0);

    /// <summary>Legacy grid arithmetic, in decimal so the price never round-trips through double.</summary>
    public static decimal ApplyDiscount(decimal price, double discountFraction)
        => price * (1m - (decimal)discountFraction);

    /// <summary>Currency rendering of the WinForms grid: <c>"$" + value.ToString("F")</c>.</summary>
    public static string FormatPrice(decimal price)
        => "$" + price.ToString("F2", CultureInfo.InvariantCulture);

    /// <summary>Reads the invariant-culture <c>DecimalValue</c> carried on the wire.</summary>
    public static decimal ReadPrice(CatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var value = item.Price?.Value;

        if (string.IsNullOrEmpty(value))
        {
            return 0m;
        }

        // No AllowThousands: the invariant group separator is ',', so "19,50" from a mis-encoded
        // producer would otherwise be read as 1950.
        return decimal.TryParse(
            value,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var price)
            ? price
            : throw new FormatException($"Catalog item {item.Id} carries a price that is not an invariant decimal: '{value}'.");
    }
}
