using System.Net;

namespace eShop.Web.Tests.Integration;

/// <summary>
/// The two redirects the Web Forms retirement record (<c>docs/webforms-retirement.md</c> §4.3)
/// left open for the parity gate. The <c>Default.aspx</c> pager linked these URLs on every catalog
/// page, so they are the only Web Forms URL shape carried forward.
/// </summary>
public class WebFormsPagerRedirectTests : IClassFixture<CatalogWebFactory>
{
    private readonly CatalogWebFactory _factory;

    public WebFormsPagerRedirectTests(CatalogWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Paginated_web_forms_route_redirects_permanently_to_the_mvc_query_string()
    {
        using var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync(new Uri("/Default/index/1/size/2", UriKind.Relative));

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/Catalog/Index?pageIndex=1&pageSize=2", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Web_forms_default_page_redirects_permanently_to_the_site_root()
    {
        using var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync(new Uri("/Default", UriKind.Relative));

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
    }

    /// <summary>Non-numeric segments must not match; they fall through to the normal 404.</summary>
    [Fact]
    public async Task Non_numeric_pager_segments_are_not_matched()
    {
        using var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync(new Uri("/Default/index/abc/size/xyz", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Following the redirect lands on the second page of two items.</summary>
    [Fact]
    public async Task Following_the_pager_redirect_renders_the_requested_page()
    {
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync(new Uri("/Default/index/1/size/2", UriKind.Relative));

        Assert.Contains("Showing 2 of 12 products - Page 2 - 6", html, StringComparison.Ordinal);
    }
}
