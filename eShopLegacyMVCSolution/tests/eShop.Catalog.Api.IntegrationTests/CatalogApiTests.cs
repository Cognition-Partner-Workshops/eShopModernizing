using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http.Json;
using eShop.Catalog.Api.Dtos;

namespace eShop.Catalog.Api.IntegrationTests;

[TestClass]
public class CatalogApiTests
{
    private static CatalogApiFactory _factory = null!;
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _factory = new CatalogApiFactory();
        _client = _factory.CreateClient();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task Items_DefaultPage_IsZeroBasedAndReturnsFirstTenOfTwelve()
    {
        var page = await _client.GetFromJsonAsync<PaginatedItemsDto<CatalogItemDto>>("/api/catalog/items");

        Assert.IsNotNull(page);
        Assert.AreEqual(0, page.PageIndex);
        Assert.AreEqual(10, page.PageSize);
        Assert.AreEqual(12, page.Count);
        Assert.AreEqual(10, page.Data.Count);
        Assert.AreEqual(".NET Bot Black Hoodie", page.Data[0].Name);
        CollectionAssert.AreEqual(Enumerable.Range(1, 10).ToList(), page.Data.Select(i => i.Id).ToList());
    }

    [TestMethod]
    public async Task Items_PageIndex1_ReturnsRemainingTwoItems()
    {
        var page = await _client.GetFromJsonAsync<PaginatedItemsDto<CatalogItemDto>>("/api/catalog/items?pageSize=10&pageIndex=1");

        Assert.IsNotNull(page);
        Assert.AreEqual(1, page.PageIndex);
        Assert.AreEqual(12, page.Count);
        CollectionAssert.AreEqual(new[] { 11, 12 }, page.Data.Select(i => i.Id).ToArray());
    }

    [TestMethod]
    public async Task Items_PageSize5_PageIndex1_SkipsExactlyFiveItems()
    {
        var page = await _client.GetFromJsonAsync<PaginatedItemsDto<CatalogItemDto>>("/api/catalog/items?pageSize=5&pageIndex=1");

        Assert.IsNotNull(page);
        CollectionAssert.AreEqual(new[] { 6, 7, 8, 9, 10 }, page.Data.Select(i => i.Id).ToArray());
    }

    [TestMethod]
    public async Task Items_PageBeyondEnd_ReturnsEmptyDataWithTotalCount()
    {
        var page = await _client.GetFromJsonAsync<PaginatedItemsDto<CatalogItemDto>>("/api/catalog/items?pageSize=10&pageIndex=5");

        Assert.IsNotNull(page);
        Assert.AreEqual(12, page.Count);
        Assert.AreEqual(0, page.Data.Count);
    }

    [TestMethod]
    public async Task Items_InvalidPaging_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/catalog/items?pageSize=0&pageIndex=-1");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Items_IncludeBrandAndTypeNames()
    {
        var page = await _client.GetFromJsonAsync<PaginatedItemsDto<CatalogItemDto>>("/api/catalog/items?pageSize=1&pageIndex=0");

        Assert.IsNotNull(page);
        var item = page.Data.Single();
        Assert.AreEqual(".NET", item.CatalogBrand);
        Assert.AreEqual("T-Shirt", item.CatalogType);
    }

    [TestMethod]
    public async Task ItemById_Existing_ReturnsItem()
    {
        var item = await _client.GetFromJsonAsync<CatalogItemDto>("/api/catalog/items/2");

        Assert.IsNotNull(item);
        Assert.AreEqual(2, item.Id);
        Assert.AreEqual(".NET Black & White Mug", item.Name);
        Assert.AreEqual(8.50m, item.Price);
        Assert.AreEqual("Mug", item.CatalogType);
    }

