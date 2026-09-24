using System.Collections.Generic;
using System.Linq;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.ViewModel;

namespace eShopLegacyMVC.Services.Catalog
{
    /// <summary>
    /// ICatalogService backed by the remote Catalog API. The MVC controllers/views keep their synchronous
    /// contract, so calls are awaited synchronously here at the process boundary.
    /// </summary>
    public class CatalogHttpService : ICatalogService
    {
        private readonly CatalogApiClient _client;

        public CatalogHttpService(CatalogApiClient client)
        {
            _client = client;
        }

        public PaginatedItemsViewModel<CatalogItem> GetCatalogItemsPaginated(int pageSize, int pageIndex)
        {
            var page = _client.GetItemsAsync(pageSize, pageIndex).GetAwaiter().GetResult();
            return new PaginatedItemsViewModel<CatalogItem>(
                page.PageIndex, page.PageSize, page.Count, page.Data.Select(ToModel).ToList());
        }

        public CatalogItem FindCatalogItem(int id)
        {
            var dto = _client.GetItemAsync(id).GetAwaiter().GetResult();
            return dto == null ? null : ToModel(dto);
        }

        public IEnumerable<CatalogBrand> GetCatalogBrands() =>
            _client.GetBrandsAsync().GetAwaiter().GetResult()
                .Select(b => new CatalogBrand { Id = b.Id, Brand = b.Brand })
                .ToList();

        public IEnumerable<CatalogType> GetCatalogTypes() =>
            _client.GetTypesAsync().GetAwaiter().GetResult()
                .Select(t => new CatalogType { Id = t.Id, Type = t.Type })
                .ToList();

        public void CreateCatalogItem(CatalogItem catalogItem)
        {
            var created = _client.CreateItemAsync(ToWriteDto(catalogItem)).GetAwaiter().GetResult();
            catalogItem.Id = created.Id;
        }

        public void UpdateCatalogItem(CatalogItem catalogItem) =>
            _client.UpdateItemAsync(catalogItem.Id, ToWriteDto(catalogItem)).GetAwaiter().GetResult();

        public void RemoveCatalogItem(CatalogItem catalogItem) =>
            _client.DeleteItemAsync(catalogItem.Id).GetAwaiter().GetResult();

        public void Dispose()
        {
        }

        private static CatalogItem ToModel(CatalogItemDto d) => new CatalogItem
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            Price = d.Price,
            PictureFileName = d.PictureFileName,
            CatalogTypeId = d.CatalogTypeId,
            CatalogType = d.CatalogType == null ? null : new CatalogType { Id = d.CatalogTypeId, Type = d.CatalogType },
            CatalogBrandId = d.CatalogBrandId,
            CatalogBrand = d.CatalogBrand == null ? null : new CatalogBrand { Id = d.CatalogBrandId, Brand = d.CatalogBrand },
            AvailableStock = d.AvailableStock,
            RestockThreshold = d.RestockThreshold,
            MaxStockThreshold = d.MaxStockThreshold,
            OnReorder = d.OnReorder,
        };

        private static CatalogItemWriteDto ToWriteDto(CatalogItem i) => new CatalogItemWriteDto
        {
            Name = i.Name,
            Description = i.Description,
            Price = i.Price,
            PictureFileName = i.PictureFileName,
            CatalogTypeId = i.CatalogTypeId,
            CatalogBrandId = i.CatalogBrandId,
            AvailableStock = i.AvailableStock,
            RestockThreshold = i.RestockThreshold,
            MaxStockThreshold = i.MaxStockThreshold,
            OnReorder = i.OnReorder,
        };
    }
}
