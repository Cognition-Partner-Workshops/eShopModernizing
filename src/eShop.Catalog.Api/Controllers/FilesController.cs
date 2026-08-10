using eShop.Catalog.Data;
using eShop.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace eShop.Catalog.Api.Controllers;

/// <summary>
/// Port of the legacy Web API 2 <c>Controllers/WebApi/FilesController</c>. The legacy action
/// returned a legacy binary-formatted stream; the modernized endpoint returns the same logical
/// payload as JSON (documented contract change, NET-63).
/// </summary>
[ApiController]
[Route("api/files")]
[Produces("application/json")]
public sealed class FilesController : ControllerBase
{
    private readonly ICatalogService _service;

    public FilesController(ICatalogService service) => _service = service;

    /// <summary>Returns the catalog brands projected onto the <see cref="BrandDto"/> contract.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BrandDto>), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var brands = _service.GetCatalogBrands()
            .Select(b => new BrandDto { Id = b.Id, Brand = b.Brand })
            .ToList();

        return Content(BrandDtoSerializer.Serialize(brands), "application/json");
    }
}
