using eShopModernizedWebForms.Models;
using eShopModernizedWebForms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eShopModernizedWebForms.Pages.Catalog
{
    public class CreateModel : PageModel
    {
        private readonly ICatalogService catalogService;
        private readonly ILogger<CreateModel> logger;

        public CreateModel(ICatalogService catalogService, ILogger<CreateModel> logger)
        {
            this.catalogService = catalogService;
            this.logger = logger;
        }

        public CatalogItemForm Form { get; private set; } = new();

        public IEnumerable<CatalogBrand> Brands { get; private set; }

        public IEnumerable<CatalogType> Types { get; private set; }

        public void OnGet()
        {
            logger.LogInformation("Now loading... /Catalog/Create.aspx");
            LoadLists();
        }

        public IActionResult OnPost(CatalogItemForm form)
        {
            Form = form;
            if (!Form.Validate())
            {
                LoadLists();
                return Page();
            }

            catalogService.CreateCatalogItem(Form.ToCatalogItem());
            return Redirect("~/");
        }

        private void LoadLists()
        {
            Brands = catalogService.GetCatalogBrands().ToList();
            Types = catalogService.GetCatalogTypes().ToList();
        }
    }
}
