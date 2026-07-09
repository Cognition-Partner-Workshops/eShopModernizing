using Catalog.Domain;
using Catalog.Infrastructure;
using Catalog.Mvc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Mvc.Controllers;

/// <summary>
/// Catalog CRUD controller, ported from the legacy ASP.NET MVC 5
/// <c>eShopLegacyMVC.Controllers.CatalogController</c>. Preserves the baseline
/// routes/actions (Index paging via <c>Skip</c>/<c>Take</c>, Details, Create,
/// Edit, Delete) and adds brand/type filtering to the listing. Replaces the
/// Autofac-injected <c>ICatalogService</c> with the EF Core 8
/// <see cref="CatalogDbContext"/> and log4net with an injected
/// <see cref="ILogger{TCategoryName}"/>.
/// </summary>
public class CatalogController : Controller
{
    public const int DefaultPageIndex = 0;
    public const int DefaultPageSize = 10;

    private readonly CatalogDbContext _db;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(CatalogDbContext db, ILogger<CatalogController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET: / , /Catalog/Index?pageSize=10&pageIndex=0&brandFilter=&typeFilter=
    public async Task<IActionResult> Index(
        int pageSize = DefaultPageSize,
        int pageIndex = DefaultPageIndex,
        int? brandFilter = null,
        int? typeFilter = null)
    {
        if (pageSize <= 0)
        {
            pageSize = DefaultPageSize;
        }

        if (pageIndex < 0)
        {
            pageIndex = DefaultPageIndex;
        }

        _logger.LogInformation(
            "Now loading... /Catalog/Index?pageSize={PageSize}&pageIndex={PageIndex}&brandFilter={BrandFilter}&typeFilter={TypeFilter}",
            pageSize, pageIndex, brandFilter, typeFilter);

        IQueryable<CatalogItem> query = _db.CatalogItems
            .Include(c => c.CatalogBrand)
            .Include(c => c.CatalogType);

        if (brandFilter is > 0)
        {
            query = query.Where(c => c.CatalogBrandId == brandFilter);
        }

        if (typeFilter is > 0)
        {
            query = query.Where(c => c.CatalogTypeId == typeFilter);
        }

        var totalItems = await query.LongCountAsync();

        var itemsOnPage = await query
            .OrderBy(c => c.Id)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        var model = new CatalogIndexViewModel
        {
            Items = new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage),
            Brands = await BuildBrandsAsync(brandFilter),
            Types = await BuildTypesAsync(typeFilter),
            BrandFilter = brandFilter,
            TypeFilter = typeFilter,
        };

        return View(model);
    }

    // GET: /Catalog/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        _logger.LogInformation("Now loading... /Catalog/Details?id={ProductId}", id);

        if (id is null)
        {
            return BadRequest();
        }

        var catalogItem = await _db.CatalogItems
            .Include(c => c.CatalogBrand)
            .Include(c => c.CatalogType)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (catalogItem is null)
        {
            return NotFound();
        }

