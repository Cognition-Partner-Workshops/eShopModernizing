using eShop.Catalog.Domain;
using Xunit;

namespace eShop.Catalog.Data.Tests;

public class WcfSeedParityTests
{
    [Fact]
    public void CatalogMatchesEveryLegacyField()
    {
        var service = new MockCatalogService();
        var expected = LegacyWcfSeed.GetPreconfiguredCatalogItems();
        var actual = service.GetCatalogItems(0, 0);
        Assert.Equal(12, actual.Count);
        Assert.Equal(expected.Select(Item), actual.Select(Item));
        Assert.Equal(LegacyWcfSeed.GetPreconfiguredCatalogBrands().Select(x => (x.Id, x.Brand)),
            service.GetCatalogBrands().Select(x => (x.Id, x.Brand)));
        Assert.Equal(LegacyWcfSeed.GetPreconfiguredCatalogTypes().Select(x => (x.Id, x.Type)),
            service.GetCatalogTypes().Select(x => (x.Id, x.Type)));
        Assert.Empty(service.GetCatalogItems(999, 0));
    }

    [Fact]
    public async Task StockAndDiscountsMatchLegacyIncludingBoundariesAndAbsence()
    {
        var service = new MockCatalogStockService();
        var stocks = LegacyWcfSeed.GetPreconfiguredCatalogItemsStock().ToList();
        Assert.Equal(6, stocks.Count);
        foreach (var stock in stocks)
        {
            Assert.Equal(stock.AvailableStock, await service.GetAvailableStockAsync(stock.Date.AddHours(12), stock.CatalogItemId));
        }

        Assert.Equal(0, await service.GetAvailableStockAsync(new DateTime(2026, 1, 1), 1));
        var discounts = LegacyWcfSeed.GetPreconfiguredDiscountItems().ToList();
        Assert.Equal(6, discounts.Count);
        for (var index = 0; index < discounts.Count; index++)
        {
            var expected = discounts[index];
            foreach (var day in new[] { expected.Start, expected.End.AddHours(23) })
            {
                var actual = await service.GetDiscountAsync(day);
                Assert.NotNull(actual);
                Assert.Equal((index + 1, expected.Start, expected.End, expected.Size),
                    (actual.Id, actual.Start, actual.End, actual.Size));
            }
        }

        Assert.Null(await service.GetDiscountAsync(new DateTime(2026, 1, 1)));
    }

    [Fact]
    public async Task StockCreateAssignsMaxPlusOneAndOverwriteKeepsIdentity()
    {
        var service = new MockCatalogStockService();
        var stock = new CatalogItemsStock { StockId = 99, CatalogItemId = 12, Date = new DateTime(2026, 1, 1), AvailableStock = 7 };
        await service.CreateAvailableStockAsync(stock);
        Assert.Equal(7, stock.StockId);
        await service.CreateAvailableStockAsync(new CatalogItemsStock { CatalogItemId = 12, Date = stock.Date.AddHours(12), AvailableStock = 9 });
        Assert.Equal(9, await service.GetAvailableStockAsync(stock.Date, 12));
        var next = new CatalogItemsStock { CatalogItemId = 11, Date = stock.Date };
        await service.CreateAvailableStockAsync(next);
        Assert.Equal(8, next.StockId);
    }

    private static object Item(CatalogItem x) =>
        (x.Id, x.Name, x.Description, x.CatalogBrandId, x.CatalogTypeId, x.Price, x.PictureFileName);
}
