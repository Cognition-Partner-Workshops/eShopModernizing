using System.Net;
using System.Text.RegularExpressions;

namespace eShop.Web.Tests.Integration;

/// <summary>
/// Every MVC row of the Behavioral Baseline section 6.1 table, asserted against the ported UI.
/// Byte counts are deliberately not asserted: the legacy layout rendered InProc session values
/// (<c>MachineName</c>, <c>SessionStartTime</c>) in the footer of every page and session state is
/// dropped (accepted delta C-09), so the golden sizes cannot be reproduced. The structure and the
/// semantics — title, row count, image link shape, anti-forgery field, links — are asserted instead.
/// </summary>
public class CatalogGoldenRouteTests : IClassFixture<CatalogWebFactory>
{
    private readonly CatalogWebFactory _factory;

    public CatalogGoldenRouteTests(CatalogWebFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/Catalog")]
    [InlineData("/Catalog/Index")]
    public async Task Catalog_list_renders_the_first_page_of_ten_items(string url)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri(url, UriKind.Relative));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<title>Index - Catalog manager (MVC)</title>", html, StringComparison.Ordinal);
        Assert.Equal(10, CountRows(html));
        Assert.Equal(10, Regex.Matches(html, @"items/\d+/pic", RegexOptions.None, TimeSpan.FromSeconds(5)).Count);
    }

    /// <summary>The legacy default route also served the catalog list from the site root.</summary>
    [Fact]
    public async Task Site_root_renders_the_same_page_as_the_catalog_list()
    {
        using var client = _factory.CreateClient();

        var root = await client.GetStringAsync(new Uri("/", UriKind.Relative));
        var catalog = await client.GetStringAsync(new Uri("/Catalog", UriKind.Relative));

        Assert.Equal(catalog, root);
    }

    [Fact]
    public async Task Catalog_list_honours_pageSize_and_pageIndex()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/Catalog/Index?pageSize=2&pageIndex=1", UriKind.Relative));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, CountRows(html));
        Assert.Contains("Showing 2 of 12 products - Page 2 - 6", html, StringComparison.Ordinal);
        Assert.Contains("Prism White T-Shirt", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Details_of_a_known_item_renders_the_detail_view()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/Catalog/Details/1", UriKind.Relative));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>Details - Catalog manager (MVC)</title>", html, StringComparison.Ordinal);
        Assert.Contains(".NET Bot Black Hoodie", html, StringComparison.Ordinal);
        Assert.Contains("items/1/pic", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Details_of_an_unknown_item_returns_404()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/Catalog/Details/999", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Details_without_an_id_returns_400()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/Catalog/Details", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_form_is_rendered_with_the_anti_forgery_field_and_the_option_lists()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/Catalog/Create", UriKind.Relative));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>Create - Catalog manager (MVC)</title>", html, StringComparison.Ordinal);
        Assert.Contains(@"name=""__RequestVerificationToken"" type=""hidden""", html, StringComparison.Ordinal);
        Assert.Contains(@"<select ", html, StringComparison.Ordinal);
        Assert.Contains(@"name=""CatalogBrandId""", html, StringComparison.Ordinal);
        Assert.Contains(@"name=""CatalogTypeId""", html, StringComparison.Ordinal);
        Assert.Contains(@">Azure</option>", html, StringComparison.Ordinal);
        Assert.Contains(@">Mug</option>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Edit_form_is_prefilled_and_carries_the_anti_forgery_field()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/Catalog/Edit/1", UriKind.Relative));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>Edit - Catalog manager (MVC)</title>", html, StringComparison.Ordinal);
        Assert.Contains(@"name=""__RequestVerificationToken"" type=""hidden""", html, StringComparison.Ordinal);
        Assert.Contains(@"value="".NET Bot Black Hoodie""", html, StringComparison.Ordinal);
        Assert.Contains(@"<option selected=""selected"" value=""2"">.NET</option>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Delete_confirmation_is_rendered()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/Catalog/Delete/1", UriKind.Relative));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>Delete - Catalog manager (MVC)</title>", html, StringComparison.Ordinal);
        Assert.Contains("Are you sure you want to delete this?", html, StringComparison.Ordinal);
        Assert.Contains(@"name=""__RequestVerificationToken"" type=""hidden""", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/Catalog/Edit")]
    [InlineData("/Catalog/Delete")]
    public async Task Edit_and_delete_without_an_id_return_400(string url)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri(url, UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/Catalog/Edit/999")]
    [InlineData("/Catalog/Delete/999")]
    public async Task Edit_and_delete_of_an_unknown_item_return_404(string url)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri(url, UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// The picture route is served by the UI itself, so the thumbnails render with only
    /// <c>eShop.Web</c> running, exactly as the legacy single-process application did.
    /// </summary>
    [Fact]
    public async Task Picture_route_serves_the_item_image()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/items/1/pic", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.NotEmpty(await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Picture_route_rejects_a_non_positive_id()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/items/0/pic", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_route_returns_404()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/nope", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Accepted delta: the golden 404 for <c>/Content/site.css</c> was an artefact of the legacy
    /// harness (IIS served the files). Bundling is replaced by plain static assets, which the
    /// Kestrel static file middleware serves on every platform.
    /// </summary>
    [Theory]
    [InlineData("/css/site.css", "text/css")]
    [InlineData("/images/brand.png", "image/png")]
    public async Task Static_assets_are_served_from_wwwroot(string url, string contentType)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri(url, UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(contentType, response.Content.Headers.ContentType?.MediaType);
    }

    private static int CountRows(string html) =>
        Regex.Matches(html, @"class=""esh-thumbnail""", RegexOptions.None, TimeSpan.FromSeconds(5)).Count;
}
