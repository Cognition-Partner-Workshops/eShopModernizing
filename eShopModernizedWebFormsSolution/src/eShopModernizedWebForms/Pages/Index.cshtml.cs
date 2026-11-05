using eShopModernizedWebForms.Models;
using eShopModernizedWebForms.Services;
using eShopModernizedWebForms.ViewModel;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eShopModernizedWebForms.Pages;

public class IndexModel(ICatalogService catalog) : PageModel
{
    public PaginatedItemsViewModel<CatalogItem> Catalog { get; private set; }
    public void OnGet(int index = 0, int size = 10)
    {
        Catalog = catalog.GetCatalogItemsPaginated(size, index);
    }
}
