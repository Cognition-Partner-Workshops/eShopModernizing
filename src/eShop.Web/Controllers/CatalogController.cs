using eShop.Catalog.Data;
using eShop.Catalog.Domain;
using eShop.Web.Configuration;
using eShop.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace eShop.Web.Controllers;

/// <summary>
/// Port of the legacy MVC 5 <c>eShopLegacyMVC.Controllers.CatalogController</c>: same routes, same
/// anti-forgery placement, same 400/404 argument handling and the same redirects to the catalog
/// index (which is the application root).
/// </summary>
public class CatalogController : Controller
{
    /// <summary>Properties the Create and Edit posts are allowed to bind (legacy [Bind(Include = …)]).</summary>
    private const string EditableProperties =
        "Id,Name,Description,Price,PictureFileName,CatalogTypeId,CatalogBrandId,AvailableStock,RestockThreshold,MaxStockThreshold,OnReorder";

    private readonly ICatalogService _service;
    private readonly ILogger<CatalogController> _log;
    private readonly CatalogWebOptions _options;

    public CatalogController(
        ICatalogService service,
        IOptions<CatalogWebOptions> options,
        ILogger<CatalogController> log)
    {
        _service = service;
        _options = options.Value;
        _log = log;
    }

    // GET /[?pageSize=3&pageIndex=10]
    public IActionResult Index(int pageSize = 10, int pageIndex = 0)
    {
        _log.LogInformation("Now loading... /Catalog/Index?pageSize={PageSize}&pageIndex={PageIndex}", pageSize, pageIndex);
        var paginatedItems = _service.GetCatalogItemsPaginated(pageSize, pageIndex);
        ChangeUriPlaceholder(paginatedItems.Data);

        return View(paginatedItems);
    }

    // GET: Catalog/Details/5
    public IActionResult Details(int? id)
    {
        _log.LogInformation("Now loading... /Catalog/Details?id={Id}", id);
        if (id is null)
        {
            return BadRequest();
        }

        var catalogItem = _service.FindCatalogItem(id.Value);
        if (catalogItem is null)
        {
            return NotFound();
        }

        AddUriPlaceHolder(catalogItem);

        return View(catalogItem);
    }

    // GET: Catalog/Create
    public IActionResult Create()
    {
        _log.LogInformation("Now loading... /Catalog/Create");

        return View(BuildFormViewModel(new CatalogItem()));
    }

    // POST: Catalog/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create([Bind(EditableProperties)] CatalogItem item)
    {
        _log.LogInformation("Now processing... /Catalog/Create?catalogItemName={Name}", item.Name);
        if (ModelState.IsValid)
        {
            _service.CreateCatalogItem(item);
            return RedirectToAction(nameof(Index));
        }

        return View(BuildFormViewModel(item));
    }

    // GET: Catalog/Edit/5
    public IActionResult Edit(int? id)
    {
        _log.LogInformation("Now loading... /Catalog/Edit?id={Id}", id);
        if (id is null)
        {
            return BadRequest();
        }

        var catalogItem = _service.FindCatalogItem(id.Value);
        if (catalogItem is null)
        {
            return NotFound();
        }

        AddUriPlaceHolder(catalogItem);

        return View(BuildFormViewModel(catalogItem));
    }

    // POST: Catalog/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit([Bind(EditableProperties)] CatalogItem item)
    {
        _log.LogInformation("Now processing... /Catalog/Edit?id={Id}", item.Id);
        if (ModelState.IsValid)
        {
            _service.UpdateCatalogItem(item);
            return RedirectToAction(nameof(Index));
        }

        AddUriPlaceHolder(item);

        return View(BuildFormViewModel(item));
    }

    // GET: Catalog/Delete/5
    public IActionResult Delete(int? id)
    {
        _log.LogInformation("Now loading... /Catalog/Delete?id={Id}", id);
        if (id is null)
        {
            return BadRequest();
        }

        var catalogItem = _service.FindCatalogItem(id.Value);
        if (catalogItem is null)
        {
            return NotFound();
        }

        AddUriPlaceHolder(catalogItem);

        return View(catalogItem);
    }

    // POST: Catalog/Delete/5
    [HttpPost]
    [ActionName(nameof(Delete))]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        _log.LogInformation("Now processing... /Catalog/DeleteConfirmed?id={Id}", id);
        var catalogItem = _service.FindCatalogItem(id);
        if (catalogItem is not null)
        {
            _service.RemoveCatalogItem(catalogItem);
        }

        return RedirectToAction(nameof(Index));
    }

    private CatalogItemFormViewModel BuildFormViewModel(CatalogItem item) =>
        new(item, _service.GetCatalogBrands(), _service.GetCatalogTypes());

    private void ChangeUriPlaceholder(IEnumerable<CatalogItem> items)
    {
        foreach (var catalogItem in items)
        {
            AddUriPlaceHolder(catalogItem);
        }
    }

    /// <summary>
    /// Legacy behaviour: the picture URI is an absolute link to <c>items/{id}/pic</c>. That route
    /// now lives in the catalog API, so the base address is configurable and falls back to the
    /// current request's scheme and host.
    /// </summary>
    private void AddUriPlaceHolder(CatalogItem item)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.PicturesBaseUrl)
            ? $"{Request.Scheme}://{Request.Host}"
            : _options.PicturesBaseUrl.TrimEnd('/');

        item.PictureUri = $"{baseUrl}/items/{item.Id}/pic";
    }
}
