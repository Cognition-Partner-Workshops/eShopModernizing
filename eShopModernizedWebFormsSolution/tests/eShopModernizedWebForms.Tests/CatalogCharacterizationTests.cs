using System.Net;
using System.Text.RegularExpressions;
using eShopModernizedWebForms.Models;
using eShopModernizedWebForms.Models.Infrastructure;
using eShopModernizedWebForms.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace eShopModernizedWebForms.Tests;

public class CatalogCharacterizationTests
{
    private static readonly string[] Names =
    [
        ".NET Bot Black Hoodie", ".NET Black & White Mug", "Prism White T-Shirt",
        ".NET Foundation T-shirt", "Roslyn Red Sheet", ".NET Blue Hoodie",
        "Roslyn Red T-Shirt", "Kudu Purple Hoodie", "Cup<T> White Mug",
        ".NET Foundation Sheet", "Cup<T> Sheet", "Prism White TShirt"
    ];
    private static readonly decimal[] Prices = [19.5m, 8.50m, 12m, 12m, 8.5m, 12m, 12m, 8.5m, 12m, 12m, 8.5m, 12m];
    private static readonly int[] Brands = [2, 2, 5, 2, 5, 2, 5, 5, 5, 2, 2, 5];
    private static readonly int[] Types = [2, 1, 2, 2, 3, 2, 2, 2, 1, 3, 3, 2];

    [Fact]
    public void MockDataFingerprintMatchesLegacy()
    {
        var data = PreconfiguredData.GetPreconfiguredCatalogItems();
        Assert.Equal(12, data.Count);
        for (var i = 0; i < data.Count; i++)
        {
            var item = data[i];
            Assert.Equal(i + 1, item.Id);
            Assert.Equal(Names[i], item.Name);
            Assert.Equal(Names[i], item.Description);
            Assert.Equal(Prices[i], item.Price);
            Assert.Equal(Brands[i], item.CatalogBrandId);
            Assert.Equal(Types[i], item.CatalogTypeId);
            Assert.Equal($"{i + 1}.png", item.PictureFileName);
            Assert.Equal(100, item.AvailableStock);
            Assert.Equal(0, item.RestockThreshold);
            Assert.Equal(0, item.MaxStockThreshold);
            Assert.False(item.OnReorder);
            Assert.Null(item.PictureUri);
        }
        Assert.Equal(["1:Azure", "2:.NET", "3:Visual Studio", "4:SQL Server", "5:Other"],
            PreconfiguredData.GetPreconfiguredCatalogBrands().Select(b => $"{b.Id}:{b.Brand}"));
        Assert.Equal(["1:Mug", "2:T-Shirt", "3:Sheet", "4:USB Memory Stick"],
            PreconfiguredData.GetPreconfiguredCatalogTypes().Select(t => $"{t.Id}:{t.Type}"));
    }

    [Theory]
    [InlineData("/", 1, 10, "Showing 10 of 12 products - Page 1 - 2")]
    [InlineData("/Default", 1, 10, "Showing 10 of 12 products - Page 1 - 2")]
    [InlineData("/Default.aspx", 1, 10, "Showing 10 of 12 products - Page 1 - 2")]
    [InlineData("/Default/index/0/size/10", 1, 10, "Showing 10 of 12 products - Page 1 - 2")]
    [InlineData("/Default/index/1/size/10", 11, 2, "Showing 10 of 12 products - Page 2 - 2")]
    [InlineData("/Default/index/1/size/3", 4, 3, "Showing 3 of 12 products - Page 2 - 4")]
    [InlineData("/Default/index/8/size/10", 0, 0, "Showing 10 of 12 products - Page 9 - 2")]
    public async Task ListAndPagingMatchLegacy(string path, int firstId, int count, string pager)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync(path);
        Assert.Contains("<title>Home Page - Catalog manager (Web Forms)</title>", html);
        Assert.Contains(pager, html);
        var ids = Regex.Matches(html, "href=\"/Catalog/Details/(\\d+)\"").Select(m => int.Parse(m.Groups[1].Value)).ToArray();
        Assert.Equal(count, ids.Length);
        if (count > 0)
        {
            Assert.Equal(Enumerable.Range(firstId, count), ids);
            Assert.Contains("<th>Name</th><th>Description</th><th>Brand</th><th>Type</th><th>Price</th><th>Picture name</th><th>Stock</th><th>Restock</th><th>Max stock</th>", html);
        }
        else Assert.Contains("No data was returned.", html);
        if (path == "/")
        {
            Assert.Contains("class=\"esh-pager-item esh-pager-item--navigable esh-pager-item--hidden\">Previous", html);
            Assert.Contains("href=\"/Default/index/1/size/10\"", html);
        }
        if (firstId == 11)
        {
            Assert.Contains("class=\"esh-pager-item esh-pager-item--navigable esh-pager-item--hidden\">Next", html);
            Assert.Contains("href=\"/Default/index/0/size/10\"", html);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    public async Task DetailsWorkAsDirectEntryForEverySeedItem(int id)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync($"/Catalog/Details/{id}");
        Assert.Contains("<title>Details - Catalog manager (Web Forms)</title>", html);
        Assert.Contains(WebUtility.HtmlEncode(Names[id - 1]), html);
        Assert.Contains($"/Pics/{id}.png", html);
        Assert.Contains($"<dd>{(Brands[id - 1] == 2 ? ".NET" : "Other")}</dd>", html);
        Assert.Contains($"<dd>{new[] { "Mug", "T-Shirt", "Sheet" }[Types[id - 1] - 1]}</dd>", html);
        Assert.Contains($"href=\"/Catalog/Edit/{id}\"", html);
    }

