using eShop.Catalog.Domain.Abstractions;
using eShop.Shared.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace eShop.Catalog.Api.Controllers;

/// <summary>
/// Ported from the legacy Web API 2 <c>eShopLegacyMVC.Controllers.WebApi.FilesController</c>, which
/// streamed the brand list as a runtime binary serialization payload served as <c>text/html</c>.
/// That formatter is banned (NET-63), so the same data is now returned as
/// <c>application/json</c> using the shared <see cref="BrandDTO"/> shape — accepted delta.
/// </summary>
[ApiController]
[Route("api/files")]
[Produces("application/json")]
public class FilesController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    /// <summary>Creates the controller.</summary>
    public FilesController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    /// <summary>Returns every catalog brand in the <see cref="BrandDTO"/> wire shape.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BrandDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BrandDTO>>> GetBrandFile(CancellationToken cancellationToken)
    {
        var brands = await _catalogService.GetCatalogBrandsAsync(cancellationToken);

        var payload = brands
            .Select(b => new BrandDTO { Id = b.Id, Brand = b.Brand })
            .ToList();

        return Ok(payload);
    }
}