    [TestMethod]
    public async Task ItemById_Missing_Returns404()
    {
        var response = await _client.GetAsync("/api/catalog/items/9999");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Items_FilterByBrand_ReturnsOnlyThatBrand()
    {
        var brands = await _client.GetFromJsonAsync<List<CatalogBrandDto>>("/api/catalog/brands");
        var dotnet = brands!.Single(b => b.Brand == ".NET");

        var page = await _client.GetFromJsonAsync<PaginatedItemsDto<CatalogItemDto>>($"/api/catalog/items?pageSize=20&brandId={dotnet.Id}");

        Assert.IsNotNull(page);
        Assert.AreEqual(6, page.Count);
        Assert.IsTrue(page.Data.All(i => i.CatalogBrandId == dotnet.Id));
    }

    [TestMethod]
    public async Task Items_FilterByType_ReturnsOnlyThatType()
    {
        var types = await _client.GetFromJsonAsync<List<CatalogTypeDto>>("/api/catalog/types");
        var sheet = types!.Single(t => t.Type == "Sheet");

        var page = await _client.GetFromJsonAsync<PaginatedItemsDto<CatalogItemDto>>($"/api/catalog/items?pageSize=20&typeId={sheet.Id}");

        Assert.IsNotNull(page);
        Assert.AreEqual(3, page.Count);
        Assert.IsTrue(page.Data.All(i => i.CatalogTypeId == sheet.Id));
    }

    [TestMethod]
    public async Task Items_FilterByBrandAndType_CombinesFilters()
    {
        var brands = await _client.GetFromJsonAsync<List<CatalogBrandDto>>("/api/catalog/brands");
        var types = await _client.GetFromJsonAsync<List<CatalogTypeDto>>("/api/catalog/types");
        var other = brands!.Single(b => b.Brand == "Other");
        var mug = types!.Single(t => t.Type == "Mug");

        var page = await _client.GetFromJsonAsync<PaginatedItemsDto<CatalogItemDto>>($"/api/catalog/items?brandId={other.Id}&typeId={mug.Id}");

        Assert.IsNotNull(page);
        Assert.AreEqual(1, page.Count);
        Assert.AreEqual("Cup<T> White Mug", page.Data.Single().Name);
    }

    [TestMethod]
    public async Task Items_FilterByUnknownBrand_ReturnsEmpty()
    {
        var page = await _client.GetFromJsonAsync<PaginatedItemsDto<CatalogItemDto>>("/api/catalog/items?brandId=9999");

        Assert.IsNotNull(page);
        Assert.AreEqual(0, page.Count);
        Assert.AreEqual(0, page.Data.Count);
    }

    [TestMethod]
    public async Task Brands_ReturnsFivePreconfiguredBrands()
    {
        var brands = await _client.GetFromJsonAsync<List<CatalogBrandDto>>("/api/catalog/brands");

        Assert.IsNotNull(brands);
        Assert.AreEqual(5, brands.Count);
        CollectionAssert.AreEquivalent(new[] { "Azure", ".NET", "Visual Studio", "SQL Server", "Other" }, brands.Select(b => b.Brand).ToArray());
    }

    [TestMethod]
    public async Task Types_ReturnsFourPreconfiguredTypes()
    {
        var types = await _client.GetFromJsonAsync<List<CatalogTypeDto>>("/api/catalog/types");

        Assert.IsNotNull(types);
        Assert.AreEqual(4, types.Count);
        CollectionAssert.AreEquivalent(new[] { "Mug", "T-Shirt", "Sheet", "USB Memory Stick" }, types.Select(t => t.Type).ToArray());
    }

    [TestMethod]
    public async Task CreateUpdateDelete_Item_RoundTrips()
    {
        var write = new CatalogItemWriteDto
        {
            Name = "Integration Item", Description = "created by test", Price = 3.25m, PictureFileName = "1.png",
            CatalogBrandId = 1, CatalogTypeId = 1, AvailableStock = 5, MaxStockThreshold = 10,
        };

        var createResponse = await _client.PostAsJsonAsync("/api/catalog/items", write);
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CatalogItemDto>();
        Assert.IsNotNull(created);
        Assert.IsTrue(created.Id > 12);
        Assert.AreEqual($"/api/catalog/items/{created.Id}", createResponse.Headers.Location!.PathAndQuery, ignoreCase: true);

        write.Name = "Integration Item (updated)";
        var updateResponse = await _client.PutAsJsonAsync($"/api/catalog/items/{created.Id}", write);
        Assert.AreEqual(HttpStatusCode.NoContent, updateResponse.StatusCode);
        var updated = await _client.GetFromJsonAsync<CatalogItemDto>($"/api/catalog/items/{created.Id}");
        Assert.AreEqual("Integration Item (updated)", updated!.Name);

        var deleteResponse = await _client.DeleteAsync($"/api/catalog/items/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var afterDelete = await _client.GetAsync($"/api/catalog/items/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [TestMethod]
    public async Task CreateItem_MissingName_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/catalog/items", new { Price = 1.0m });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Health_Returns200()
    {
        var response = await _client.GetAsync("/health");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
