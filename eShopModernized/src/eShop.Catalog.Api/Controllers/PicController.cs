using eShop.Catalog.Domain.Abstractions;
using eShop.Shared.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace eShop.Catalog.Api.Controllers;

/// <summary>
/// Catalog item picture endpoint, ported from the legacy MVC
/// <c>eShopLegacyMVC.Controllers.PicController</c>. Pictures are read from
/// <see cref="CatalogOptions.PicsFolder"/> (relative to the content root) instead of
/// <c>Server.MapPath("~/Pics")</c>.
/// </summary>
[ApiController]
public class PicController : ControllerBase
{
    /// <summary>Route name of the picture endpoint, kept from the legacy controller.</summary>
    public const string GetPicRouteName = "GetPicRouteTemplate";

    private readonly ICatalogService _catalogService;
    private readonly IWebHostEnvironment _environment;
    private readonly CatalogOptions _options;
    private readonly ILogger<PicController> _logger;

    /// <summary>Creates the controller.</summary>
    public PicController(
        ICatalogService catalogService,
        IWebHostEnvironment environment,
        IOptions<CatalogOptions> options,
        ILogger<PicController> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _catalogService = catalogService;
        _environment = environment;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Returns the picture of a catalog item: 400 for a non-positive identifier, 404 when the item
    /// or its picture file does not exist, otherwise the file with its image content type.
    /// </summary>
    [HttpGet("items/{catalogItemId:int}/pic", Name = GetPicRouteName)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPic(int catalogItemId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Now loading picture of catalog item {CatalogItemId}", catalogItemId);

        if (catalogItemId <= 0)
        {
            return BadRequest();
        }

        var item = await _catalogService.FindCatalogItemAsync(catalogItemId, cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        var path = Path.Combine(PicsFolderPath(), item.PictureFileName);

        if (!System.IO.File.Exists(path))
        {
            _logger.LogWarning(
                "Picture file {PictureFileName} of catalog item {CatalogItemId} was not found",
                item.PictureFileName,
                catalogItemId);

            return NotFound();
        }

        var buffer = await System.IO.File.ReadAllBytesAsync(path, cancellationToken);

        return File(buffer, GetImageMimeTypeFromImageFileExtension(Path.GetExtension(item.PictureFileName)));
    }

    private string PicsFolderPath() =>
        Path.IsPathRooted(_options.PicsFolder)
            ? _options.PicsFolder
            : Path.Combine(_environment.ContentRootPath, _options.PicsFolder);

    /// <summary>Extension to content type mapping, kept identical to the legacy controller.</summary>
    private static string GetImageMimeTypeFromImageFileExtension(string extension) => extension switch
    {
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".bmp" => "image/bmp",
        ".tiff" => "image/tiff",
        ".wmf" => "image/wmf",
        ".jp2" => "image/jp2",
        ".svg" => "image/svg+xml",
        _ => "application/octet-stream",
    };
}
