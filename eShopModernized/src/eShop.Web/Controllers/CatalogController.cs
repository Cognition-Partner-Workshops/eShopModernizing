using eShop.Catalog.Domain.Abstractions;
using eShop.Catalog.Domain.Entities;
using eShop.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace eShop.Web.Controllers;

/// <summary>
/// Catalog manager UI, ported from the legacy MVC 5 <c>eShopLegacyMVC.Controllers.CatalogController</c>.
/// The route shapes (<c>/Catalog</c>, <c>/Catalog/Index</c>, <c>/Catalog/Details/{id}</c>,
/// <c>/Catalog/Create</c>, <c>/Catalog/Edit/{id}</c>, <c>/Catalog/Delete/{id}</c>), the status codes and
/// the post/redirect/get behaviour are unchanged.
/// </summary>
public class CatalogController : Controller
{
    private readonly ICatalogService _service;
    private readonly ILogger<CatalogController> _logger;

    /// <summary>Creates the controller.</summary>
    public CatalogController(ICatalogService service, ILogger<CatalogController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Paginated catalog list; the legacy defaults of 10 items from page 0 are kept.</summary>
    public async Task<IActionResult> Index(int pageSize = 10, int pageIndex = 0)
    {
        _logger.LogInformation("Now loading... /Catalog/Index?pageSize={PageSize}&pageIndex={PageIndex}", pageSize, pageIndex);

        var paginatedItems = await _service.GetCatalogItemsPaginatedAsync(pageSize, pageIndex);
        ChangeUriPlaceholder(paginatedItems.Data);

        return View(paginatedItems);
    }

    /// <summary>Detail view: 400 without an id, 404 for an unknown id.</summary>
    public async Task<IActionResult> Details(int? id)
    {
        _logger.LogInformation("Now loading... /Catalog/Details?id={Id}", id);

        if (id is null)
        {
            return BadRequest();
        }

        var catalogItem = await _service.FindCatalogItemAsync(id.Value);
        if (catalogItem is null)
        {
            return NotFound();
        }

        AddUriPlaceHolder(catalogItem);

        return View(catalogItem);
    }

    /// <summary>Empty create form with the brand and type option lists.</summary>
    public async Task<IActionResult> Create()
    {
        _logger.LogInformation("Now loading... /Catalog/Create");

        return View(await WithOptionsAsync(new CatalogItemViewModel()));
    }

    /// <summary>Creates a catalog item and redirects to the list, as the legacy action did.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CatalogItemViewModel catalogItem)
    {
        ArgumentNullException.ThrowIfNull(catalogItem);

        _logger.LogInformation("Now processing... /Catalog/Create?catalogItemName={Name}", catalogItem.Name);

        if (ModelState.IsValid)
        {
            await _service.CreateCatalogItemAsync(catalogItem.ToCatalogItem());
            return RedirectToAction(nameof(Index));
        }

        return View(await WithOptionsAsync(catalogItem));
    }

    /// <summary>Prefilled edit form: 400 without an id, 404 for an unknown id.</summary>
    public async Task<IActionResult> Edit(int? id)
    {
        _logger.LogInformation("Now loading... /Catalog/Edit?id={Id}", id);

        if (id is null)
        {
            return BadRequest();
        }

        var catalogItem = await _service.FindCatalogItemAsync(id.Value);
        if (catalogItem is null)
        {
            return NotFound();
        }

        AddUriPlaceHolder(catalogItem);

        return View(await WithOptionsAsync(CatalogItemViewModel.FromCatalogItem(catalogItem)));
    }

    /// <summary>Updates a catalog item and redirects to the list, as the legacy action did.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CatalogItemViewModel catalogItem)
    {
        ArgumentNullException.ThrowIfNull(catalogItem);

        _logger.LogInformation("Now processing... /Catalog/Edit?id={Id}", catalogItem.Id);

        if (ModelState.IsValid)
        {
            await _service.UpdateCatalogItemAsync(catalogItem.ToCatalogItem());
            return RedirectToAction(nameof(Index));
        }

        return View(await WithOptionsAsync(catalogItem));
    }

    /// <summary>Delete confirmation view: 400 without an id, 404 for an unknown id.</summary>
    public async Task<IActionResult> Delete(int? id)
    {
        _logger.LogInformation("Now loading... /Catalog/Delete?id={Id}", id);

        if (id is null)
        {
            return BadRequest();
        }

        var catalogItem = await _service.FindCatalogItemAsync(id.Value);
        if (catalogItem is null)
        {
            return NotFound();
        }

        AddUriPlaceHolder(catalogItem);

        return View(catalogItem);
    }

    /// <summary>Removes a catalog item and redirects to the list, as the legacy action did.</summary>
    [HttpPost]
    [ActionName(nameof(Delete))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        _logger.LogInformation("Now processing... /Catalog/DeleteConfirmed?id={Id}", id);

        var catalogItem = await _service.FindCatalogItemAsync(id);
        if (catalogItem is null)
        {
            return NotFound();
        }

        await _service.RemoveCatalogItemAsync(catalogItem);

        return RedirectToAction(nameof(Index));
    }

    private async Task<CatalogItemViewModel> WithOptionsAsync(CatalogItemViewModel model) =>
        model.WithOptions(await _service.GetCatalogBrandsAsync(), await _service.GetCatalogTypesAsync());

    private void ChangeUriPlaceholder(IEnumerable<CatalogItem> items)
    {
        foreach (var catalogItem in items)
        {
            AddUriPlaceHolder(catalogItem);
        }
    }

    private void AddUriPlaceHolder(CatalogItem item) =>
        item.PictureUri = Url.RouteUrl(PicController.GetPicRouteName, new { catalogItemId = item.Id }, Request.Scheme);
}
