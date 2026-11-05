using eShopModernizedWebForms.Models;
using eShopModernizedWebForms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eShopModernizedWebForms.Pages.Catalog
{
    public class DeleteModel : PageModel
    {
        private readonly ICatalogService catalogService;
        private readonly ILogger<DeleteModel> logger;

        public DeleteModel(ICatalogService catalogService, ILogger<DeleteModel> logger)
        {
            this.catalogService = catalogService;
            this.logger = logger;
        }

        public CatalogItem ProductToDelete { get; private set; }

        public IActionResult OnGet(int id)
        {
            logger.LogInformation("Now loading... /Catalog/Delete.aspx?id={Id}", id);
            ProductToDelete = catalogService.FindCatalogItem(id);
            return ProductToDelete == null ? NotFound() : Page();
        }

        public IActionResult OnPost(int id)
        {
            ProductToDelete = catalogService.FindCatalogItem(id);
            if (ProductToDelete == null)
            {
                return NotFound();
            }

            catalogService.RemoveCatalogItem(ProductToDelete);
            return Redirect("~/");
        }
    }
}
