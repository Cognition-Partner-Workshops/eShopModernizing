using System.Collections.Generic;
using System.Linq;
using eShopLegacyMVC.Services;
using eShopLegacyMVC.Services.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace eShopLegacyMVC.Controllers.WebApi
{
    /// <summary>
    /// /api/catalog* surface of the MVC app. Serves the in-process mock in UseMockData mode and
    /// otherwise proxies (via ICatalogService) to the extracted Catalog API, so clients of the
    /// monolith keep working during the transition.
    /// </summary>
    [ApiController]
    [Route("api/catalog")]
    public class CatalogApiController : ControllerBase
    {
        private readonly ICatalogService _service;

        public CatalogApiController(ICatalogService service)
        {
            _service = service;
        }

        [HttpGet("items")]
        public ActionResult<PaginatedItemsDto<CatalogItemDto>> Items([FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0)
        {
            if (pageSize <= 0 || pageIndex < 0)
            {
                return BadRequest("pageSize must be > 0 and pageIndex must be >= 0");
            }

            var page = _service.GetCatalogItemsPaginated(pageSize, pageIndex);
            return new PaginatedItemsDto<CatalogItemDto>
            {
                PageIndex = page.ActualPage,
                PageSize = page.ItemsPerPage,
                Count = page.TotalItems,
                Data = page.Data.Select(ToDto).ToList(),
            };
        }

        [HttpGet("items/{id:int}")]
        public ActionResult<CatalogItemDto> ItemById(int id)
        {
            var item = _service.FindCatalogItem(id);
            return item == null ? NotFound() : ToDto(item);
        }

        [HttpGet("brands")]
        public IEnumerable<CatalogBrandDto> Brands() =>
            _service.GetCatalogBrands().Select(b => new CatalogBrandDto { Id = b.Id, Brand = b.Brand });

        [HttpGet("types")]
        public IEnumerable<CatalogTypeDto> Types() =>
            _service.GetCatalogTypes().Select(t => new CatalogTypeDto { Id = t.Id, Type = t.Type });

        private static CatalogItemDto ToDto(Models.CatalogItem i) => new CatalogItemDto
        {
            Id = i.Id,
            Name = i.Name,
            Description = i.Description,
            Price = i.Price,
            PictureFileName = i.PictureFileName,
            CatalogTypeId = i.CatalogTypeId,
            CatalogType = i.CatalogType?.Type,
            CatalogBrandId = i.CatalogBrandId,
            CatalogBrand = i.CatalogBrand?.Brand,
            AvailableStock = i.AvailableStock,
            RestockThreshold = i.RestockThreshold,
            MaxStockThreshold = i.MaxStockThreshold,
            OnReorder = i.OnReorder,
        };
    }
}
