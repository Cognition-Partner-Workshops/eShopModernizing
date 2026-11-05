using Microsoft.EntityFrameworkCore;

namespace eShopWcf.Catalog.Data;

public sealed class CatalogService(CatalogDbContext db)
{
    public Task<CatalogItem?> FindCatalogItem(int id, CancellationToken cancellationToken = default) =>
        db.CatalogItems.AsNoTracking().Include(x => x.CatalogBrand).Include(x => x.CatalogType)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<CatalogBrand>> GetCatalogBrands(CancellationToken cancellationToken = default) =>
        db.CatalogBrands.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);

    public Task<List<CatalogType>> GetCatalogTypes(CancellationToken cancellationToken = default) =>
        db.CatalogTypes.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);

    public Task<List<CatalogItem>> GetCatalogItems(int brand, int type, CancellationToken cancellationToken = default) =>
        db.CatalogItems.AsNoTracking().Where(x => (brand == 0 || x.CatalogBrandId == brand) && (type == 0 || x.CatalogTypeId == type))
            .OrderBy(x => x.Id).ToListAsync(cancellationToken);

    public async Task<int> GetAvailableStock(DateTime date, int itemId, CancellationToken cancellationToken = default)
    {
        var day = date.Date;
        return await db.CatalogItemsStocks.AsNoTracking()
            .Where(x => x.CatalogItemId == itemId && x.Date == day).OrderBy(x => x.StockId)
            .Select(x => x.AvailableStock).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CreateAvailableStock(CatalogItemsStock stock, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stock);
        stock.Date = stock.Date.Date;
        var existing = await db.CatalogItemsStocks.Where(x => x.CatalogItemId == stock.CatalogItemId && x.Date == stock.Date)
            .OrderBy(x => x.StockId).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            existing.AvailableStock = stock.AvailableStock;
        }
        else
        {
            stock.StockId = (await db.CatalogItemsStocks.MaxAsync(x => (int?)x.StockId, cancellationToken).ConfigureAwait(false) ?? 0) + 1;
            db.CatalogItemsStocks.Add(stock);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CreateCatalogItem(CatalogItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.Id = (await db.CatalogItems.MaxAsync(x => (int?)x.Id, cancellationToken).ConfigureAwait(false) ?? 0) + 1;
        db.CatalogItems.Add(item);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> UpdateCatalogItem(CatalogItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        var existing = await db.CatalogItems.FindAsync([item.Id], cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return false;
        }

        db.Entry(existing).CurrentValues.SetValues(item);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> RemoveCatalogItem(int id, CancellationToken cancellationToken = default)
    {
        var existing = await db.CatalogItems.FindAsync([id], cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return false;
        }

        db.CatalogItems.Remove(existing);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public Task<DiscountItem?> GetDiscount(DateTime date, CancellationToken cancellationToken = default)
    {
        var day = date.Date;
        return db.DiscountItems.AsNoTracking().Where(x => x.Start <= day && x.End >= day)
            .OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken);
    }
}
