using Catalog.Domain;
using Catalog.Infrastructure;
using Catalog.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Web.Pages.Catalog;

/// <summary>
/// Create catalog item page, ported from the legacy Web Forms
/// <c>Catalog/Create.aspx</c> and its <c>Create_Click</c> code-behind.
/// </summary>
public class CreateModel : PageModel
{
    private readonly CatalogDbContext _db;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(CatalogDbContext db, ILogger<CreateModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    [BindProperty]
    public CatalogItemInputModel Input { get; set; } = new();

    public SelectList Brands { get; private set; } = null!;

    public SelectList Types { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        _logger.LogInformation("Now loading... /Catalog/Create");
        await PopulateDropdownsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return Page();
        }

        var catalogItem = new CatalogItem
        {
            Name = Input.Name,
            Description = Input.Description,
            CatalogBrandId = Input.CatalogBrandId,
            CatalogTypeId = Input.CatalogTypeId,
            Price = Input.Price,
            AvailableStock = Input.AvailableStock,
            RestockThreshold = Input.RestockThreshold,
            MaxStockThreshold = Input.MaxStockThreshold,
        };

        _db.CatalogItems.Add(catalogItem);
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
