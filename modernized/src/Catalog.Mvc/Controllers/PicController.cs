using Catalog.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Mvc.Controllers;

/// <summary>
/// Serves catalog item images, ported from the legacy ASP.NET MVC 5
/// <c>eShopLegacyMVC.Controllers.PicController</c>. Preserves the baseline route
/// <c>GET /items/{catalogItemId}/pic</c>, reading the file from the app's
/// <c>wwwroot/Pics</c> folder (replacing <c>Server.MapPath("~/Pics")</c>).
/// </summary>
public class PicController : Controller
{
    public const string GetPicRouteName = "GetPicRouteTemplate";
    private const string PicsFolder = "Pics";

    private readonly CatalogDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PicController> _logger;

    public PicController(
        CatalogDbContext db,
        IWebHostEnvironment environment,
        ILogger<PicController> logger)
    {
        _db = db;
        _environment = environment;
        _logger = logger;
    }

    // GET: /items/5/pic
    [HttpGet]
    [Route("items/{catalogItemId:int}/pic", Name = GetPicRouteName)]
    public async Task<IActionResult> Index(int catalogItemId)
    {
        _logger.LogInformation("Now loading... /items/{CatalogItemId}/pic", catalogItemId);

        if (catalogItemId <= 0)
        {
            return BadRequest();
        }

        var item = await _db.CatalogItems.FirstOrDefaultAsync(c => c.Id == catalogItemId);
        if (item is null)
        {
            return NotFound();
        }

        var webRoot = _environment.WebRootPath;
        var path = Path.Combine(webRoot, PicsFolder, item.PictureFileName);

        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var mimeType = GetImageMimeTypeFromImageFileExtension(Path.GetExtension(item.PictureFileName));
        var buffer = await System.IO.File.ReadAllBytesAsync(path);

        return File(buffer, mimeType);
    }

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
