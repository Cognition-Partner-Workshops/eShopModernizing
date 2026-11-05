using eShopModernizedWebForms.Models;
using eShopModernizedWebForms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eShopModernizedWebForms.Pages.Catalog
{
    public class DetailsModel : PageModel
    {
        private readonly ICatalogService catalogService;
        private readonly ILogger<DetailsModel> logger;

        public DetailsModel(ICatalogService catalogService, ILogger<DetailsModel> logger)
        {
            this.catalogService = catalogService;
            this.logger = logger;
        }

        public CatalogItem Product { get; private set; }

        public IActionResult OnGet(int id)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Now loading... /Catalog/Details.aspx?id={Id}", id);
            }
            Product = catalogService.FindCatalogItem(id);
            return Product == null ? NotFound() : Page();
        }
    }
}
