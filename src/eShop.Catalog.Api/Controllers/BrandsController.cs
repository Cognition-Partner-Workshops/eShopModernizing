using eShop.Catalog.Data;
using eShop.Catalog.Domain;
using Microsoft.AspNetCore.Mvc;

namespace eShop.Catalog.Api.Controllers;

/// <summary>
/// Port of the legacy Web API 2 <c>Controllers/WebApi/BrandsController</c>.
/// </summary>
[ApiController]
[Route("api/brands")]
[Produces("application/json")]
public sealed class BrandsController : ControllerBase
{
    private readonly ICatalogService _service;

    public BrandsController(ICatalogService service) => _service = service;

    /// <summary>Returns every catalog brand.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CatalogBrand>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<CatalogBrand>> Get() => Ok(_service.GetCatalogBrands());

    /// <summary>Returns one catalog brand, or 404 with an empty body when it does not exist.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CatalogBrand), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CatalogBrand> Get(int id)
    {
        var brand = _service.GetCatalogBrands().FirstOrDefault(x => x.Id == id);

        return brand is null ? NotFound() : Ok(brand);
    }
}
