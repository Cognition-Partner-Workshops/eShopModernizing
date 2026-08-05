using System.Globalization;
using eShop.Catalog.Grpc;
using eShop.Catalog.GrpcClient;

namespace eShop.Catalog.GrpcClient.Tests;

public class CatalogFormattingTests
{
    private static CatalogItem Item(string price = "19.50") => new()
    {
        Id = 3,
        Name = ".NET Blue Hoodie",
        Description = "Blue hoodie",
        Price = new DecimalValue { Value = price },
        PictureFileName = "3.png",
        CatalogBrandId = 2,
        CatalogTypeId = 2,
    };

    [Fact]
    public void FormatItem_ShowsTheColumnsTheWinFormsGridShowed()
    {
        var line = CatalogFormatting.FormatItem(Item());

        Assert.Contains("3", line, StringComparison.Ordinal);
        Assert.Contains(".NET Blue Hoodie", line, StringComparison.Ordinal);
        Assert.Contains("$19.50", line, StringComparison.Ordinal);
        Assert.Contains("brand=2", line, StringComparison.Ordinal);
        Assert.Contains("type=2", line, StringComparison.Ordinal);
        Assert.Contains("3.png", line, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatDiscountedItem_AppliesTheRunningDiscountToThePrice()
        => Assert.Contains("$16.58", CatalogFormatting.FormatDiscountedItem(Item(), 0.15), StringComparison.Ordinal);

    [Fact]
    public void FormatDiscountedItem_WithoutADiscountShowsTheListPrice()
        => Assert.Contains("$19.50", CatalogFormatting.FormatDiscountedItem(Item(), 0), StringComparison.Ordinal);

    [Fact]
    public void ApplyDiscount_StaysInDecimalSoPricesDoNotDrift()
        => Assert.Equal(16.5750m, CatalogFormatting.ApplyDiscount(19.50m, 0.15));

    [Theory]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("en-US")]
    public void Formatting_IsInvariantOnEveryHostCulture(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);

        try
        {
            Assert.Equal("$1234.50", CatalogFormatting.FormatPrice(1234.5m));
            Assert.Equal(
                "15% sale ends on 2026-08-31!",
                CatalogFormatting.FormatDiscountBanner(0.15, new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc)));
            Assert.Equal(
                "2026-08-05  item 3  available 25",
                CatalogFormatting.FormatStockAvailability(new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc), 3, 25));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void FormatPercentage_RoundsToWholePercentAsTheBannerDid()
    {
        Assert.Equal(15d, CatalogFormatting.FormatPercentage(0.15));

        // Math.Round is midpoint-to-even, exactly as in the legacy banner code.
        Assert.Equal(12d, CatalogFormatting.FormatPercentage(0.125));
    }

    [Fact]
    public void FormatShipmentChoice_MatchesTheWinFormsShipmentPickerEntry()
        => Assert.Equal("3 - .NET Blue Hoodie", CatalogFormatting.FormatShipmentChoice(Item()));

    [Fact]
    public void FormatBrandAndType_ShowIdThenName()
    {
        Assert.Equal("   2  .NET", CatalogFormatting.FormatBrand(new CatalogBrand { Id = 2, Brand = ".NET" }));
        Assert.Equal("   1  Mug", CatalogFormatting.FormatType(new CatalogType { Id = 1, Type = "Mug" }));
    }

    [Fact]
    public void ReadPrice_TreatsAnUnsetPriceAsZero()
        => Assert.Equal(0m, CatalogFormatting.ReadPrice(new CatalogItem { Id = 3 }));

    [Fact]
    public void ReadPrice_RejectsAPriceThatIsNotInvariant()
        => Assert.Throws<FormatException>(() => CatalogFormatting.ReadPrice(Item("19,50")));
}
