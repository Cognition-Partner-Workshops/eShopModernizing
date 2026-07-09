using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Catalog.Domain;
using Xunit;

namespace Catalog.Api.Tests;

/// <summary>
/// End-to-end tests over the real ASP.NET Core pipeline (routing, model binding,
/// JSON serialization) using <see cref="CatalogApiFactory"/> with a seeded Sqlite
/// database. Assertions are pinned to the behavioral baseline: 5 brands, 4 types,
/// 12 items, and the legacy PascalCase JSON shape.
/// </summary>
public class CatalogApiEndpointTests : IDisposable
{
    private readonly CatalogApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public CatalogApiEndpointTests()
    {
        _factory = new CatalogApiFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetBrands_ReturnsFiveBaselineBrands()
    {
        var response = await _client.GetAsync("/api/brands");
        response.EnsureSuccessStatusCode();

        var brands = await response.Content.ReadFromJsonAsync<List<CatalogBrand>>(JsonOptions);

        Assert.NotNull(brands);
        Assert.Equal(5, brands!.Count);
        Assert.Equal(
            new[] { "Azure", ".NET", "Visual Studio", "SQL Server", "Other" },
            brands.OrderBy(b => b.Id).Select(b => b.Brand).ToArray());
    }

    [Fact]
    public async Task GetBrands_ProducesLegacyPascalCaseJsonShape()
    {
        var json = await _client.GetStringAsync("/api/brands");

        Assert.Equal(
            "[{\"Id\":1,\"Brand\":\"Azure\"},{\"Id\":2,\"Brand\":\".NET\"}," +
            "{\"Id\":3,\"Brand\":\"Visual Studio\"},{\"Id\":4,\"Brand\":\"SQL Server\"}," +
            "{\"Id\":5,\"Brand\":\"Other\"}]",
            json);
    }

    [Fact]
    public async Task GetBrandById_ReturnsBaselineBrand()
    {
        var json = await _client.GetStringAsync("/api/brands/1");

        Assert.Equal("{\"Id\":1,\"Brand\":\"Azure\"}", json);
    }

    [Fact]
    public async Task GetBrandById_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/brands/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTypes_ReturnsFourBaselineTypes()
    {
        var response = await _client.GetAsync("/api/catalogtypes");
        response.EnsureSuccessStatusCode();

        var types = await response.Content.ReadFromJsonAsync<List<CatalogType>>(JsonOptions);

        Assert.NotNull(types);
        Assert.Equal(
            new[] { "Mug", "T-Shirt", "Sheet", "USB Memory Stick" },
            types!.OrderBy(t => t.Id).Select(t => t.Type).ToArray());
    }

    [Fact]
    public async Task GetItems_ReturnsTwelveBaselineItems()
    {
        var items = await _client.GetFromJsonAsync<List<CatalogItem>>("/api/catalogitems", JsonOptions);

        Assert.NotNull(items);
        Assert.Equal(12, items!.Count);
    }

    [Fact]
    public async Task GetItemById_ReturnsBaselineItem()
    {
        var item = await _client.GetFromJsonAsync<CatalogItem>("/api/catalogitems/1", JsonOptions);

        Assert.NotNull(item);
        Assert.Equal(1, item!.Id);
        Assert.Equal(".NET Bot Black Hoodie", item.Name);
        Assert.Equal(19.5M, item.Price);
        Assert.Equal("1.png", item.PictureFileName);
        Assert.Equal(2, item.CatalogBrandId);
        Assert.Equal(2, item.CatalogTypeId);
        Assert.Equal(".NET", item.CatalogBrand!.Brand);
        Assert.Equal("T-Shirt", item.CatalogType!.Type);
    }

    [Fact]
    public async Task GetItemById_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/catalogitems/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetItems_SkipTake_PagesInIdOrder()
    {
        var firstPage = await _client.GetFromJsonAsync<List<CatalogItem>>(
            "/api/catalogitems?skip=0&take=5", JsonOptions);
        var secondPage = await _client.GetFromJsonAsync<List<CatalogItem>>(
            "/api/catalogitems?skip=5&take=5", JsonOptions);

        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, firstPage!.Select(i => i.Id).ToArray());
        Assert.Equal(new[] { 6, 7, 8, 9, 10 }, secondPage!.Select(i => i.Id).ToArray());
    }

    [Fact]
    public async Task GetItems_FilterByBrand_ReturnsOnlyThatBrand()
    {
        var items = await _client.GetFromJsonAsync<List<CatalogItem>>(
            "/api/catalogitems?brandId=2", JsonOptions);

        Assert.NotNull(items);
        Assert.NotEmpty(items!);
        Assert.All(items!, item => Assert.Equal(2, item.CatalogBrandId));
        Assert.Equal(new[] { 1, 2, 4, 6, 10, 11 }, items!.Select(i => i.Id).ToArray());
    }

    [Fact]
    public async Task GetItems_FilterByType_ReturnsOnlyThatType()
    {
        var items = await _client.GetFromJsonAsync<List<CatalogItem>>(
            "/api/catalogitems?typeId=1", JsonOptions);

        Assert.NotNull(items);
        Assert.All(items!, item => Assert.Equal(1, item.CatalogTypeId));
        Assert.Equal(new[] { 2, 9 }, items!.Select(i => i.Id).ToArray());
    }

    [Fact]
    public async Task CreateItem_Returns201AndPersists()
    {
        var newItem = new CatalogItem
        {
            Name = "Test Widget",
            Description = "Test Widget Description",
            Price = 42.5M,
            PictureFileName = "test.png",
            CatalogTypeId = 1,
            CatalogBrandId = 1,
            AvailableStock = 10,
        };

        var response = await _client.PostAsJsonAsync("/api/catalogitems", newItem);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<CatalogItem>(JsonOptions);
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.Equal("Test Widget", created.Name);

        var fetched = await _client.GetFromJsonAsync<CatalogItem>(
            $"/api/catalogitems/{created.Id}", JsonOptions);
        Assert.NotNull(fetched);
        Assert.Equal("Test Widget", fetched!.Name);
        Assert.Equal(42.5M, fetched.Price);
    }

    [Fact]
    public async Task UpdateItem_Returns204AndPersistsChanges()
    {
        var item = await _client.GetFromJsonAsync<CatalogItem>("/api/catalogitems/1", JsonOptions);
        Assert.NotNull(item);

        item!.Name = "Renamed Hoodie";
        item.Price = 25.0M;

        var response = await _client.PutAsJsonAsync("/api/catalogitems/1", item);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var updated = await _client.GetFromJsonAsync<CatalogItem>("/api/catalogitems/1", JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Renamed Hoodie", updated!.Name);
        Assert.Equal(25.0M, updated.Price);
    }

    [Fact]
    public async Task UpdateItem_UnknownId_ReturnsNotFound()
    {
        var item = new CatalogItem { Id = 99999, Name = "Ghost", Description = "Ghost" };

        var response = await _client.PutAsJsonAsync("/api/catalogitems/99999", item);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteItem_Returns204AndRemovesItem()
    {
        var response = await _client.DeleteAsync("/api/catalogitems/1");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync("/api/catalogitems/1");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        var remaining = await _client.GetFromJsonAsync<List<CatalogItem>>("/api/catalogitems", JsonOptions);
        Assert.NotNull(remaining);
        Assert.Equal(11, remaining!.Count);
    }

    [Fact]
    public async Task DeleteItem_UnknownId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/catalogitems/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
