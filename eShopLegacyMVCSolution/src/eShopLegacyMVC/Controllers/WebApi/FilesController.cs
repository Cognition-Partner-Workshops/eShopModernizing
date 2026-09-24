using eShopLegacy.Utilities;
using eShopLegacyMVC.Services;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace eShopLegacyMVC.Controllers.WebApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly ICatalogService _service;

        public FilesController(ICatalogService service)
        {
            _service = service;
        }

        // GET api/<controller>
        [HttpGet]
        public IActionResult Get()
        {
            var brands = _service.GetCatalogBrands()
                .Select(b => new BrandDTO
                {
                    Id = b.Id,
                    Brand = b.Brand
                }).ToList();
            var serializer = new Serializing();
            var stream = serializer.SerializeBinary(brands);

            return new FileStreamResult(stream, "application/octet-stream");
        }

        [Serializable]
        public class BrandDTO
        {
            public int Id { get; set; }
            public string Brand { get; set; }
        }
    }
}
