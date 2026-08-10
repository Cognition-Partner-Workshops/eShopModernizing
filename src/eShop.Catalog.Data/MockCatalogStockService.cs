using eShop.Catalog.Domain;

namespace eShop.Catalog.Data;

/// <summary>
/// In-memory <see cref="ICatalogStockService"/> used when <c>Catalog:UseMockData</c> is true, so
/// the gRPC service is demoable on Linux without SQL Server. The data set is the legacy WCF
/// <c>PreconfiguredData.GetPreconfiguredCatalogItemsStock()</c> /
/// <c>GetPreconfiguredDiscountItems()</c> content, kept verbatim.
/// </summary>
public class MockCatalogStockService : ICatalogStockService
{
    private readonly List<CatalogItemsStock> _stocks =
    [
        new() { StockId = 1, CatalogItemId = 1, Date = new DateTime(2017, 9, 20), AvailableStock = 100 },
        new() { StockId = 2, CatalogItemId = 1, Date = new DateTime(2017, 9, 21), AvailableStock = 120 },
        new() { StockId = 3, CatalogItemId = 1, Date = new DateTime(2017, 9, 22), AvailableStock = 80 },
        new() { StockId = 4, CatalogItemId = 2, Date = new DateTime(2017, 9, 20), AvailableStock = 45 },
        new() { StockId = 5, CatalogItemId = 4, Date = new DateTime(2017, 9, 25), AvailableStock = 65 },
        new() { StockId = 6, CatalogItemId = 5, Date = new DateTime(2017, 9, 28), AvailableStock = 22 },
    ];

    private readonly List<DiscountItem> _discounts =
    [
        new() { Id = 1, Start = new DateTime(2017, 9, 18), End = new DateTime(2017, 9, 21), Size = 0.3f },
        new() { Id = 2, Start = new DateTime(2017, 9, 22), End = new DateTime(2017, 9, 26), Size = 0.25f },
        new() { Id = 3, Start = new DateTime(2017, 9, 27), End = new DateTime(2017, 9, 30), Size = 0.1f },
        new() { Id = 4, Start = new DateTime(2017, 10, 5), End = new DateTime(2017, 10, 20), Size = 0.5f },
        new() { Id = 5, Start = new DateTime(2017, 11, 13), End = new DateTime(2017, 11, 25), Size = 0.3f },
        new() { Id = 6, Start = new DateTime(2017, 12, 20), End = new DateTime(2017, 12, 25), Size = 0.25f },
    ];

    public Task<int> GetAvailableStockAsync(DateTime date, int catalogItemId, CancellationToken cancellationToken = default)
    {
        lock (_stocks)
        {
            var stock = _stocks.FirstOrDefault(s => s.CatalogItemId == catalogItemId && s.Date.Date == date.Date);
            return Task.FromResult(stock?.AvailableStock ?? 0);
        }
    }

    public Task CreateAvailableStockAsync(CatalogItemsStock catalogItemsStock, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalogItemsStock);

        lock (_stocks)
        {
            var existing = _stocks.FirstOrDefault(s =>
                s.CatalogItemId == catalogItemsStock.CatalogItemId && s.Date.Date == catalogItemsStock.Date.Date);

            if (existing is not null)
            {
                existing.AvailableStock = catalogItemsStock.AvailableStock;
            }
            else
            {
                catalogItemsStock.StockId = _stocks.Count == 0 ? 1 : _stocks.Max(s => s.StockId) + 1;
                catalogItemsStock.Date = catalogItemsStock.Date.Date;
                _stocks.Add(catalogItemsStock);
            }
        }

        return Task.CompletedTask;
    }

    public Task<DiscountItem?> GetDiscountAsync(DateTime day, CancellationToken cancellationToken = default)
    {
        lock (_discounts)
        {
            var discount = _discounts.FirstOrDefault(d => d.Start.Date <= day.Date && d.End.Date >= day.Date);
            return Task.FromResult(discount);
        }
    }
}
