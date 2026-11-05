using eShopModernizedWebForms.Models;
using eShopModernizedWebForms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eShopModernizedWebForms.Pages.Catalog
{
    public class EditModel : PageModel
    {
        private readonly ICatalogService catalogService;
        private readonly ILogger<EditModel> logger;

        public EditModel(ICatalogService catalogService, ILogger<EditModel> logger)
        {
            this.catalogService = catalogService;
            this.logger = logger;
        }

        public CatalogItemForm Form { get; private set; }

        public IEnumerable<CatalogBrand> Brands { get; private set; }

        public IEnumerable<CatalogType> Types { get; private set; }

        public IActionResult OnGet(int id)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Now loading... /Catalog/Edit.aspx?id={Id}", id);
            }
            var product = catalogService.FindCatalogItem(id);
            if (product == null)
            {
                return NotFound();
            }

            Form = CatalogItemForm.FromItem(product);
            LoadLists();
            return Page();
        }

        public IActionResult OnPost(int id, CatalogItemForm form)
        {
            Form = form;
            if (!Form.Validate())
            {
                LoadLists();
                return Page();
            }

            // Like the legacy Save_Click, the item is rebuilt from the posted fields only
            // (OnReorder is not on the form) and written as a full update.
            var catalogItem = Form.ToCatalogItem();
            catalogItem.Id = id;
            catalogItem.PictureFileName = Form.PictureFileName;
            catalogService.UpdateCatalogItem(catalogItem);
            return Redirect("~/");
        }

        private void LoadLists()
        {
            Brands = catalogService.GetCatalogBrands().ToList();
            Types = catalogService.GetCatalogTypes().ToList();
        }
    }
}
