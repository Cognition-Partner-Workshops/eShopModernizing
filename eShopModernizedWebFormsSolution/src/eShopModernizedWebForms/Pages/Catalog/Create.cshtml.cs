using eShopModernizedWebForms.Models;
using eShopModernizedWebForms.Services;
using Microsoft.AspNetCore.Mvc;

namespace eShopModernizedWebForms.Pages.Catalog;

public class CreateModel(ICatalogService catalog) : CatalogFormModel(catalog)
{
    public void OnGet() { }
    public IActionResult OnPost()
    {
        if (!ValidateForm()) return Page();
        Item.Id = 0;
        Item.PictureFileName = CatalogItem.DefaultPictureName;
        CatalogService.CreateCatalogItem(Item);
        return Redirect("/");
    }
}
