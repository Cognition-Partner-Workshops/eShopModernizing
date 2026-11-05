using eShop.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Data;

/// <summary>
/// EF Core 8 implementation of <see cref="ICatalogService"/>. It reproduces the legacy MVC
/// <c>CatalogService</c> query semantics (LongCount / Include / OrderBy / Skip / Take) and the
/// WCF brand+type filter.
/// </summary>
public class CatalogService : ICatalogService
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogItemIdGenerator _idGenerator;

    public CatalogService(CatalogDbContext db, ICatalogItemIdGenerator idGenerator)
    {
        _db = db;
        _idGenerator = idGenerator;
    }

    public CatalogItem? FindCatalogItem(int id) =>
        _db.CatalogItems
            .Include(c => c.CatalogBrand)
            .Include(c => c.CatalogType)
            .FirstOrDefault(ci => ci.Id == id);

    public IEnumerable<CatalogBrand> GetCatalogBrands() => _db.CatalogBrands.ToList();

    public IEnumerable<CatalogType> GetCatalogTypes() => _db.CatalogTypes.ToList();

    public IReadOnlyList<CatalogItem> GetCatalogItems(int brandIdFilter, int typeIdFilter)
    {
        IQueryable<CatalogItem> items = _db.CatalogItems
            .Include(c => c.CatalogBrand)
            .Include(c => c.CatalogType);

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

    public PaginatedItems<CatalogItem> GetCatalogItemsPaginated(int pageSize, int pageIndex)
    {
        var totalItems = _db.CatalogItems.LongCount();

        var itemsOnPage = _db.CatalogItems
            .Include(c => c.CatalogBrand)
            .Include(c => c.CatalogType)
            .OrderBy(c => c.Id)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToList();

        return new PaginatedItems<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);
    }

    public void CreateCatalogItem(CatalogItem catalogItem)
    {
        catalogItem.Id = _idGenerator.GetNextId(_db);
        _db.CatalogItems.Add(catalogItem);
        _db.SaveChanges();
    }

    public void UpdateCatalogItem(CatalogItem catalogItem)
    {
        _db.Entry(catalogItem).State = EntityState.Modified;
        _db.SaveChanges();
    }

    public void RemoveCatalogItem(CatalogItem catalogItem)
    {
        _db.CatalogItems.Remove(catalogItem);
        _db.SaveChanges();
    }
}
