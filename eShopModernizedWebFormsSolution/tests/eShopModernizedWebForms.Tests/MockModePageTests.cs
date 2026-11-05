using System.Net;

namespace eShopModernizedWebForms.Tests
{
    public class MockModePageTests
    {
        [Theory]
        [InlineData("/", "Home Page")]
        [InlineData("/Default", "Home Page")]
        [InlineData("/Default.aspx", "Home Page")]
        [InlineData("/Default/index/1/size/5", "Home Page")]
        [InlineData("/Catalog/Create", "Create")]
        [InlineData("/Catalog/Create.aspx", "Create")]
        [InlineData("/Catalog/Edit/1", "Edit")]
        [InlineData("/Catalog/Details/1", "Details")]
        [InlineData("/Catalog/Delete/1", "Delete")]
        public async Task LegacyPublicUrlsRender(string url, string title)
        {
            using var factory = new WebFormsAppFactory();
            var html = await factory.CreateBrowser().GetStringAsync(new Uri(url, UriKind.Relative));
            Assert.Contains($"<title>{title} - Catalog manager (Web Forms)</title>", html, StringComparison.Ordinal);
            Assert.Contains("Catalog manager <span>(Web Forms)</span>", html, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("/Pics/1.png")]
        [InlineData("/Pics/dummy.png")]
        [InlineData("/images/brand.png")]
        [InlineData("/Content/site.css")]
        [InlineData("/Scripts/webforms-validation.js")]
        public async Task StaticAssetsAreServed(string url)
        {
            using var factory = new WebFormsAppFactory();
            var response = await factory.CreateBrowser().GetAsync(new Uri(url, UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DefaultPageShowsFirstTenOfTwelveWithPager()
        {
            using var factory = new WebFormsAppFactory();
            var html = await factory.CreateBrowser().GetStringAsync(new Uri("/", UriKind.Relative));
            Assert.Contains("Showing 10 of 12 products - Page 1 - 2", html, StringComparison.Ordinal);
            Assert.Contains("class=\"esh-pager-item esh-pager-item--navigable esh-pager-item--hidden\" href=\"/Default/index/-1/size/10\"", html, StringComparison.Ordinal);
            Assert.Contains("class=\"esh-pager-item esh-pager-item--navigable\" href=\"/Default/index/1/size/10\"", html, StringComparison.Ordinal);
            Assert.Equal(10, CountOccurrences(html, "class=\"esh-thumbnail\""));
        }

        [Fact]
        public async Task SecondPageShowsRemainingItems()
        {
            using var factory = new WebFormsAppFactory();
            var html = await factory.CreateBrowser().GetStringAsync(new Uri("/Default/index/1/size/10", UriKind.Relative));
            Assert.Contains("Showing 10 of 12 products - Page 2 - 2", html, StringComparison.Ordinal);
            Assert.Equal(2, CountOccurrences(html, "class=\"esh-thumbnail\""));
            Assert.Contains("/Catalog/Edit/12", html, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("/Catalog/Create")]
        [InlineData("/Catalog/Create.aspx")]
        public async Task CreateEditDeleteRoundTrip(string createUrl)
        {
            using var factory = new WebFormsAppFactory();
            var client = factory.CreateBrowser();

            FormPost.AssertRedirectToRoot(await FormPost.SubmitAsync(client, createUrl, FormPost.ValidItem("Parity Mug")));
            var details = await client.GetStringAsync(new Uri("/Catalog/Details/13", UriKind.Relative));
            Assert.Contains("<span>Parity Mug</span>", details, StringComparison.Ordinal);
            Assert.Contains("<span class=\"esh-price\">21.75</span>", details, StringComparison.Ordinal);
            Assert.Contains("src=\"/Pics/dummy.png\"", details, StringComparison.Ordinal);

            var edit = FormPost.ValidItem("Parity Mug v2");
            edit["PictureFileName"] = "dummy.png";
            FormPost.AssertRedirectToRoot(await FormPost.SubmitAsync(client, "/Catalog/Edit/13", edit));
            var list = await client.GetStringAsync(new Uri("/Default/index/1/size/10", UriKind.Relative));
            Assert.Contains("Parity Mug v2", list, StringComparison.Ordinal);
            Assert.Contains("Showing 10 of 13 products - Page 2 - 2", list, StringComparison.Ordinal);

            FormPost.AssertRedirectToRoot(await FormPost.SubmitAsync(client, "/Catalog/Delete/13", new Dictionary<string, string>()));
            var missing = await client.GetAsync(new Uri("/Catalog/Details/13", UriKind.Relative));
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }

        [Theory]
        [InlineData("Name", "", "The Name field is required.")]
        [InlineData("Price", "-1", "The Price must be a positive number with maximum two decimals between 0 and 1 million.")]
        [InlineData("Price", "1.234", "The Price must be a positive number with maximum two decimals between 0 and 1 million.")]
        [InlineData("Price", "1000000.01", "The Price must be a positive number with maximum two decimals between 0 and 1 million.")]
        [InlineData("Stock", "10000001", "The field Stock must be between 0 and 10 million.")]
        [InlineData("Restock", "abc", "The field Restock must be between 0 and 10 million.")]
        [InlineData("Maxstock", "-5", "The field Max stock must be between 0 and 10 million.")]
        public async Task InvalidCreateIsRejectedWithLegacyMessage(string field, string value, string message)
        {
            using var factory = new WebFormsAppFactory();
            var client = factory.CreateBrowser();
            var fields = FormPost.ValidItem("Invalid");
            fields[field] = value;

            var response = await FormPost.SubmitAsync(client, "/Catalog/Create", fields);
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains($"style=\"display:inline;\">{message}</span>", html, StringComparison.Ordinal);
            var list = await client.GetStringAsync(new Uri("/", UriKind.Relative));
            Assert.Contains("Showing 10 of 12 products", list, StringComparison.Ordinal);
        }

        [Fact]
        public async Task EditResetsOnReorderLikeLegacyFullUpdate()
        {
            using var factory = new WebFormsAppFactory();
            var service = (Services.ICatalogService)factory.Services.GetService(typeof(Services.ICatalogService));
            service.FindCatalogItem(2).OnReorder = true;
            var client = factory.CreateBrowser();
            var edit = FormPost.ValidItem("Mug");
            edit["PictureFileName"] = "2.png";

            FormPost.AssertRedirectToRoot(await FormPost.SubmitAsync(client, "/Catalog/Edit/2", edit));

            Assert.False(service.FindCatalogItem(2).OnReorder);
            Assert.Equal("2.png", service.FindCatalogItem(2).PictureFileName);
        }

        [Fact]
        public async Task PostWithoutAntiforgeryTokenIsRejected()
        {
            using var factory = new WebFormsAppFactory();
            using var content = new FormUrlEncodedContent(FormPost.ValidItem("x"));
            var response = await factory.CreateBrowser().PostAsync(new Uri("/Catalog/Create", UriKind.Relative), content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DetailsEncodesMarkupInNames()
        {
            using var factory = new WebFormsAppFactory();
            var html = await factory.CreateBrowser().GetStringAsync(new Uri("/Catalog/Details/9", UriKind.Relative));
            Assert.Contains("<span>Cup&lt;T&gt; White Mug</span>", html, StringComparison.Ordinal);
        }

        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            for (int i = text.IndexOf(value, StringComparison.Ordinal); i >= 0; i = text.IndexOf(value, i + value.Length, StringComparison.Ordinal))
            {
                count++;
            }

            return count;
        }
    }
}
