using eShop.Catalog.Domain.Abstractions;
using eShop.Catalog.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace eShop.Catalog.Api.Controllers;

/// <summary>
/// Catalog brands endpoint, ported from the legacy Web API 2
/// <c>eShopLegacyMVC.Controllers.WebApi.BrandsController</c>.
/// The legacy <c>DELETE api/brands/{id}</c> action was a documented no-op ("demo only - don't
/// actually delete") and is not ported — decision D-03, recorded as an accepted delta.
/// </summary>
[ApiController]
[Route("api/brands")]
[Produces("application/json")]
public class BrandsController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    /// <summary>Creates the controller.</summary>
    public BrandsController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    /// <summary>Returns every catalog brand.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CatalogBrand>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CatalogBrand>>> GetBrands(CancellationToken cancellationToken)
    {
        var brands = await _catalogService.GetCatalogBrandsAsync(cancellationToken);

        return Ok(brands);
    }

    /// <summary>Returns a single catalog brand, or 404 when the identifier is unknown.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CatalogBrand), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CatalogBrand>> GetBrand(int id, CancellationToken cancellationToken)
    {
        var brands = await _catalogService.GetCatalogBrandsAsync(cancellationToken);
        var brand = brands.FirstOrDefault(b => b.Id == id);

        return brand is null ? NotFound() : Ok(brand);
    }
}
