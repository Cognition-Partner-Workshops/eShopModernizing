using System;
using System.Collections.Generic;
using System.Linq;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.Models.Infrastructure;
using eShopLegacyMVC.ViewModel;

namespace eShopLegacyMVC.Services
{
    public class CatalogServiceMock : ICatalogService
    {
        private List<CatalogItem> catalogItems;

        public CatalogServiceMock()
        {
            catalogItems = new List<CatalogItem>(PreconfiguredData.GetPreconfiguredCatalogItems());
        }

        public PaginatedItemsViewModel<CatalogItem> GetCatalogItemsPaginated(int pageSize = 10, int pageIndex = 0)
        {
            var items = ComposeCatalogItems(catalogItems);
            
            var itemsOnPage = items
                .OrderBy(c => c.Id)
                .Skip(pageSize * pageIndex)
                .Take(pageSize)
                .ToList();

            return new PaginatedItemsViewModel<CatalogItem>(
                pageIndex, pageSize, items.Count, itemsOnPage);
        }

        public PaginatedItemsViewModel<CatalogItem> GetCatalogItemsPaginated(CatalogQuery query)
        {
            var pageSize = query.PageSize > 0 ? query.PageSize : CatalogQuery.DefaultPageSize;
            var pageIndex = query.PageIndex > 0 ? query.PageIndex : 0;
            var sort = CatalogSortOptions.Normalize(query.Sort);
            var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
            var items = ComposeCatalogItems(catalogItems);

            IEnumerable<CatalogItem> filtered = items;
            if (search != null)
            {
                filtered = filtered.Where(c => (c.Name != null
                    && c.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    || (c.Description != null
                    && c.Description.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
            }
            if (query.BrandId.HasValue)
            {
                filtered = filtered.Where(c => c.CatalogBrandId == query.BrandId.Value);
            }
            if (query.TypeId.HasValue)
            {
                filtered = filtered.Where(c => c.CatalogTypeId == query.TypeId.Value);
            }

            switch (sort)
            {
                case CatalogSortOptions.NameDesc:
                    filtered = filtered.OrderByDescending(c => c.Name).ThenBy(c => c.Id);
                    break;
                case CatalogSortOptions.PriceAsc:
                    filtered = filtered.OrderBy(c => c.Price).ThenBy(c => c.Id);
                    break;
                case CatalogSortOptions.PriceDesc:
                    filtered = filtered.OrderByDescending(c => c.Price).ThenBy(c => c.Id);
                    break;
                default:
                    filtered = filtered.OrderBy(c => c.Name).ThenBy(c => c.Id);
                    break;
            }

            var filteredItems = filtered.ToList();
            var itemsOnPage = filteredItems
                .Skip(pageSize * pageIndex)
                .Take(pageSize)
                .ToList();

            return new PaginatedItemsViewModel<CatalogItem>(
                pageIndex, pageSize, filteredItems.Count, itemsOnPage);
        }

        public CatalogItem FindCatalogItem(int id)
        {
            return catalogItems.FirstOrDefault(x => x.Id == id);
        }

        public IEnumerable<CatalogType> GetCatalogTypes()
        {
            return PreconfiguredData.GetPreconfiguredCatalogTypes();
        }

        public IEnumerable<CatalogBrand> GetCatalogBrands()
        {
            return PreconfiguredData.GetPreconfiguredCatalogBrands();
        }

        public void CreateCatalogItem(CatalogItem catalogItem)
        {
            var maxId = catalogItems.Max(i => i.Id);
            catalogItem.Id = ++maxId;
            catalogItems.Add(catalogItem);
        }

        public void UpdateCatalogItem(CatalogItem modifiedItem)
        {
            var originalItem = FindCatalogItem(modifiedItem.Id);
            if (originalItem != null)
            {
                catalogItems[catalogItems.IndexOf(originalItem)] = modifiedItem;
            }
        }

        public void RemoveCatalogItem(CatalogItem catalogItem)
        {
            catalogItems.Remove(catalogItem);
        }

        public void Dispose()
        {
        }

        private List<CatalogItem> ComposeCatalogItems(List<CatalogItem> items)
        {
            var catalogTypes = PreconfiguredData.GetPreconfiguredCatalogTypes();
            var catalogBrands = PreconfiguredData.GetPreconfiguredCatalogBrands();
            items.ForEach(i => i.CatalogBrand = catalogBrands.First(b => b.Id == i.CatalogBrandId));
            items.ForEach(i => i.CatalogType = catalogTypes.First(b => b.Id == i.CatalogTypeId));

            return items;
            ;
        }
    }
}