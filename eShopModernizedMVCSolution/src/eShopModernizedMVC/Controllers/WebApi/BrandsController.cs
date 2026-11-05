using eShopModernizedMVC.Models;
using eShopModernizedMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace eShopModernizedMVC.Controllers.WebApi
{
    [Route("api/brands")]
    public class BrandsController : ControllerBase
    {
        private readonly ICatalogService _service;

        public BrandsController(ICatalogService service) { _service = service; }

        [HttpGet]
        public IEnumerable<CatalogBrand> Get() => _service.GetCatalogBrands();

        [HttpGet("{id:int}")]
        public IActionResult Get(int id)
        {
            var brand = _service.GetCatalogBrands().FirstOrDefault(x => x.Id == id);
            if (brand == null) return NotFound();
            return Ok(brand);
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var brand = _service.GetCatalogBrands().FirstOrDefault(x => x.Id == id);
            if (brand == null) return NotFound();
            return Ok();
        }
    }
}
