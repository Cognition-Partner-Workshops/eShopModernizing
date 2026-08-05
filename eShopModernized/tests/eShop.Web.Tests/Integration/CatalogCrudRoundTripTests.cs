using System.Net;
using System.Text.RegularExpressions;

namespace eShop.Web.Tests.Integration;

/// <summary>
/// Behavioral Baseline section 6.2: the write path. Each test owns its factory because the mock
/// catalog service is a singleton and these tests mutate it.
/// </summary>
public class CatalogCrudRoundTripTests
{
    [Fact]
    public async Task Create_redirects_to_the_list_and_the_item_is_visible_afterwards()
    {
        using var factory = new CatalogWebFactory();
        using var client = factory.CreateNonRedirectingClient();

        var token = await GetAntiForgeryTokenAsync(client, "/Catalog/Create");

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "Smoke Test Item",
            ["Description"] = "Smoke test",
            ["Price"] = "1.50",
            ["CatalogBrandId"] = "1",
            ["CatalogTypeId"] = "1",
            ["AvailableStock"] = "1",
            ["RestockThreshold"] = "0",
            ["MaxStockThreshold"] = "2",
        });

        var response = await client.PostAsync(new Uri("/Catalog/Create", UriKind.Relative), content);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());

        var list = await client.GetStringAsync(new Uri("/Catalog/Index?pageSize=20", UriKind.Relative));
        Assert.Contains("Smoke Test Item", list, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_without_the_anti_forgery_token_is_rejected()
    {
        using var factory = new CatalogWebFactory();
        using var client = factory.CreateNonRedirectingClient();

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Name"] = "No Token Item",
            ["Price"] = "1.00",
            ["CatalogBrandId"] = "1",
            ["CatalogTypeId"] = "1",
        });

        var response = await client.PostAsync(new Uri("/Catalog/Create", UriKind.Relative), content);

        // The legacy app answered with a 500 raised by HttpAntiForgeryException; ASP.NET Core
        // rejects the request with a 400 instead. Either way the write is refused.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var list = await client.GetStringAsync(new Uri("/Catalog/Index?pageSize=20", UriKind.Relative));
        Assert.DoesNotContain("No Token Item", list, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Edit_redirects_to_the_list_and_the_change_is_visible_afterwards()
    {
        using var factory = new CatalogWebFactory();
        using var client = factory.CreateNonRedirectingClient();

        var token = await GetAntiForgeryTokenAsync(client, "/Catalog/Edit/1");

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = "1",
            ["Name"] = "Renamed Hoodie",
            ["Description"] = ".NET Bot Black Hoodie",
            ["Price"] = "19.50",
            ["PictureFileName"] = "1.png",
            ["CatalogBrandId"] = "2",
            ["CatalogTypeId"] = "2",
            ["AvailableStock"] = "100",
            ["RestockThreshold"] = "0",
            ["MaxStockThreshold"] = "0",
        });

        var response = await client.PostAsync(new Uri("/Catalog/Edit", UriKind.Relative), content);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());

        var details = await client.GetStringAsync(new Uri("/Catalog/Details/1", UriKind.Relative));
        Assert.Contains("Renamed Hoodie", details, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Delete_redirects_to_the_list_and_the_item_is_gone_afterwards()
    {
        using var factory = new CatalogWebFactory();
        using var client = factory.CreateNonRedirectingClient();

        var token = await GetAntiForgeryTokenAsync(client, "/Catalog/Delete/1");

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["id"] = "1",
        });

        var response = await client.PostAsync(new Uri("/Catalog/Delete/1", UriKind.Relative), content);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());

        var details = await client.GetAsync(new Uri("/Catalog/Details/1", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, details.StatusCode);
    }

    [Fact]
    public async Task Create_with_an_invalid_model_redisplays_the_form_and_writes_nothing()
    {
        using var factory = new CatalogWebFactory();
        using var client = factory.CreateNonRedirectingClient();

        var token = await GetAntiForgeryTokenAsync(client, "/Catalog/Create");

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = string.Empty,
            ["Price"] = "1.00",
            ["CatalogBrandId"] = "1",
            ["CatalogTypeId"] = "1",
        });

        var response = await client.PostAsync(new Uri("/Catalog/Create", UriKind.Relative), content);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>Create - Catalog manager (MVC)</title>", html, StringComparison.Ordinal);
        Assert.Contains(">Azure</option>", html, StringComparison.Ordinal);

        var list = await client.GetStringAsync(new Uri("/Catalog/Index?pageSize=20", UriKind.Relative));
        Assert.Equal(12, Regex.Matches(list, @"class=""esh-thumbnail""", RegexOptions.None, TimeSpan.FromSeconds(5)).Count);
    }

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(new Uri(url, UriKind.Relative));

        var match = Regex.Match(
            html,
            @"name=""__RequestVerificationToken"" type=""hidden"" value=""(?<token>[^""]+)""",
            RegexOptions.None,
            TimeSpan.FromSeconds(5));

        Assert.True(match.Success, FormattableString.Invariant($"No anti-forgery token rendered by {url}."));

        return match.Groups["token"].Value;
    }
}
