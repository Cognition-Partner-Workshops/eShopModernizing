using eShop.Catalog.Api.Pictures;
using eShop.Catalog.Data;
using Microsoft.AspNetCore.Mvc;

namespace eShop.Catalog.Api.Controllers;

/// <summary>
/// Port of the legacy MVC <c>PicController</c>, keeping the <c>items/{catalogItemId:int}/pic</c>
/// route and its 400 / 404 semantics.
/// </summary>
[ApiController]
public sealed class PicController : ControllerBase
{
    public const string GetPicRouteName = "GetPicRouteTemplate";

    private readonly ICatalogService _service;
    private readonly CatalogPictureStore _pictures;

    public PicController(ICatalogService service, CatalogPictureStore pictures)
    {
        _service = service;
        _pictures = pictures;
    }

    /// <summary>Serves the picture of a catalog item.</summary>
    [HttpGet("items/{catalogItemId:int}/pic", Name = GetPicRouteName)]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Index(int catalogItemId)
    {
        if (catalogItemId <= 0)
        {
            return BadRequest();
        }

        var item = _service.FindCatalogItem(catalogItemId);
        if (item is null)
        {
            return NotFound();
        }

        var picture = _pictures.Find(item.PictureFileName);

        return picture is null ? NotFound() : File(picture.Content, picture.ContentType);
    }
}
