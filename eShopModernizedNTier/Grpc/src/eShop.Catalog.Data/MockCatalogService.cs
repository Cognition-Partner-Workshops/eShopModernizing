using eShop.Catalog.Domain;

namespace eShop.Catalog.Data;

/// <summary>
/// In-memory ICatalogService, the port of the legacy CatalogServiceMock. It keeps the solution
/// runnable until the EF Core 8 implementation lands in NET-64.
/// </summary>
public class MockCatalogService : ICatalogService
{
    private readonly List<CatalogItem> _catalogItems;
    private readonly List<CatalogBrand> _catalogBrands;
    private readonly List<CatalogType> _catalogTypes;

    public MockCatalogService()
    {
        _catalogItems = PreconfiguredData.GetPreconfiguredCatalogItems();
        _catalogBrands = PreconfiguredData.GetPreconfiguredCatalogBrands();
        _catalogTypes = PreconfiguredData.GetPreconfiguredCatalogTypes();
    }

    public CatalogItem? FindCatalogItem(int id) => _catalogItems.FirstOrDefault(x => x.Id == id);

    public IEnumerable<CatalogBrand> GetCatalogBrands() => _catalogBrands;

    public IEnumerable<CatalogType> GetCatalogTypes() => _catalogTypes;

    public IReadOnlyList<CatalogItem> GetCatalogItems(int brandIdFilter, int typeIdFilter)
    {
        IEnumerable<CatalogItem> items = Compose(_catalogItems);

        if (brandIdFilter > 0)
        {
            items = items.Where(i => i.CatalogBrandId == brandIdFilter);
        }

        if (typeIdFilter > 0)
        {
            items = items.Where(i => i.CatalogTypeId == typeIdFilter);
        }

        return items.OrderBy(i => i.Id).ToList();
    }

    public PaginatedItems<CatalogItem> GetCatalogItemsPaginated(int pageSize = 10, int pageIndex = 0)
    {
        var items = Compose(_catalogItems);

        var itemsOnPage = items
            .OrderBy(c => c.Id)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToList();

        return new PaginatedItems<CatalogItem>(pageIndex, pageSize, items.Count, itemsOnPage);
    }

    public void CreateCatalogItem(CatalogItem catalogItem)
    {
        catalogItem.Id = _catalogItems.Max(i => i.Id) + 1;
        _catalogItems.Add(catalogItem);
    }

    public void UpdateCatalogItem(CatalogItem modifiedItem)
    {
        var originalItem = FindCatalogItem(modifiedItem.Id);
        if (originalItem is not null)
        {
            _catalogItems[_catalogItems.IndexOf(originalItem)] = modifiedItem;
        }
    }

    public void RemoveCatalogItem(CatalogItem catalogItem) => _catalogItems.Remove(catalogItem);

    private List<CatalogItem> Compose(List<CatalogItem> items)
    {
        items.ForEach(i =>
        {
            i.CatalogBrand = _catalogBrands.First(b => b.Id == i.CatalogBrandId);
            i.CatalogType = _catalogTypes.First(t => t.Id == i.CatalogTypeId);
        });

        return items;
    }
}
