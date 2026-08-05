using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace eShop.Catalog.Grpc.Tests;

/// <summary>
/// Every RPC of <c>catalog.proto</c> exercised against the host started with
/// <c>Catalog:UseMockData=true</c>, i.e. with no database at all. The expectations are the
/// in-memory seed data (eShop.Catalog.Data.Infrastructure.PreconfiguredData); the RPC semantics
/// themselves are covered by <see cref="CatalogGrpcServiceTests" /> on the EF Core path.
/// </summary>
public class CatalogGrpcMockDataTests
{
    private static Timestamp Date(int year, int month, int day)
        => Timestamp.FromDateTime(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public async Task FindCatalogItem_ReturnsTheMockItemWithBrandAndType()
    {
        using var app = new CatalogGrpcMockApplication();

        var item = await app.Client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = 1 });

        Assert.Equal(".NET Bot Black Hoodie", item.Name);
        Assert.Equal("19.5", item.Price.Value);
        Assert.Equal(".NET", item.CatalogBrand.Brand);
        Assert.Equal("T-Shirt", item.CatalogType.Type);
    }

    [Fact]
    public async Task FindCatalogItem_UnknownId_IsNotFound()
    {
        using var app = new CatalogGrpcMockApplication();

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => app.Client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = 4242 }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetCatalogBrands_ReturnsTheMockBrands()
    {
        using var app = new CatalogGrpcMockApplication();

        var response = await app.Client.GetCatalogBrandsAsync(new Empty());

        Assert.Equal(["Azure", ".NET", "Visual Studio", "SQL Server", "Other"], response.Brands.Select(brand => brand.Brand));
    }

    [Fact]
    public async Task GetCatalogTypes_ReturnsTheMockTypes()
    {
        using var app = new CatalogGrpcMockApplication();

        var response = await app.Client.GetCatalogTypesAsync(new Empty());

        Assert.Equal(["Mug", "T-Shirt", "Sheet", "USB Memory Stick"], response.CatalogTypes.Select(type => type.Type));
    }

    [Fact]
    public async Task GetCatalogItems_ZeroFilters_ReturnEverythingOrderedById()
    {
        using var app = new CatalogGrpcMockApplication();

        var response = await app.Client.GetCatalogItemsAsync(new GetCatalogItemsRequest());

        Assert.Equal(Enumerable.Range(1, 12), response.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task GetCatalogItems_FiltersOnBrandAndType()
    {
        using var app = new CatalogGrpcMockApplication();

        var response = await app.Client.GetCatalogItemsAsync(
            new GetCatalogItemsRequest { BrandIdFilter = 5, TypeIdFilter = 2 });

        Assert.Equal([3, 7, 8, 12], response.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task GetAvailableStock_ReturnsTheSeededStockForThatDate()
    {
        using var app = new CatalogGrpcMockApplication();

        var response = await app.Client.GetAvailableStockAsync(
            new GetAvailableStockRequest { CatalogItemId = 1, Date = Date(2017, 9, 21) });

        Assert.Equal(120, response.AvailableStock);
    }

    [Fact]
    public async Task GetAvailableStock_WithoutAMatchingRow_ReturnsZero()
    {
        using var app = new CatalogGrpcMockApplication();

        var response = await app.Client.GetAvailableStockAsync(
            new GetAvailableStockRequest { CatalogItemId = 3, Date = Date(2026, 5, 4) });

        Assert.Equal(0, response.AvailableStock);
    }

    [Fact]
    public async Task GetAvailableStock_WithoutADate_IsInvalidArgument()
    {
        using var app = new CatalogGrpcMockApplication();

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => app.Client.GetAvailableStockAsync(new GetAvailableStockRequest { CatalogItemId = 1 }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task CreateAvailableStock_RecordsANewEntry()
    {
        using var app = new CatalogGrpcMockApplication();

        await app.Client.CreateAvailableStockAsync(new CatalogItemsStock
        {
            CatalogItemId = 3,
            AvailableStock = 7,
            Date = Date(2026, 5, 4),
        });

        var response = await app.Client.GetAvailableStockAsync(
            new GetAvailableStockRequest { CatalogItemId = 3, Date = Date(2026, 5, 4) });

        Assert.Equal(7, response.AvailableStock);
    }

    [Fact]
    public async Task CreateAvailableStock_OverwritesTheEntryForThatItemAndDate()
    {
        using var app = new CatalogGrpcMockApplication();

        await app.Client.CreateAvailableStockAsync(new CatalogItemsStock
        {
            CatalogItemId = 1,
            AvailableStock = 5,
            Date = Date(2017, 9, 21),
        });

        var response = await app.Client.GetAvailableStockAsync(
            new GetAvailableStockRequest { CatalogItemId = 1, Date = Date(2017, 9, 21) });

        Assert.Equal(5, response.AvailableStock);
    }

    [Fact]
    public async Task CreateAvailableStock_WithoutACatalogItem_IsInvalidArgument()
    {
        using var app = new CatalogGrpcMockApplication();

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => app.Client.CreateAvailableStockAsync(new CatalogItemsStock { Date = Date(2026, 5, 4) }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task CreateCatalogItem_AllocatesTheNextId()
    {
        using var app = new CatalogGrpcMockApplication();

        await app.Client.CreateCatalogItemAsync(new CatalogItem
        {
            Id = 999,
            Name = "Devin Mug",
            Description = "A mug",
            Price = new DecimalValue { Value = "3.25" },
            PictureFileName = "13.png",
            CatalogBrandId = 1,
            CatalogTypeId = 1,
        });

        var created = await app.Client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = 13 });

        Assert.Equal("Devin Mug", created.Name);
        Assert.Equal("3.25", created.Price.Value);
        Assert.Equal("13.png", created.PictureFileName);
    }

    [Fact]
    public async Task CreateCatalogItem_WithoutAName_IsInvalidArgument()
    {
        using var app = new CatalogGrpcMockApplication();

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => app.Client.CreateCatalogItemAsync(new CatalogItem { CatalogBrandId = 1, CatalogTypeId = 1 }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task UpdateCatalogItem_WritesTheContractFields()
    {
        using var app = new CatalogGrpcMockApplication();

        await app.Client.UpdateCatalogItemAsync(new CatalogItem
        {
            Id = 1,
            Name = "Renamed hoodie",
            Description = "Renamed",
            Price = new DecimalValue { Value = "21.75" },
            PictureFileName = "1.png",
            CatalogBrandId = 5,
            CatalogTypeId = 2,
        });

        var updated = await app.Client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = 1 });

        Assert.Equal("Renamed hoodie", updated.Name);
        Assert.Equal("21.75", updated.Price.Value);
        Assert.Equal(5, updated.CatalogBrandId);
    }

    [Fact]
    public async Task UpdateCatalogItem_UnknownId_IsNotFound()
    {
        using var app = new CatalogGrpcMockApplication();

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => app.Client.UpdateCatalogItemAsync(new CatalogItem { Id = 4242, Name = "Nope" }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task RemoveCatalogItem_DropsTheItem()
    {
        using var app = new CatalogGrpcMockApplication();

        await app.Client.RemoveCatalogItemAsync(new CatalogItem { Id = 4, Name = "irrelevant" });

        var response = await app.Client.GetCatalogItemsAsync(new GetCatalogItemsRequest());

        Assert.Equal(11, response.Items.Count);
        Assert.DoesNotContain(4, response.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task RemoveCatalogItem_UnknownId_IsNotFound()
    {
        using var app = new CatalogGrpcMockApplication();

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => app.Client.RemoveCatalogItemAsync(new CatalogItem { Id = 4242 }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetDiscount_ReturnsTheDiscountCoveringTheDay()
    {
        using var app = new CatalogGrpcMockApplication();

        var discount = await app.Client.GetDiscountAsync(new GetDiscountRequest { Day = Date(2017, 9, 20) });

        Assert.Equal(1, discount.Id);
        Assert.Equal(0.3, discount.Size);
        Assert.Equal(Date(2017, 9, 18), discount.Start);
        Assert.Equal(Date(2017, 9, 21), discount.End);
    }

    [Fact]
    public async Task GetDiscount_OutsideEveryRange_IsNotFound()
    {
        using var app = new CatalogGrpcMockApplication();

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => app.Client.GetDiscountAsync(new GetDiscountRequest { Day = Date(2017, 10, 1) }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }
}
