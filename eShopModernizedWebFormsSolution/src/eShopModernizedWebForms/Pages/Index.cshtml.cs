using System.Globalization;
using eShopModernizedWebForms.Models;
using eShopModernizedWebForms.Services;
using eShopModernizedWebForms.ViewModel;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eShopModernizedWebForms.Pages
{
    public class IndexModel : PageModel
    {
        public const int DefaultPageIndex = 0;
        public const int DefaultPageSize = 10;

        private readonly ICatalogService catalogService;
        private readonly ILogger<IndexModel> logger;

        public IndexModel(ICatalogService catalogService, ILogger<IndexModel> logger)
        {
            this.catalogService = catalogService;
            this.logger = logger;
        }

        public PaginatedItemsViewModel<CatalogItem> Catalog { get; private set; }

        public void OnGet()
        {
            int size = DefaultPageSize;
            int index = DefaultPageIndex;
            if (RouteData.Values.ContainsKey("size") && RouteData.Values.ContainsKey("index"))
            {
                // Same conversion as the legacy page: non-numeric values fail the request.
                size = Convert.ToInt32(RouteData.Values["size"], CultureInfo.InvariantCulture);
                index = Convert.ToInt32(RouteData.Values["index"], CultureInfo.InvariantCulture);
            }

            Catalog = catalogService.GetCatalogItemsPaginated(size, index);
            logger.LogInformation("Now loading... /Default.aspx?size={Size}&index={Index}", size, index);
        }

        public string PageUrl(int index) =>
            string.Create(CultureInfo.InvariantCulture, $"/Default/index/{index}/size/{Catalog.ItemsPerPage}");

        public string NextCssClass =>
            "esh-pager-item esh-pager-item--navigable" + (Catalog.ActualPage < Catalog.TotalPages - 1 ? "" : " esh-pager-item--hidden");

        public string PreviousCssClass =>
            "esh-pager-item esh-pager-item--navigable" + (Catalog.ActualPage > 0 ? "" : " esh-pager-item--hidden");
    }
}
