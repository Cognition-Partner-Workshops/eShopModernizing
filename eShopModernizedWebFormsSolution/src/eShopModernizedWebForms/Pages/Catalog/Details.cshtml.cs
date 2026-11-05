using eShopModernizedWebForms.Models;
using eShopModernizedWebForms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eShopModernizedWebForms.Pages.Catalog;

public class DetailsModel(ICatalogService catalog) : PageModel
{
    public CatalogItem Item { get; private set; }
    public IActionResult OnGet(int id)
    {
        Item = catalog.FindCatalogItem(id);
        if (Item == null) return NotFound();
        Item.CatalogBrand = catalog.GetCatalogBrands().First(b => b.Id == Item.CatalogBrandId);
        Item.CatalogType = catalog.GetCatalogTypes().First(t => t.Id == Item.CatalogTypeId);
        return Page();
    }
}
