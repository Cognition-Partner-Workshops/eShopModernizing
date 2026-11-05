using eShopModernizedMVC.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace eShopModernizedMVC.Controllers.WebApi
{
    [Route("api/files")]
    public class FilesController : ControllerBase
    {
        private readonly ICatalogService _service;

        public FilesController(ICatalogService service) { _service = service; }

        [HttpGet]
        public IActionResult Get()
        {
            var brands = _service.GetCatalogBrands()
                .Select(b => new BrandDTO { Id = b.Id, Brand = b.Brand }).ToList();
            return File(JsonSerializer.SerializeToUtf8Bytes(brands), "application/json");
        }

        public class BrandDTO
        {
            public int Id { get; set; }
            public string Brand { get; set; }
        }
    }
}
