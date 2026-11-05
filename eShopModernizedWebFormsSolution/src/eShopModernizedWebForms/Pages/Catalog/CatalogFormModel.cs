using eShopModernizedWebForms.Models;
using eShopModernizedWebForms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace eShopModernizedWebForms.Pages.Catalog;

public abstract class CatalogFormModel(ICatalogService catalog) : PageModel
{
    protected ICatalogService CatalogService { get; } = catalog;
    [BindProperty]
    public CatalogItem Item { get; set; } = new() { CatalogBrandId = 1, CatalogTypeId = 1 };
    public bool IsEdit { get; protected set; }
    public IEnumerable<SelectListItem> Brands => CatalogService.GetCatalogBrands().Select(b => new SelectListItem(b.Brand, b.Id.ToString()));
    public IEnumerable<SelectListItem> Types => CatalogService.GetCatalogTypes().Select(t => new SelectListItem(t.Type, t.Id.ToString()));

    protected bool ValidateForm()
    {
        if (!CatalogService.GetCatalogBrands().Any(b => b.Id == Item.CatalogBrandId))
        {
            ModelState.AddModelError("Item.CatalogBrandId", "Select a valid brand.");
        }
        if (!CatalogService.GetCatalogTypes().Any(t => t.Id == Item.CatalogTypeId))
        {
            ModelState.AddModelError("Item.CatalogTypeId", "Select a valid type.");
        }
        if (string.IsNullOrWhiteSpace(Item.Name))
        {
            ModelState.AddModelError("Item.Name", "The Name field is required.");
        }
        if (Item.Price > 1000000)
        {
            ModelState.AddModelError("Item.Price", "The Price must be a positive number with maximum two decimals between 0 and 1 million.");
        }
        return ModelState.IsValid;
    }
}
