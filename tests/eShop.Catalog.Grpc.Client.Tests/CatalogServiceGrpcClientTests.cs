using eShop.Catalog.Domain;
using Grpc.Core;
using Xunit;

namespace eShop.Catalog.Grpc.Client.Tests;

/// <summary>
/// Every catalog operation the WinForms client performs, driven through the production client
/// wrapper against the real gRPC service in mock-data mode. This is the automated replacement for
/// clicking through the form: the UI itself can only be exercised on Windows.
/// </summary>
public class CatalogServiceGrpcClientTests : IDisposable
{
    private readonly CatalogServiceClientTestHost _host = new();

    private ICatalogServiceClient Client => _host.Client;

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task FindCatalogItem_ReturnsTheItemWithItsBrandAndType()
    {
        var item = await Client.FindCatalogItemAsync(1);

        Assert.NotNull(item);
        Assert.Equal(".NET Bot Black Hoodie", item.Name);
        Assert.Equal(19.5M, item.Price);
        Assert.Equal("1.png", item.PictureFileName);
        Assert.Equal(".NET", item.CatalogBrand?.Brand);
        Assert.Equal("T-Shirt", item.CatalogType?.Type);
    }

    [Fact]
    public async Task FindCatalogItem_ReturnsNullForAMissingItem_AsTheSoapProxyDid()
    {
        Assert.Null(await Client.FindCatalogItemAsync(9999));
    }

    [Fact]
    public async Task FindCatalogItem_SurfacesInvalidArgumentAsAnRpcException()
    {
        var exception = await Assert.ThrowsAsync<RpcException>(() => Client.FindCatalogItemAsync(0));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task GetCatalogBrands_ReturnsEveryBrand()
    {
        var brands = await Client.GetCatalogBrandsAsync();

        Assert.Equal(new[] { "Azure", ".NET", "Visual Studio", "SQL Server", "Other" }, brands.Select(b => b.Brand));
    }

    [Fact]
    public async Task GetCatalogTypes_ReturnsEveryType()
    {
        var types = await Client.GetCatalogTypesAsync();

        Assert.Equal(new[] { "Mug", "T-Shirt", "Sheet", "USB Memory Stick" }, types.Select(t => t.Type));
    }

    [Fact]
    public async Task GetCatalogItems_WithoutFilters_ReturnsEveryItem()
    {
        var items = await Client.GetCatalogItemsAsync(0, 0);

        Assert.Equal(12, items.Count);
        Assert.All(items, item =>
        {
            Assert.NotNull(item.CatalogBrand);
            Assert.NotNull(item.CatalogType);
        });
    }

    [Fact]
    public async Task GetCatalogItems_FiltersByBrandAndType()
    {
        var items = await Client.GetCatalogItemsAsync(5, 2);

        Assert.Equal(new[] { 3, 7, 8, 12 }, items.Select(i => i.Id));
    }

    [Fact]
    public async Task CreateCatalogItem_AppendsAnItemWithAServiceAssignedId()
    {
        await Client.CreateCatalogItemAsync(new CatalogItem
        {
            Name = "Modern Hoodie",
            Description = "Modern Hoodie",
            Price = 42.25M,
            PictureFileName = "13.png",
            CatalogBrandId = 2,
            CatalogTypeId = 2,
        });

        var created = (await Client.GetCatalogItemsAsync(0, 0)).Single(i => i.Name == "Modern Hoodie");

        Assert.Equal(13, created.Id);
        Assert.Equal(42.25M, created.Price);
    }

    [Fact]
    public async Task UpdateCatalogItem_PersistsTheNewValues()
    {
        var item = await Client.FindCatalogItemAsync(2);
        Assert.NotNull(item);

        item.Description = "Updated";
        item.Price = 9.99M;
        await Client.UpdateCatalogItemAsync(item);

        var updated = await Client.FindCatalogItemAsync(2);

        Assert.Equal("Updated", updated!.Description);
        Assert.Equal(9.99M, updated.Price);
    }

    [Fact]
    public async Task RemoveCatalogItem_DeletesTheItem()
    {
        var item = await Client.FindCatalogItemAsync(3);
        Assert.NotNull(item);

        await Client.RemoveCatalogItemAsync(item);

        var remaining = await Client.GetCatalogItemsAsync(0, 0);

        Assert.DoesNotContain(remaining, i => i.Id == 3);
        Assert.Equal(11, remaining.Count);
    }

    [Fact]
    public async Task GetAvailableStock_ReturnsTheStockRecordedForThatDay()
    {
        Assert.Equal(120, await Client.GetAvailableStockAsync(new DateTime(2017, 9, 21), 1));
    }

    [Fact]
    public async Task GetAvailableStock_ReturnsZeroWhenNoStockWasRecorded()
    {
        Assert.Equal(0, await Client.GetAvailableStockAsync(new DateTime(2026, 1, 1), 1));
    }

    [Fact]
    public async Task CreateAvailableStock_IsVisibleToASubsequentLookup()
    {
        await Client.CreateAvailableStockAsync(new CatalogItemsStock
        {
            CatalogItemId = 7,
            Date = new DateTime(2026, 2, 3),
            AvailableStock = 17,
        });

        Assert.Equal(17, await Client.GetAvailableStockAsync(new DateTime(2026, 2, 3), 7));
    }

    [Fact]
    public async Task GetDiscount_ReturnsTheDiscountCoveringTheDay()
    {
        var discount = await Client.GetDiscountAsync(new DateTime(2017, 9, 20));

        Assert.NotNull(discount);
        Assert.Equal(0.3, discount.Size, 5);
        Assert.Equal(new DateTime(2017, 9, 18), discount.Start);
        Assert.Equal(new DateTime(2017, 9, 21), discount.End);
    }

    [Fact]
    public async Task GetDiscount_ReturnsNullForADayWithoutADiscount_AsTheSoapProxyDid()
    {
        Assert.Null(await Client.GetDiscountAsync(new DateTime(2026, 1, 1)));
    }
}
