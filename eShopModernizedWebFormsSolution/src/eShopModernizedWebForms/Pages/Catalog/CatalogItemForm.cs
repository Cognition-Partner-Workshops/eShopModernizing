using System.Globalization;
using System.Text.RegularExpressions;
using eShopModernizedWebForms.Models;

namespace eShopModernizedWebForms.Pages.Catalog
{
    /// <summary>
    /// Posted Create/Edit form values plus the server-side equivalent of the legacy
    /// RequiredFieldValidator / RangeValidator (Currency, Integer) controls.
    /// </summary>
    public class CatalogItemForm
    {
        public const string NameRequiredMessage = "The Name field is required.";
        public const string PriceRangeMessage = "The Price must be a positive number with maximum two decimals between 0 and 1 million.";
        public const string StockRangeMessage = "The field Stock must be between 0 and 10 million.";
        public const string RestockRangeMessage = "The field Restock must be between 0 and 10 million.";
        public const string MaxstockRangeMessage = "The field Max stock must be between 0 and 10 million.";

        private static readonly Regex CurrencyFormat = new(@"^\s*([-\+])?(((\d+),)*)(\d+)(\.(\d{1,2}))?\s*$", RegexOptions.CultureInvariant);
        private static readonly Regex IntegerFormat = new(@"^\s*([-\+])?(\d+)\s*$", RegexOptions.CultureInvariant);

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Brand { get; set; }

        public string Type { get; set; }

        public string Price { get; set; } = "0.00";

        public string PictureFileName { get; set; } = CatalogItem.DefaultPictureName;

        public string Stock { get; set; } = "0";

        public string Restock { get; set; } = "0";

        public string Maxstock { get; set; } = "0";

        public Dictionary<string, string> Errors { get; } = new();

        public static CatalogItemForm FromItem(CatalogItem item) => new()
        {
            Name = item.Name,
            Description = item.Description,
            Brand = item.CatalogBrandId.ToString(CultureInfo.InvariantCulture),
            Type = item.CatalogTypeId.ToString(CultureInfo.InvariantCulture),
            Price = item.Price.ToString(CultureInfo.CurrentCulture),
            PictureFileName = item.PictureFileName,
            Stock = item.AvailableStock.ToString(CultureInfo.CurrentCulture),
            Restock = item.RestockThreshold.ToString(CultureInfo.CurrentCulture),
            Maxstock = item.MaxStockThreshold.ToString(CultureInfo.CurrentCulture),
        };

        public bool Validate()
        {
            Errors.Clear();
            if (string.IsNullOrWhiteSpace(Name))
            {
                Errors[nameof(Name)] = NameRequiredMessage;
            }

            if (ParseCurrency(Price) is not decimal price || price < 0 || price > 1000000)
            {
                Errors[nameof(Price)] = PriceRangeMessage;
            }

            ValidateInteger(Stock, nameof(Stock), StockRangeMessage);
            ValidateInteger(Restock, nameof(Restock), RestockRangeMessage);
            ValidateInteger(Maxstock, nameof(Maxstock), MaxstockRangeMessage);
            return Errors.Count == 0;
        }

        public CatalogItem ToCatalogItem() => new()
        {
            Name = Name,
            Description = Description ?? string.Empty,
            CatalogBrandId = int.Parse(Brand, CultureInfo.InvariantCulture),
            CatalogTypeId = int.Parse(Type, CultureInfo.InvariantCulture),
            Price = ParseCurrency(Price).GetValueOrDefault(),
            AvailableStock = ParseInteger(Stock).GetValueOrDefault(),
            RestockThreshold = ParseInteger(Restock).GetValueOrDefault(),
            MaxStockThreshold = ParseInteger(Maxstock).GetValueOrDefault(),
        };

        public string ValidatorStyle(string field) => Errors.ContainsKey(field) ? "display:inline;" : "display:none;";

        internal static decimal? ParseCurrency(string value)
        {
            if (value == null || !CurrencyFormat.IsMatch(value))
            {
                return null;
            }

            return decimal.Parse(value.Replace(",", string.Empty, StringComparison.Ordinal).Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        }

        internal static int? ParseInteger(string value)
        {
            if (value == null || !IntegerFormat.IsMatch(value) || !int.TryParse(value.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int result))
            {
                return null;
            }

            return result;
        }

        private void ValidateInteger(string value, string field, string message)
        {
            if (ParseInteger(value) is not int number || number < 0 || number > 10000000)
            {
                Errors[field] = message;
            }
        }
    }
}
