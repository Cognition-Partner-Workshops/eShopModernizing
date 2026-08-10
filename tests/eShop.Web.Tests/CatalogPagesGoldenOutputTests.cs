using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace eShop.Web.Tests;

/// <summary>
/// Behavioral Baseline sections 6.1 and 6.2: the MVC front end is the parity oracle, so these
/// integration tests assert the golden status codes, pagination, CSRF enforcement and the
/// redirects to "/" against the in-process ASP.NET Core host.
/// </summary>
public class CatalogPagesGoldenOutputTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CatalogPagesGoldenOutputTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Theory]
    [InlineData("/")]
    [InlineData("/Catalog")]
    [InlineData("/Catalog/Index")]
    public async Task CatalogIndex_Returns200Html(string path)
    {
        var response = await CreateClient(_factory).GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("<title>Index - Catalog manager (MVC)</title>", html);
        Assert.Equal(10, CountCatalogRows(html));
        Assert.Contains("/items/1/pic", html);
    }

    [Fact]
    public async Task CatalogIndex_HonoursPageSizeAndPageIndex()
    {
        var html = await CreateClient(_factory).GetStringAsync("/Catalog/Index?pageSize=2&pageIndex=1");

        Assert.Equal(2, CountCatalogRows(html));
        Assert.Contains("Showing 2 of 12 products - Page 2 - 6", html);
        Assert.Contains("/items/3/pic", html);
        Assert.Contains("/items/4/pic", html);
    }

    [Theory]
    [InlineData("/Catalog/Details/1", HttpStatusCode.OK)]
    [InlineData("/Catalog/Details/999", HttpStatusCode.NotFound)]
    [InlineData("/Catalog/Details", HttpStatusCode.BadRequest)]
    [InlineData("/Catalog/Edit/1", HttpStatusCode.OK)]
    [InlineData("/Catalog/Edit/999", HttpStatusCode.NotFound)]
    [InlineData("/Catalog/Edit", HttpStatusCode.BadRequest)]
    [InlineData("/Catalog/Delete/1", HttpStatusCode.OK)]
    [InlineData("/Catalog/Delete/999", HttpStatusCode.NotFound)]
    [InlineData("/Catalog/Delete", HttpStatusCode.BadRequest)]
    [InlineData("/Catalog/Create", HttpStatusCode.OK)]
    public async Task Route_ReturnsTheGoldenStatusCode(string path, HttpStatusCode expected)
    {
        var response = await CreateClient(_factory).GetAsync(path);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task CreateForm_RendersTheAntiForgeryToken()
    {
        var html = await CreateClient(_factory).GetStringAsync("/Catalog/Create");

        Assert.Contains("<title>Create - Catalog manager (MVC)</title>", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task UnknownRoute_Returns404()
    {
        var response = await CreateClient(_factory).GetAsync("/nope");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePost_WithAntiForgeryToken_Redirects302ToRoot()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        var client = CreateClient(factory);
        var form = await AntiForgeryForm(client, "/Catalog/Create", new Dictionary<string, string>
        {
            ["Name"] = "Smoke Test Item",
            ["Description"] = "Smoke Test Item",
            ["Price"] = "1.50",
            ["PictureFileName"] = "dummy.png",
            ["CatalogTypeId"] = "1",
            ["CatalogBrandId"] = "1",
            ["AvailableStock"] = "1",
            ["RestockThreshold"] = "1",
            ["MaxStockThreshold"] = "1",
            ["OnReorder"] = "false",
        });

        var response = await client.PostAsync("/Catalog/Create", form);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
        Assert.Contains("Smoke Test Item", await client.GetStringAsync("/Catalog/Index?pageSize=20"));
    }

    /// <summary>
    /// The Create form is bound to a view model, so its inputs are named <c>Item.*</c>. Model
    /// binding falls back to the unprefixed legacy names, so both shapes post successfully.
    /// </summary>
    [Fact]
    public async Task CreatePost_UsingTheRenderedFormFieldNames_Redirects302ToRoot()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        var client = CreateClient(factory);
        var form = await AntiForgeryForm(client, "/Catalog/Create", new Dictionary<string, string>
        {
            ["Item.Name"] = "View Model Item",
            ["Item.Description"] = "View Model Item",
            ["Item.Price"] = "2.50",
            ["Item.PictureFileName"] = "dummy.png",
            ["Item.CatalogTypeId"] = "1",
            ["Item.CatalogBrandId"] = "1",
            ["Item.AvailableStock"] = "1",
            ["Item.RestockThreshold"] = "1",
            ["Item.MaxStockThreshold"] = "1",
            ["Item.OnReorder"] = "false",
        });

        var response = await client.PostAsync("/Catalog/Create", form);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
        Assert.Contains("View Model Item", await client.GetStringAsync("/Catalog/Index?pageSize=20"));
    }

    [Fact]
    public async Task CreatePost_WithoutAntiForgeryToken_IsRejected()
    {
        var response = await CreateClient(_factory).PostAsync(
            "/Catalog/Create",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["Name"] = "No token" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EditPost_WithAntiForgeryToken_Redirects302ToRoot()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        var client = CreateClient(factory);
        var form = await AntiForgeryForm(client, "/Catalog/Edit/1", new Dictionary<string, string>
        {
            ["Id"] = "1",
            ["Name"] = "Renamed Hoodie",
            ["Description"] = ".NET Bot Black Hoodie",
            ["Price"] = "19.50",
            ["PictureFileName"] = "1.png",
            ["CatalogTypeId"] = "2",
            ["CatalogBrandId"] = "2",
            ["AvailableStock"] = "100",
            ["RestockThreshold"] = "0",
            ["MaxStockThreshold"] = "0",
            ["OnReorder"] = "false",
        });

        var response = await client.PostAsync("/Catalog/Edit", form);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
        Assert.Contains("Renamed Hoodie", await client.GetStringAsync("/Catalog/Details/1"));
    }

    [Fact]
    public async Task EditPost_WithoutAntiForgeryToken_IsRejected()
    {
        var response = await CreateClient(_factory).PostAsync(
            "/Catalog/Edit",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["Id"] = "1", ["Name"] = "No token" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeletePost_WithAntiForgeryToken_Redirects302ToRootAndRemovesTheItem()
    {
        using var factory = _factory.WithWebHostBuilder(_ => { });
        var client = CreateClient(factory);
        var form = await AntiForgeryForm(client, "/Catalog/Delete/1", new Dictionary<string, string>());

        var response = await client.PostAsync("/Catalog/Delete/1", form);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Catalog/Details/1")).StatusCode);
    }

    [Fact]
    public async Task DeletePost_WithoutAntiForgeryToken_IsRejected()
    {
        var response = await CreateClient(_factory).PostAsync(
            "/Catalog/Delete/1",
            new FormUrlEncodedContent(new Dictionary<string, string>()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Fetches a form page and returns its fields plus the page's anti-forgery token.</summary>
    private static async Task<FormUrlEncodedContent> AntiForgeryForm(
        HttpClient client,
        string formPath,
        Dictionary<string, string> fields)
    {
        var html = await client.GetStringAsync(formPath);
        var token = Regex.Match(
            html,
            """name="__RequestVerificationToken"[^>]*value="([^"]+)""").Groups[1].Value;

        Assert.NotEmpty(token);
        fields["__RequestVerificationToken"] = token;

        return new FormUrlEncodedContent(fields);
    }

    private static int CountCatalogRows(string html) =>
        Regex.Matches(html, "esh-thumbnail").Count;
}
