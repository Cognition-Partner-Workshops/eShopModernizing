using System;

namespace eShopLegacyMVC.ViewModel
{
    /// <summary>
    /// Supported values of <see cref="CatalogQuery.Sort"/>.
    /// </summary>
    public static class CatalogSortOptions
    {
        public const string NameAsc = "name-asc";
        public const string NameDesc = "name-desc";
        public const string PriceAsc = "price-asc";
        public const string PriceDesc = "price-desc";

        /// <summary>
        /// Returns the supplied value when it is a supported sort option,
        /// otherwise <see cref="NameAsc"/>.
        /// </summary>
        public static string Normalize(string sort)
        {
            if (string.IsNullOrWhiteSpace(sort))
            {
                return NameAsc;
            }

            var candidate = sort.Trim();
            if (string.Equals(candidate, NameDesc, StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidate, PriceAsc, StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidate, PriceDesc, StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidate, NameAsc, StringComparison.OrdinalIgnoreCase))
            {
                return candidate.ToLowerInvariant();
            }

            return NameAsc;
        }
    }
}
