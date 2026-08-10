using eShop.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Data;

/// <summary>
/// EF Core 8 implementation of <see cref="ICatalogStockService"/>, a method-by-method port of the
/// stock and discount members of the legacy WCF <c>CatalogService.svc.cs</c>.
/// </summary>
public class CatalogStockService : ICatalogStockService
{
    private readonly CatalogDbContext _db;

    public CatalogStockService(CatalogDbContext db) => _db = db;

    public async Task<int> GetAvailableStockAsync(DateTime date, int catalogItemId, CancellationToken cancellationToken = default)
    {
        var day = date.Date;

        var stock = await _db.CatalogItemsStocks
            .Where(s => s.CatalogItemId == catalogItemId && s.Date == day)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return stock?.AvailableStock ?? 0;
    }

    public async Task CreateAvailableStockAsync(CatalogItemsStock catalogItemsStock, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalogItemsStock);

        var day = catalogItemsStock.Date.Date;

        var existing = await _db.CatalogItemsStocks
            .Where(s => s.CatalogItemId == catalogItemsStock.CatalogItemId && s.Date == day)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.AvailableStock = catalogItemsStock.AvailableStock;
            _db.CatalogItemsStocks.Update(existing);
        }
        else
        {
            var maxId = await _db.CatalogItemsStocks
                .Select(s => (int?)s.StockId)
                .MaxAsync(cancellationToken)
                .ConfigureAwait(false) ?? 0;

            catalogItemsStock.StockId = maxId + 1;
            catalogItemsStock.Date = day;
            await _db.CatalogItemsStocks.AddAsync(catalogItemsStock, cancellationToken).ConfigureAwait(false);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<DiscountItem?> GetDiscountAsync(DateTime day, CancellationToken cancellationToken = default)
    {
        var date = day.Date;

        return await _db.DiscountItems
            .Where(d => d.Start <= date && d.End >= date)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
