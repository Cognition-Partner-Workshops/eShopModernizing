using Catalog.Domain;
using Catalog.Infrastructure;
using Catalog.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Web.Pages.Catalog;

/// <summary>
/// Edit catalog item page, ported from the legacy Web Forms
/// <c>Catalog/Edit.aspx</c> and its <c>Save_Click</c> code-behind. The picture
/// file name is preserved (read-only), matching the legacy behavior.
/// </summary>
public class EditModel : PageModel
{
    private readonly CatalogDbContext _db;
    private readonly ILogger<EditModel> _logger;

    public EditModel(CatalogDbContext db, ILogger<EditModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    [BindProperty]
    public CatalogItemInputModel Input { get; set; } = new();

    public SelectList Brands { get; private set; } = null!;

    public SelectList Types { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        _logger.LogInformation("Now loading... /Catalog/Edit?id={ProductId}", id);

        var product = await _db.CatalogItems.FirstOrDefaultAsync(ci => ci.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        Input = new CatalogItemInputModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            CatalogBrandId = product.CatalogBrandId,
            CatalogTypeId = product.CatalogTypeId,
            Price = product.Price,
            PictureFileName = product.PictureFileName,
            AvailableStock = product.AvailableStock,
            RestockThreshold = product.RestockThreshold,
            MaxStockThreshold = product.MaxStockThreshold,
        };

        await PopulateDropdownsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return Page();
        }

        var product = await _db.CatalogItems.FirstOrDefaultAsync(ci => ci.Id == Input.Id);
        if (product is null)
        {
            return NotFound();
        }

        product.Name = Input.Name;
        product.Description = Input.Description;
        product.CatalogBrandId = Input.CatalogBrandId;
        product.CatalogTypeId = Input.CatalogTypeId;
        product.Price = Input.Price;
        product.PictureFileName = Input.PictureFileName;
        product.AvailableStock = Input.AvailableStock;
        product.RestockThreshold = Input.RestockThreshold;
        product.MaxStockThreshold = Input.MaxStockThreshold;

        await _db.SaveChangesAsync();

        return RedirectToPage("Index");
    }

    private async Task PopulateDropdownsAsync()
    {
        var brands = await _db.CatalogBrands.OrderBy(b => b.Id).ToListAsync();
        var types = await _db.CatalogTypes.OrderBy(t => t.Id).ToListAsync();

        Brands = new SelectList(brands, nameof(CatalogBrand.Id), nameof(CatalogBrand.Brand), Input.CatalogBrandId);
        Types = new SelectList(types, nameof(CatalogType.Id), nameof(CatalogType.Type), Input.CatalogTypeId);
    }
}
