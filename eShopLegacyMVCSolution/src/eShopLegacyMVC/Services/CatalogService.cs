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
            var pageSize = query.PageSize > 0 ? query.PageSize : CatalogQuery.DefaultPageSize;
            var pageIndex = query.PageIndex > 0 ? query.PageIndex : 0;
            var sort = CatalogSortOptions.Normalize(query.Sort);
            var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

            IQueryable<CatalogItem> filtered = db.CatalogItems;
            if (search != null)
            {
                filtered = filtered.Where(c => c.Name.Contains(search)
                    || (c.Description != null && c.Description.Contains(search)));
            }
            if (query.BrandId.HasValue)
            {
                filtered = filtered.Where(c => c.CatalogBrandId == query.BrandId.Value);
            }
            if (query.TypeId.HasValue)
            {
                filtered = filtered.Where(c => c.CatalogTypeId == query.TypeId.Value);
            }

            IOrderedQueryable<CatalogItem> ordered;
            switch (sort)
            {
                case CatalogSortOptions.NameDesc:
                    ordered = filtered.OrderByDescending(c => c.Name).ThenBy(c => c.Id);
                    break;
                case CatalogSortOptions.PriceAsc:
                    ordered = filtered.OrderBy(c => c.Price).ThenBy(c => c.Id);
                    break;
                case CatalogSortOptions.PriceDesc:
                    ordered = filtered.OrderByDescending(c => c.Price).ThenBy(c => c.Id);
                    break;
                default:
                    ordered = filtered.OrderBy(c => c.Name).ThenBy(c => c.Id);
                    break;
            }

            var totalItems = filtered.LongCount();
            var itemsOnPage = ordered
                .Include(c => c.CatalogBrand)
                .Include(c => c.CatalogType)
                .Skip(pageSize * pageIndex)
                .Take(pageSize)
                .ToList();

            return new PaginatedItemsViewModel<CatalogItem>(
                pageIndex, pageSize, totalItems, itemsOnPage);
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