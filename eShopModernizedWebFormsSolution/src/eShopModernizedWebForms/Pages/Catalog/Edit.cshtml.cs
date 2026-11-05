using eShopModernizedWebForms.Services;
using Microsoft.AspNetCore.Mvc;

namespace eShopModernizedWebForms.Pages.Catalog;

public class EditModel : CatalogFormModel
{
    public EditModel(ICatalogService catalog) : base(catalog) { IsEdit = true; }
    public IActionResult OnGet(int id)
    {
        Item = CatalogService.FindCatalogItem(id);
        return Item == null ? NotFound() : Page();
    }
    public IActionResult OnPost(int id)
    {
        var original = CatalogService.FindCatalogItem(id);
        if (original == null) return NotFound();
        Item.Id = id;
        Item.PictureFileName = original.PictureFileName;
        if (!ValidateForm()) return Page();
        CatalogService.UpdateCatalogItem(Item);
        return Redirect("/");
    }
}
