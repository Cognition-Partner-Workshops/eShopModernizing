namespace eShopLegacyMVC.ViewModel
{
    /// <summary>
    /// Free-text search, brand/type filter, sort and paging values bound from the
    /// query string of GET /Catalog/Index.
    /// </summary>
    public class CatalogQuery
    {
        public const int DefaultPageSize = 10;

        public string Search { get; set; }

        public int? BrandId { get; set; }

        public int? TypeId { get; set; }

        /// <summary>
        /// One of the <see cref="CatalogSortOptions"/> values. Any unknown or missing
        /// value is treated as <see cref="CatalogSortOptions.NameAsc"/>.
        /// </summary>
        public string Sort { get; set; }

        public int PageSize { get; set; }

        public int PageIndex { get; set; }

        public CatalogQuery()
        {
            Sort = CatalogSortOptions.NameAsc;
            PageSize = DefaultPageSize;
            PageIndex = 0;
        }

        /// <summary>
        /// True when no search text, brand or type narrows the result set.
        /// </summary>
        public bool HasFilters
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Search) || BrandId.HasValue || TypeId.HasValue;
            }
        }
    }
}