    [Fact]
    public async Task CreateEditDeleteRoundTripPersistsAndRedirectsHome()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var values = ValidForm("New <item>");
        var created = await Post(client, "/Catalog/Create", values);
        AssertRedirectHome(created);
        var service = factory.Services.GetRequiredService<ICatalogService>();
        var item = service.FindCatalogItem(13);
        Assert.Equal("New <item>", item.Name);
        Assert.Equal("dummy.png", item.PictureFileName);
        Assert.Equal(13, service.GetCatalogItemsPaginated(20, 0).TotalItems);
        Assert.Contains("New &lt;item&gt;", await client.GetStringAsync("/Default/index/1/size/10"));
        var editPage = await client.GetStringAsync("/Catalog/Edit/13");
        Assert.Contains("readonly", editPage);
        values["Item.Name"] = "Changed";
        values["Item.Price"] = "22.25";
        values["Item.PictureFileName"] = "untrusted.png";
        AssertRedirectHome(await Post(client, "/Catalog/Edit/13", values));
        Assert.Contains("Changed", await client.GetStringAsync("/Catalog/Details/13"));
        Assert.Equal(22.25m, service.FindCatalogItem(13).Price);
        Assert.Equal("dummy.png", service.FindCatalogItem(13).PictureFileName);
        AssertRedirectHome(await Post(client, "/Catalog/Delete/13", []));
        Assert.Null(service.FindCatalogItem(13));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Catalog/Details/13")).StatusCode);
        Assert.Equal(12, service.GetCatalogItemsPaginated(20, 0).TotalItems);
    }

    [Theory]
    [InlineData("Item.Name", "", "The Name field is required.")]
    [InlineData("Item.Price", "-1", "The field Price")]
    [InlineData("Item.Price", "1.234", "maximum two decimals")]
    [InlineData("Item.Price", "1000001", "between 0 and 1 million")]
    [InlineData("Item.Price", "not-a-price", "not valid for Price")]
    [InlineData("Item.AvailableStock", "-1", "between 0 and 10 million")]
    [InlineData("Item.RestockThreshold", "10000001", "between 0 and 10 million")]
    [InlineData("Item.MaxStockThreshold", "-1", "between 0 and 10 million")]
    public async Task InvalidCreateDoesNotMutateCatalog(string field, string value, string error)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var form = ValidForm("Invalid");
        form[field] = value;
        var response = await Post(client, "/Catalog/Create", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(error, await response.Content.ReadAsStringAsync());
        Assert.Equal(12, factory.Services.GetRequiredService<ICatalogService>().GetCatalogItemsPaginated(20, 0).TotalItems);
    }

    [Theory]
    [InlineData("/Catalog/Create", "Item.CatalogBrandId", "Select a valid brand.")]
    [InlineData("/Catalog/Create", "Item.CatalogTypeId", "Select a valid type.")]
    [InlineData("/Catalog/Edit/1", "Item.CatalogBrandId", "Select a valid brand.")]
    [InlineData("/Catalog/Edit/1", "Item.CatalogTypeId", "Select a valid type.")]
    public async Task UnknownBrandOrTypeDoesNotMutateCatalog(string path, string field, string error)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var form = ValidForm("Invalid reference");
        form[field] = "999";
        var response = await Post(client, path, form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains(error, html);
        Assert.Contains("Invalid reference", html);
        var service = factory.Services.GetRequiredService<ICatalogService>();
        var original = service.FindCatalogItem(1);
        Assert.Equal(Names[0], original.Name);
        Assert.Equal(Prices[0], original.Price);
        Assert.Equal(Brands[0], original.CatalogBrandId);
        Assert.Equal(Types[0], original.CatalogTypeId);
        Assert.Null(service.FindCatalogItem(13));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
        Assert.Equal(12, service.GetCatalogItemsPaginated(20, 0).TotalItems);
    }

    [Fact]
    public async Task InvalidEditLeavesOriginalUntouched()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var form = ValidForm("");
        var response = await Post(client, "/Catalog/Edit/1", form);
        Assert.Contains("The Name field is required.", await response.Content.ReadAsStringAsync());
        Assert.Equal(Names[0], factory.Services.GetRequiredService<ICatalogService>().FindCatalogItem(1).Name);
    }

    [Theory]
    [InlineData("/Catalog/Details/999")]
    [InlineData("/Catalog/Edit/999")]
    [InlineData("/Catalog/Delete/999")]
    public async Task UnknownIdReturnsNotFound(string path)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task PostsRequireAntiforgeryToken()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        foreach (var path in new[] { "/Catalog/Create", "/Catalog/Edit/1", "/Catalog/Delete/1" })
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(path, new FormUrlEncodedContent(ValidForm("Forgery")))).StatusCode);
        }
    }

    [Fact]
    public async Task EmptyCatalogRendersLegacyEmptyState()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var service = factory.Services.GetRequiredService<ICatalogService>();
        foreach (var item in service.GetCatalogItemsPaginated(20, 0).Data.ToArray()) service.RemoveCatalogItem(item);
        var html = await client.GetStringAsync("/");
        Assert.Contains("No data was returned.", html);
        Assert.Contains("Showing 10 of 0 products - Page 1 - 0", html);
    }

    [Fact]
    public void MockMutationAndEdgeSemanticsMatchLegacy()
    {
        var service = new CatalogServiceMock();
        Assert.Null(service.FindCatalogItem(999));
        service.UpdateCatalogItem(new CatalogItem { Id = 999 });
        Assert.Equal(12, service.GetCatalogItemsPaginated(20, 0).TotalItems);
        service.RemoveCatalogItem(service.FindCatalogItem(12));
        var created = new CatalogItem { Name = "Replacement", CatalogBrandId = 1, CatalogTypeId = 1 };
        service.CreateCatalogItem(created);
        Assert.Equal(12, created.Id);
        service.UpdateCatalogItem(new CatalogItem { Id = 12, Name = "Updated", CatalogBrandId = 1, CatalogTypeId = 1 });
        Assert.Equal("Updated", service.FindCatalogItem(12).Name);
        Assert.Equal(Enumerable.Range(1, 12), service.GetCatalogItemsPaginated(20, 0).Data.Select(i => i.Id));
        Assert.Empty(service.GetCatalogItemsPaginated(10, 2).Data);
        Assert.Throws<DivideByZeroException>(() => service.GetCatalogItemsPaginated(0, 0));
        foreach (var item in service.GetCatalogItemsPaginated(20, 0).Data.ToArray()) service.RemoveCatalogItem(item);
        Assert.Throws<InvalidOperationException>(() => service.CreateCatalogItem(new CatalogItem()));
    }

    [Theory]
    [InlineData("/About", "About", "Your application description page.")]
    [InlineData("/About.aspx", "About", "Your application description page.")]
    [InlineData("/Contact", "Contact", "425.555.0100")]
    [InlineData("/Contact.aspx", "Contact", "425.555.0100")]
    public async Task InformationalPagesKeepContent(string path, string title, string content)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync(path);
        Assert.Contains($"<title>{title} - Catalog manager (Web Forms)</title>", html);
        Assert.Contains(content, html);
    }

    [Theory]
    [InlineData("/Content/bootstrap.css")]
    [InlineData("/Content/site.css")]
    [InlineData("/Content/custom.css")]
    [InlineData("/Content/base.css")]
    [InlineData("/fonts/Montserrat-Regular.woff2")]
    [InlineData("/images/brand.png")]
    [InlineData("/images/main_banner.png")]
    [InlineData("/Pics/1.png")]
    [InlineData("/Pics/dummy.png")]
    public async Task LegacyAssetsAreServed(string path)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public void SqlModeFailsWithClearMessage()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string> { ["UseMockData"] = "false" })));
        var error = Assert.Throws<NotSupportedException>(() => factory.CreateClient());
        Assert.Contains("UseMockData=false is not supported", error.Message);
    }

    private static Dictionary<string, string> ValidForm(string name) => new()
    {
        ["Item.Name"] = name,
        ["Item.Description"] = "Description",
        ["Item.CatalogBrandId"] = "2",
        ["Item.CatalogTypeId"] = "1",
        ["Item.Price"] = "10.50",
        ["Item.AvailableStock"] = "10",
        ["Item.RestockThreshold"] = "2",
        ["Item.MaxStockThreshold"] = "100"
    };

    private static async Task<HttpResponseMessage> Post(HttpClient client, string path, Dictionary<string, string> values)
    {
        var html = await client.GetStringAsync(path);
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        values["__RequestVerificationToken"] = WebUtility.HtmlDecode(token);
        return await client.PostAsync(path, new FormUrlEncodedContent(values));
    }

    private static void AssertRedirectHome(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
    }
}
