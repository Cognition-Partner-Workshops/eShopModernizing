using eShop.Catalog.Grpc.Mapping;
using eShop.Catalog.Grpc.Protos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Xunit;

namespace eShop.Catalog.Grpc.Tests;

/// <summary>
/// Exercises every one of the ten operations of the legacy WCF contract over a real gRPC channel,
/// against the mock data set (the same data the legacy WCF <c>PreconfiguredData</c> declared).
/// </summary>
public class CatalogGrpcServiceTests : IDisposable
{
    private readonly CatalogGrpcTestHost _host = new();

    private Protos.Catalog.CatalogClient Client => _host.Client;

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task FindCatalogItem_ReturnsTheItemWithItsBrandAndType()
    {
        var response = await Client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = 1 });

        Assert.Equal(1, response.Item.Id);
        Assert.Equal(".NET Bot Black Hoodie", response.Item.Name);
        Assert.Equal(".NET Bot Black Hoodie", response.Item.Description);
        Assert.Equal(19.5M, CatalogProtoMapper.ToDecimal(response.Item.Price));
        Assert.Equal("1.png", response.Item.PictureFilename);
        Assert.Equal(2, response.Item.CatalogBrandId);
        Assert.Equal(2, response.Item.CatalogTypeId);
        Assert.Equal(".NET", response.Item.CatalogBrand.Brand);
        Assert.Equal("T-Shirt", response.Item.CatalogType.Type);
    }

    [Fact]
    public async Task FindCatalogItem_MapsAMissingItemToNotFound()
    {
        var exception = await Assert.ThrowsAsync<RpcException>(
            () => Client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = 9999 }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task FindCatalogItem_MapsANonPositiveIdToInvalidArgument()
    {
        var exception = await Assert.ThrowsAsync<RpcException>(
            () => Client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = 0 }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task GetCatalogBrands_ReturnsEveryBrand()
    {
        var response = await Client.GetCatalogBrandsAsync(new GetCatalogBrandsRequest());

        Assert.Equal(5, response.Brands.Count);
        Assert.Equal(new[] { "Azure", ".NET", "Visual Studio", "SQL Server", "Other" }, response.Brands.Select(b => b.Brand));
    }

    [Fact]
    public async Task GetCatalogTypes_ReturnsEveryType()
    {
        var response = await Client.GetCatalogTypesAsync(new GetCatalogTypesRequest());

        Assert.Equal(new[] { "Mug", "T-Shirt", "Sheet", "USB Memory Stick" }, response.Types_.Select(t => t.Type));
    }

    [Fact]
    public async Task GetCatalogItems_WithoutFilters_ReturnsEveryItem()
    {
        var response = await Client.GetCatalogItemsAsync(new GetCatalogItemsRequest());

        Assert.Equal(12, response.Items.Count);
        Assert.All(response.Items, item =>
        {
            Assert.NotNull(item.CatalogBrand);
            Assert.NotNull(item.CatalogType);
        });
    }

    [Fact]
    public async Task GetCatalogItems_FiltersByBrandAndType()
    {
        var response = await Client.GetCatalogItemsAsync(new GetCatalogItemsRequest
        {
            BrandIdFilter = 5,
            TypeIdFilter = 2,
        });

        Assert.Equal(new[] { 3, 7, 8, 12 }, response.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetCatalogItems_MapsANegativeFilterToInvalidArgument()
    {
        var exception = await Assert.ThrowsAsync<RpcException>(
            () => Client.GetCatalogItemsAsync(new GetCatalogItemsRequest { BrandIdFilter = -1 }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task CreateCatalogItem_AppendsAnItemWithAServiceAssignedId()
    {
        await Client.CreateCatalogItemAsync(new CreateCatalogItemRequest
        {
            CatalogItem = new CatalogItem
            {
                Name = "Devin Hoodie",
                Description = "Devin Hoodie",
                Price = CatalogProtoMapper.ToDecimalValue(42.25M),
                PictureFilename = "13.png",
                CatalogBrandId = 2,
                CatalogTypeId = 2,
            },
        });

        var created = (await Client.GetCatalogItemsAsync(new GetCatalogItemsRequest())).Items.Single(i => i.Name == "Devin Hoodie");

        Assert.Equal(13, created.Id);
        Assert.Equal(42.25M, CatalogProtoMapper.ToDecimal(created.Price));
        Assert.Equal("13.png", created.PictureFilename);
    }

    [Fact]
    public async Task CreateCatalogItem_MapsAMissingPayloadToInvalidArgument()
    {
        var exception = await Assert.ThrowsAsync<RpcException>(
            () => Client.CreateCatalogItemAsync(new CreateCatalogItemRequest()).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task UpdateCatalogItem_PersistsTheNewValues()
    {
        await Client.UpdateCatalogItemAsync(new UpdateCatalogItemRequest
        {
            CatalogItem = new CatalogItem
            {
                Id = 2,
                Name = ".NET Black & White Mug (2026)",
                Description = "Updated",
                Price = CatalogProtoMapper.ToDecimalValue(9.99M),
                PictureFilename = "2.png",
                CatalogBrandId = 2,
                CatalogTypeId = 1,
            },
        });

        var updated = (await Client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = 2 })).Item;

        Assert.Equal(".NET Black & White Mug (2026)", updated.Name);
        Assert.Equal("Updated", updated.Description);
        Assert.Equal(9.99M, CatalogProtoMapper.ToDecimal(updated.Price));
    }

    [Fact]
    public async Task UpdateCatalogItem_MapsAMissingItemToNotFound()
    {
        var exception = await Assert.ThrowsAsync<RpcException>(
            () => Client.UpdateCatalogItemAsync(new UpdateCatalogItemRequest
            {
                CatalogItem = new CatalogItem { Id = 9999, Name = "ghost" },
            }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task RemoveCatalogItem_DeletesTheItem()
    {
        await Client.RemoveCatalogItemAsync(new RemoveCatalogItemRequest
        {
            CatalogItem = new CatalogItem { Id = 3, Name = "Prism White T-Shirt" },
        });

        var remaining = (await Client.GetCatalogItemsAsync(new GetCatalogItemsRequest())).Items;

        Assert.DoesNotContain(remaining, i => i.Id == 3);
        Assert.Equal(11, remaining.Count);
    }

    [Fact]
    public async Task RemoveCatalogItem_MapsAMissingItemToNotFound()
    {
        var exception = await Assert.ThrowsAsync<RpcException>(
            () => Client.RemoveCatalogItemAsync(new RemoveCatalogItemRequest
            {
                CatalogItem = new CatalogItem { Id = 9999 },
            }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetAvailableStock_ReturnsTheStockRecordedForThatDay()
    {
        var response = await Client.GetAvailableStockAsync(new GetAvailableStockRequest
        {
            CatalogItemId = 1,
            Date = Date(2017, 9, 21),
        });

        Assert.Equal(120, response.AvailableStock);
    }

    [Fact]
    public async Task GetAvailableStock_ReturnsZeroWhenNoStockWasRecorded()
    {
        var response = await Client.GetAvailableStockAsync(new GetAvailableStockRequest
        {
            CatalogItemId = 1,
            Date = Date(2026, 1, 1),
        });

        Assert.Equal(0, response.AvailableStock);
    }

    [Fact]
    public async Task GetAvailableStock_MapsAMissingDateToInvalidArgument()
    {
        var exception = await Assert.ThrowsAsync<RpcException>(
            () => Client.GetAvailableStockAsync(new GetAvailableStockRequest { CatalogItemId = 1 }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task CreateAvailableStock_InsertsANewRow()
    {
        await Client.CreateAvailableStockAsync(new CreateAvailableStockRequest
        {
            CatalogItemsStock = new CatalogItemsStock
            {
                CatalogItemId = 7,
                Date = Date(2026, 2, 3),
                AvailableStock = 17,
            },
        });

        var response = await Client.GetAvailableStockAsync(new GetAvailableStockRequest
        {
            CatalogItemId = 7,
            Date = Date(2026, 2, 3),
        });

        Assert.Equal(17, response.AvailableStock);
    }

    [Fact]
    public async Task CreateAvailableStock_OverwritesTheRowForTheSameItemAndDay()
    {
        await Client.CreateAvailableStockAsync(new CreateAvailableStockRequest
        {
            CatalogItemsStock = new CatalogItemsStock
            {
                CatalogItemId = 1,
                Date = Date(2017, 9, 20),
                AvailableStock = 5,
            },
        });

        var response = await Client.GetAvailableStockAsync(new GetAvailableStockRequest
        {
            CatalogItemId = 1,
            Date = Date(2017, 9, 20),
        });

        Assert.Equal(5, response.AvailableStock);
    }

    [Fact]
    public async Task GetDiscount_ReturnsTheDiscountCoveringTheDay()
    {
        var response = await Client.GetDiscountAsync(new GetDiscountRequest { Day = Date(2017, 9, 20) });

        Assert.Equal(0.3, response.Discount.Size, 5);
        Assert.Equal(Date(2017, 9, 18), response.Discount.Start);
        Assert.Equal(Date(2017, 9, 21), response.Discount.End);
    }

    [Fact]
    public async Task GetDiscount_MapsADayWithoutADiscountToNotFound()
    {
        var exception = await Assert.ThrowsAsync<RpcException>(
            () => Client.GetDiscountAsync(new GetDiscountRequest { Day = Date(2026, 1, 1) }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    private static Timestamp Date(int year, int month, int day) =>
        Timestamp.FromDateTime(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc));
}
