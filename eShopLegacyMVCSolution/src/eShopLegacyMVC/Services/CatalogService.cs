using eShopLegacyMVC.Models;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using eShopLegacyMVC.ViewModel;

namespace eShopLegacyMVC.Services
{

    public class CatalogService : ICatalogService
    {
        private CatalogDBContext db;
        private CatalogItemHiLoGenerator indexGenerator;

        public CatalogService(CatalogDBContext db, CatalogItemHiLoGenerator indexGenerator)
        {
            this.db = db;
            this.indexGenerator = indexGenerator;
        }

        public PaginatedItemsViewModel<CatalogItem> GetCatalogItemsPaginated(int pageSize, int pageIndex)
        {
            var totalItems = db.CatalogItems.LongCount();

            var itemsOnPage = db.CatalogItems
                .Include(c => c.CatalogBrand)
                .Include(c => c.CatalogType)
                .OrderBy(c => c.Id)
                .Skip(pageSize * pageIndex)
                .Take(pageSize)
                .ToList();

            return new PaginatedItemsViewModel<CatalogItem>(
                pageIndex, pageSize, totalItems, itemsOnPage);
        }

        public PaginatedItemsViewModel<CatalogItem> GetCatalogItemsPaginated(CatalogQuery query)
        {
            var items = db.CatalogItems.Include(c => c.CatalogBrand).Include(c => c.CatalogType).AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                items = items.Where(i => i.Name.Contains(query.Search) || i.Description.Contains(query.Search));
            }
            if (query.BrandId.HasValue)
            {
                items = items.Where(i => i.CatalogBrandId == query.BrandId.Value);
            }
            if (query.TypeId.HasValue)
            {
                items = items.Where(i => i.CatalogTypeId == query.TypeId.Value);
            }
            switch (CatalogSortOptions.Normalize(query.Sort))
            {
                case CatalogSortOptions.NameDesc:
                    items = items.OrderByDescending(i => i.Name).ThenBy(i => i.Id);
                    break;
                case CatalogSortOptions.PriceAsc:
                    items = items.OrderBy(i => i.Price).ThenBy(i => i.Id);
                    break;
                case CatalogSortOptions.PriceDesc:
                    items = items.OrderByDescending(i => i.Price).ThenBy(i => i.Id);
                    break;
                default:
                    items = items.OrderBy(i => i.Name).ThenBy(i => i.Id);
                    break;
            }
            var totalItems = items.LongCount();
            var itemsOnPage = items.Skip(query.PageSize * query.PageIndex).Take(query.PageSize).ToList();
            return new PaginatedItemsViewModel<CatalogItem>(query.PageIndex, query.PageSize, totalItems, itemsOnPage);
        }

        public CatalogItem FindCatalogItem(int id)
        {
            return db.CatalogItems.Include(c => c.CatalogBrand).Include(c => c.CatalogType).FirstOrDefault(ci => ci.Id == id);
        }
        public IEnumerable<CatalogType> GetCatalogTypes()
        {
            return db.CatalogTypes;
        }

        public IEnumerable<CatalogBrand> GetCatalogBrands()
        {
            return db.CatalogBrands;
        }

        public void CreateCatalogItem(CatalogItem catalogItem)
        {
            catalogItem.Id = indexGenerator.GetNextSequenceValue(db);
            db.CatalogItems.Add(catalogItem);
            db.SaveChanges();
        }

        public void UpdateCatalogItem(CatalogItem catalogItem)
        {
            db.Entry(catalogItem).State = EntityState.Modified;
            db.SaveChanges();
        }

        public void RemoveCatalogItem(CatalogItem catalogItem)
        {
            db.CatalogItems.Remove(catalogItem);
            db.SaveChanges();
        }

        public void Dispose()
        {
            db.Dispose();
        }
    }
}