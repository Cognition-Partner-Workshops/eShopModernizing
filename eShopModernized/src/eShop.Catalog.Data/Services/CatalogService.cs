using eShop.Catalog.Domain;
using eShop.Catalog.Domain.Abstractions;
using eShop.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Data.Services;

/// <summary>
/// EF Core implementation of <see cref="ICatalogService" />, ported from the legacy
/// eShopLegacyMVC CatalogService. Query shape (LongCount / Include / OrderBy / Skip / Take)
/// is preserved so paging output stays identical to the legacy baseline.
/// </summary>
public class CatalogService : ICatalogService
{
    private readonly CatalogDbContext _db;

    public CatalogService(CatalogDbContext db)
    {
        _db = db;
    }

    public PaginatedItemsViewModel<CatalogItem> GetCatalogItemsPaginated(int pageSize = 10, int pageIndex = 0)
    {
        var totalItems = _db.CatalogItems.LongCount();

        var itemsOnPage = PageQuery(pageSize, pageIndex).ToList();

        return new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);
    }

    public async Task<PaginatedItemsViewModel<CatalogItem>> GetCatalogItemsPaginatedAsync(int pageSize = 10, int pageIndex = 0, CancellationToken cancellationToken = default)
    {
        var totalItems = await _db.CatalogItems.LongCountAsync(cancellationToken).ConfigureAwait(false);

        var itemsOnPage = await PageQuery(pageSize, pageIndex).ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);
    }

    public CatalogItem? FindCatalogItem(int id)
        => ItemWithNavigations().FirstOrDefault(ci => ci.Id == id);

    public Task<CatalogItem?> FindCatalogItemAsync(int id, CancellationToken cancellationToken = default)
        => ItemWithNavigations().FirstOrDefaultAsync(ci => ci.Id == id, cancellationToken);

    public IEnumerable<CatalogType> GetCatalogTypes()
        => _db.CatalogTypes.ToList();

    public async Task<IEnumerable<CatalogType>> GetCatalogTypesAsync(CancellationToken cancellationToken = default)
        => await _db.CatalogTypes.ToListAsync(cancellationToken).ConfigureAwait(false);

    public IEnumerable<CatalogBrand> GetCatalogBrands()
        => _db.CatalogBrands.ToList();

    public async Task<IEnumerable<CatalogBrand>> GetCatalogBrandsAsync(CancellationToken cancellationToken = default)
        => await _db.CatalogBrands.ToListAsync(cancellationToken).ConfigureAwait(false);

    public void CreateCatalogItem(CatalogItem catalogItem)
    {
        _db.CatalogItems.Add(catalogItem);
        _db.SaveChanges();
    }

    public async Task CreateCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default)
    {
        await _db.CatalogItems.AddAsync(catalogItem, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public void UpdateCatalogItem(CatalogItem catalogItem)
    {
        _db.Entry(catalogItem).State = EntityState.Modified;
        _db.SaveChanges();
    }

    public Task UpdateCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default)
    {
        _db.Entry(catalogItem).State = EntityState.Modified;
        return _db.SaveChangesAsync(cancellationToken);
    }

    public void RemoveCatalogItem(CatalogItem catalogItem)
    {
        _db.CatalogItems.Remove(catalogItem);
        _db.SaveChanges();
    }

    public Task RemoveCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default)
    {
        _db.CatalogItems.Remove(catalogItem);
        return _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<CatalogItem>> GetCatalogItemsAsync(int brandIdFilter, int typeIdFilter, CancellationToken cancellationToken = default)
    {
        var query = _db.CatalogItems.AsNoTracking();

        if (brandIdFilter != 0)
        {
            query = query.Where(ci => ci.CatalogBrandId == brandIdFilter);
        }

        if (typeIdFilter != 0)
        {
            query = query.Where(ci => ci.CatalogTypeId == typeIdFilter);
        }

        return await query
            .OrderBy(ci => ci.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> GetAvailableStockAsync(DateTime date, int catalogItemId, CancellationToken cancellationToken = default)
    {
        var stock = await FindStockAsync(catalogItemId, date.Date, cancellationToken).ConfigureAwait(false);

        return stock?.AvailableStock ?? 0;
    }

    public async Task CreateAvailableStockAsync(CatalogItemsStock catalogItemsStock, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalogItemsStock);

        var date = catalogItemsStock.Date.Date;

        var existing = await FindStockAsync(catalogItemsStock.CatalogItemId, date, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            existing.AvailableStock = catalogItemsStock.AvailableStock;
            _db.Update(existing);
        }
        else
        {
            var maxStockId = await _db.CatalogItemsStocks
                .MaxAsync(stock => (int?)stock.StockId, cancellationToken)
                .ConfigureAwait(false) ?? 0;

            _db.CatalogItemsStocks.Add(new CatalogItemsStock
            {
                StockId = maxStockId + 1,
                CatalogItemId = catalogItemsStock.CatalogItemId,
                AvailableStock = catalogItemsStock.AvailableStock,
                Date = date,
            });
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<DiscountItem?> GetDiscountAsync(DateTime day, CancellationToken cancellationToken = default)
    {
        var date = day.Date;

        return _db.DiscountItems
            .AsNoTracking()
            .Where(item => item.Start <= date && item.End >= date)
            .OrderBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private Task<CatalogItemsStock?> FindStockAsync(int catalogItemId, DateTime date, CancellationToken cancellationToken)
        => _db.CatalogItemsStocks
            .Where(stock => stock.CatalogItemId == catalogItemId && stock.Date == date)
            .OrderBy(stock => stock.StockId)
            .FirstOrDefaultAsync(cancellationToken);

    private IQueryable<CatalogItem> ItemWithNavigations()
        => _db.CatalogItems
            .Include(ci => ci.CatalogBrand)
            .Include(ci => ci.CatalogType);

    private IQueryable<CatalogItem> PageQuery(int pageSize, int pageIndex)
        => ItemWithNavigations()
            .OrderBy(ci => ci.Id)
            .Skip(pageSize * pageIndex)
            .Take(pageSize);
}
