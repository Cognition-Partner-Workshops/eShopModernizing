using eShop.Catalog.Domain.Entities;

namespace eShop.Catalog.Domain.Tests;

public class StockAndDiscountTests
{
    [Fact]
    public void CatalogItemsStock_Defaults()
    {
        var stock = new CatalogItemsStock();

        Assert.Equal(default, stock.Date);
        Assert.Equal(0, stock.CatalogItemId);
        Assert.Equal(0, stock.AvailableStock);
        Assert.Equal(0, stock.StockId);
    }

    [Fact]
    public void CatalogItemsStock_RoundTripsValues()
    {
        var date = new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc);
        var stock = new CatalogItemsStock
        {
            Date = date,
            CatalogItemId = 3,
            AvailableStock = 42,
            StockId = 9,
        };

        Assert.Equal(date, stock.Date);
        Assert.Equal(3, stock.CatalogItemId);
        Assert.Equal(42, stock.AvailableStock);
        Assert.Equal(9, stock.StockId);
    }

    [Fact]
    public void DiscountItem_RoundTripsValues()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        var discount = new DiscountItem
        {
            Id = 1,
            Size = 12.5,
            Start = start,
            End = end,
        };

        Assert.Equal(1, discount.Id);
        Assert.Equal(12.5, discount.Size);
        Assert.Equal(start, discount.Start);
        Assert.Equal(end, discount.End);
    }
}