        return View(catalogItem);
    }

    // GET: /Catalog/Create
    public async Task<IActionResult> Create()
    {
        _logger.LogInformation("Now loading... /Catalog/Create");

        ViewBag.CatalogBrandId = await BuildBrandsAsync(null);
        ViewBag.CatalogTypeId = await BuildTypesAsync(null);
        return View(new CatalogItemInputModel());
    }

    // POST: /Catalog/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CatalogItemInputModel input)
    {
        _logger.LogInformation("Now processing... /Catalog/Create?catalogItemName={ProductName}", input.Name);

        if (ModelState.IsValid)
        {
            var catalogItem = new CatalogItem
            {
                Name = input.Name,
                Description = input.Description,
                CatalogBrandId = input.CatalogBrandId,
                CatalogTypeId = input.CatalogTypeId,
                Price = input.Price,
                AvailableStock = input.AvailableStock,
                RestockThreshold = input.RestockThreshold,
                MaxStockThreshold = input.MaxStockThreshold,
            };

            _db.CatalogItems.Add(catalogItem);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        ViewBag.CatalogBrandId = await BuildBrandsAsync(input.CatalogBrandId);
        ViewBag.CatalogTypeId = await BuildTypesAsync(input.CatalogTypeId);
        return View(input);
    }

    // GET: /Catalog/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        _logger.LogInformation("Now loading... /Catalog/Edit?id={ProductId}", id);

        if (id is null)
        {
            return BadRequest();
        }

        var catalogItem = await _db.CatalogItems.FirstOrDefaultAsync(c => c.Id == id);
        if (catalogItem is null)
        {
            return NotFound();
        }

        ViewBag.CatalogBrandId = await BuildBrandsAsync(catalogItem.CatalogBrandId);
        ViewBag.CatalogTypeId = await BuildTypesAsync(catalogItem.CatalogTypeId);
        return View(ToInputModel(catalogItem));
    }

    // POST: /Catalog/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CatalogItemInputModel input)
    {
        _logger.LogInformation("Now processing... /Catalog/Edit?id={ProductId}", input.Id);

        if (ModelState.IsValid)
        {
            var catalogItem = await _db.CatalogItems.FirstOrDefaultAsync(c => c.Id == input.Id);
            if (catalogItem is null)
            {
                return NotFound();
            }

            catalogItem.Name = input.Name;
            catalogItem.Description = input.Description;
            catalogItem.CatalogBrandId = input.CatalogBrandId;
            catalogItem.CatalogTypeId = input.CatalogTypeId;
            catalogItem.Price = input.Price;
            catalogItem.PictureFileName = input.PictureFileName;
            catalogItem.AvailableStock = input.AvailableStock;
            catalogItem.RestockThreshold = input.RestockThreshold;
            catalogItem.MaxStockThreshold = input.MaxStockThreshold;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        ViewBag.CatalogBrandId = await BuildBrandsAsync(input.CatalogBrandId);
        ViewBag.CatalogTypeId = await BuildTypesAsync(input.CatalogTypeId);
        return View(input);
    }

    // GET: /Catalog/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        _logger.LogInformation("Now loading... /Catalog/Delete?id={ProductId}", id);

        if (id is null)
        {
            return BadRequest();
        }

        var catalogItem = await _db.CatalogItems
            .Include(c => c.CatalogBrand)
            .Include(c => c.CatalogType)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (catalogItem is null)
        {
            return NotFound();
        }

        return View(catalogItem);
    }

    // POST: /Catalog/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        _logger.LogInformation("Now processing... /Catalog/DeleteConfirmed?id={ProductId}", id);

        var catalogItem = await _db.CatalogItems.FirstOrDefaultAsync(c => c.Id == id);
        if (catalogItem is not null)
        {
            _db.CatalogItems.Remove(catalogItem);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private static CatalogItemInputModel ToInputModel(CatalogItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Description = item.Description,
        CatalogBrandId = item.CatalogBrandId,
        CatalogTypeId = item.CatalogTypeId,
        Price = item.Price,
        PictureFileName = item.PictureFileName,
        AvailableStock = item.AvailableStock,
        RestockThreshold = item.RestockThreshold,
        MaxStockThreshold = item.MaxStockThreshold,
    };

    private async Task<SelectList> BuildBrandsAsync(int? selected)
    {
        var brands = await _db.CatalogBrands.OrderBy(b => b.Id).ToListAsync();
        return new SelectList(brands, nameof(CatalogBrand.Id), nameof(CatalogBrand.Brand), selected);
    }

    private async Task<SelectList> BuildTypesAsync(int? selected)
    {
        var types = await _db.CatalogTypes.OrderBy(t => t.Id).ToListAsync();
        return new SelectList(types, nameof(CatalogType.Id), nameof(CatalogType.Type), selected);
    }
}
