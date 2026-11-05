using eShopModernizedWebForms.Models.Infrastructure;
using eShopModernizedWebForms.Pages.Catalog;

namespace eShopModernizedWebForms.Tests
{
    public class CatalogItemFormTests
    {
        [Theory]
        [InlineData("0", true)]
        [InlineData("12", true)]
        [InlineData("12.5", true)]
        [InlineData(" 12.50 ", true)]
        [InlineData("1,000.00", true)]
        [InlineData("1000000", true)]
        [InlineData("1000000.01", false)]
        [InlineData("12.", false)]
        [InlineData("12.345", false)]
        [InlineData("-0.01", false)]
        [InlineData("abc", false)]
        [InlineData("", false)]
        public void PriceFollowsCurrencyRangeValidator(string price, bool valid)
        {
            var form = new CatalogItemForm { Name = "n", Price = price };
            Assert.Equal(valid, form.Validate());
        }

        [Theory]
        [InlineData("0", true)]
        [InlineData("+5", true)]
        [InlineData("10000000", true)]
        [InlineData("10000001", false)]
        [InlineData("1.5", false)]
        [InlineData("-1", false)]
        public void StockFollowsIntegerRangeValidator(string stock, bool valid)
        {
            var form = new CatalogItemForm { Name = "n", Stock = stock };
            Assert.Equal(valid, form.Validate());
        }

        [Fact]
        public void WhitespaceNameFailsRequiredValidator()
        {
            var form = new CatalogItemForm { Name = "   " };
            Assert.False(form.Validate());
            Assert.Equal(CatalogItemForm.NameRequiredMessage, form.Errors["Name"]);
        }

        [Fact]
        public void CanonicalCsvFixtureParsesWithLegacyRules()
        {
            var setup = Path.Combine(AppContext.BaseDirectory, "Setup");
            var headers = CatalogDBInitializer.GetHeaders(
                Path.Combine(setup, "CatalogItems.csv"),
                new[] { "catalogtypename", "catalogbrandname", "description", "name", "price", "pictureFileName" },
                new[] { "availablestock", "restockthreshold", "maxstockthreshold", "onreorder" });
            var types = File.ReadAllLines(Path.Combine(setup, "CatalogTypes.csv")).Skip(1).Select((t, i) => (t.Trim('"').Trim(), i + 1)).ToDictionary(x => x.Item1, x => x.Item2);
            var brands = File.ReadAllLines(Path.Combine(setup, "CatalogBrands.csv")).Skip(1).Select((b, i) => (b.Trim('"').Trim(), i + 1)).ToDictionary(x => x.Item1, x => x.Item2);
            var rows = File.ReadAllLines(Path.Combine(setup, "CatalogItems.csv")).Skip(1)
                .Select(r => System.Text.RegularExpressions.Regex.Split(r, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"))
                .Select(c => CatalogDBInitializer.CreateCatalogItem(c, headers, types, brands))
                .ToList();

            Assert.NotEmpty(rows);
            Assert.All(rows, r => Assert.False(string.IsNullOrEmpty(r.Name)));
        }
    }
}
