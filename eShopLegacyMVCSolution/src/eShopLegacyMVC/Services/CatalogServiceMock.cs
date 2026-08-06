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
            var items = ComposeCatalogItems(catalogItems).AsEnumerable();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                items = items.Where(i => i.Name.IndexOf(query.Search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (i.Description ?? string.Empty).IndexOf(query.Search, StringComparison.OrdinalIgnoreCase) >= 0);
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
            var filtered = items.ToList();
            return new PaginatedItemsViewModel<CatalogItem>(query.PageIndex, query.PageSize,
                filtered.Count, filtered.Skip(query.PageSize * query.PageIndex).Take(query.PageSize).ToList());
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