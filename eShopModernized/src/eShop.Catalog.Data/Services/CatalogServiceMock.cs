using eShop.Catalog.Data.Infrastructure;
using eShop.Catalog.Domain;
using eShop.Catalog.Domain.Abstractions;
using eShop.Catalog.Domain.Entities;

namespace eShop.Catalog.Data.Services;

/// <summary>
/// In-memory <see cref="ICatalogService" /> ported from the legacy eShopLegacyMVC
/// CatalogServiceMock; selected when <c>Catalog:UseMockData</c> is true.
/// </summary>
public class CatalogServiceMock : ICatalogService
{
    private readonly List<CatalogItem> _catalogItems = PreconfiguredData.GetPreconfiguredCatalogItems();
    private readonly List<CatalogItemsStock> _catalogItemsStock = PreconfiguredData.GetPreconfiguredCatalogItemsStock();
    private readonly List<DiscountItem> _discountItems = PreconfiguredData.GetPreconfiguredDiscountItems();

    public PaginatedItemsViewModel<CatalogItem> GetCatalogItemsPaginated(int pageSize = 10, int pageIndex = 0)
    {
        var items = ComposeCatalogItems(_catalogItems);

        var itemsOnPage = items
            .OrderBy(ci => ci.Id)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToList();

        return new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, items.Count, itemsOnPage);
    }

    public Task<PaginatedItemsViewModel<CatalogItem>> GetCatalogItemsPaginatedAsync(int pageSize = 10, int pageIndex = 0, CancellationToken cancellationToken = default)
        => Task.FromResult(GetCatalogItemsPaginated(pageSize, pageIndex));

    public CatalogItem? FindCatalogItem(int id)
        => ComposeCatalogItems(_catalogItems).FirstOrDefault(ci => ci.Id == id);

    public Task<CatalogItem?> FindCatalogItemAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(FindCatalogItem(id));

    public IEnumerable<CatalogType> GetCatalogTypes()
        => PreconfiguredData.GetPreconfiguredCatalogTypes();

    public Task<IEnumerable<CatalogType>> GetCatalogTypesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(GetCatalogTypes());

    public IEnumerable<CatalogBrand> GetCatalogBrands()
        => PreconfiguredData.GetPreconfiguredCatalogBrands();

    public Task<IEnumerable<CatalogBrand>> GetCatalogBrandsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(GetCatalogBrands());

    public void CreateCatalogItem(CatalogItem catalogItem)
    {
        ArgumentNullException.ThrowIfNull(catalogItem);

        var maxId = _catalogItems.Max(ci => ci.Id);
        catalogItem.Id = ++maxId;
        _catalogItems.Add(catalogItem);
    }

    public Task CreateCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default)
    {
        CreateCatalogItem(catalogItem);
        return Task.CompletedTask;
    }

    public void UpdateCatalogItem(CatalogItem catalogItem)
    {
        ArgumentNullException.ThrowIfNull(catalogItem);

        var originalItem = _catalogItems.FirstOrDefault(ci => ci.Id == catalogItem.Id);
        if (originalItem != null)
        {
            _catalogItems[_catalogItems.IndexOf(originalItem)] = catalogItem;
        }
    }

    public Task UpdateCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default)
    {
        UpdateCatalogItem(catalogItem);
        return Task.CompletedTask;
    }

    public void RemoveCatalogItem(CatalogItem catalogItem)
    {
        ArgumentNullException.ThrowIfNull(catalogItem);

        _catalogItems.Remove(catalogItem);
    }

    public Task RemoveCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default)
    {
        RemoveCatalogItem(catalogItem);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<CatalogItem>> GetCatalogItemsAsync(int brandIdFilter, int typeIdFilter, CancellationToken cancellationToken = default)
    {
        var items = _catalogItems
            .Where(ci =>
                (brandIdFilter == 0 || ci.CatalogBrandId == brandIdFilter) &&
                (typeIdFilter == 0 || ci.CatalogTypeId == typeIdFilter))
            .OrderBy(ci => ci.Id)
            .ToList();

        return Task.FromResult<IEnumerable<CatalogItem>>(items);
    }

    public Task<int> GetAvailableStockAsync(DateTime date, int catalogItemId, CancellationToken cancellationToken = default)
        => Task.FromResult(FindStock(catalogItemId, date.Date)?.AvailableStock ?? 0);

    public Task CreateAvailableStockAsync(CatalogItemsStock catalogItemsStock, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalogItemsStock);

        var date = catalogItemsStock.Date.Date;

        var existing = FindStock(catalogItemsStock.CatalogItemId, date);

        if (existing is not null)
        {
            existing.AvailableStock = catalogItemsStock.AvailableStock;
        }
        else
        {
            var maxStockId = _catalogItemsStock.Count == 0 ? 0 : _catalogItemsStock.Max(stock => stock.StockId);

            _catalogItemsStock.Add(new CatalogItemsStock
            {
                StockId = maxStockId + 1,
                CatalogItemId = catalogItemsStock.CatalogItemId,
                AvailableStock = catalogItemsStock.AvailableStock,
                Date = date,
            });
        }

        return Task.CompletedTask;
    }

    public Task<DiscountItem?> GetDiscountAsync(DateTime day, CancellationToken cancellationToken = default)
    {
        var date = day.Date;

        var discount = _discountItems
            .Where(item => item.Start.Date <= date && item.End.Date >= date)
            .OrderBy(item => item.Id)
            .FirstOrDefault();

        return Task.FromResult(discount);
    }

    private CatalogItemsStock? FindStock(int catalogItemId, DateTime date)
        => _catalogItemsStock
            .Where(stock => stock.CatalogItemId == catalogItemId && stock.Date.Date == date)
            .OrderBy(stock => stock.StockId)
            .FirstOrDefault();

    private static List<CatalogItem> ComposeCatalogItems(List<CatalogItem> items)
    {
        var catalogTypes = PreconfiguredData.GetPreconfiguredCatalogTypes();
        var catalogBrands = PreconfiguredData.GetPreconfiguredCatalogBrands();

        items.ForEach(i => i.CatalogBrand = catalogBrands.FirstOrDefault(b => b.Id == i.CatalogBrandId));
        items.ForEach(i => i.CatalogType = catalogTypes.FirstOrDefault(t => t.Id == i.CatalogTypeId));

        return items;
    }
}
