using System.Collections.Generic;
using eShopLegacyMVC.Models;

namespace eShopLegacyMVC.ViewModel
{
    /// <summary>
    /// Model of Views/Catalog/Index.cshtml: the paginated (already filtered and
    /// sorted) items plus the lookup lists and the echoed query used to re-render
    /// the filter bar.
    /// </summary>
    public class CatalogIndexViewModel
    {
        public PaginatedItemsViewModel<CatalogItem> Items { get; set; }

        public IEnumerable<CatalogBrand> Brands { get; set; }

        public IEnumerable<CatalogType> Types { get; set; }

        public CatalogQuery Query { get; set; }
    }
}
