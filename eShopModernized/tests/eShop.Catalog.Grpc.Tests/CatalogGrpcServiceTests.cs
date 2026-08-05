using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using DomainDiscountItem = eShop.Catalog.Domain.Entities.DiscountItem;
using DomainStock = eShop.Catalog.Domain.Entities.CatalogItemsStock;

namespace eShop.Catalog.Grpc.Tests;

/// <summary>
/// One test per RPC of <c>catalog.proto</c>, driven through a real in-process gRPC channel.
/// The expectations come from the legacy WCF implementation
/// (eShopLegacyNTier/src/eShopWCFService/CatalogService.svc.cs); no SOAP golden output exists.
/// </summary>
public class CatalogGrpcServiceTests
{
    private static Timestamp Date(int year, int month, int day)
        => Timestamp.FromDateTime(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public async Task FindCatalogItem_ReturnsItemWithBrandAndType()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

        var item = await app.Client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = 1 });

        Assert.Equal(1, item.Id);
        Assert.Equal(".NET Bot Black Hoodie", item.Name);
        Assert.Equal("19.5", item.Price.Value);
        Assert.Equal("1.png", item.PictureFileName);
        Assert.Equal(".NET", item.CatalogBrand.Brand);
        Assert.Equal("T-Shirt", item.CatalogType.Type);
    }

    [Fact]
    public async Task FindCatalogItem_UnknownId_IsNotFound()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => app.Client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = 4242 }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetCatalogBrands_ReturnsTheSeededBrands()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

        var response = await app.Client.GetCatalogBrandsAsync(new Empty());

        Assert.Equal(5, response.Brands.Count);
        Assert.Equal(["Azure", ".NET", "Visual Studio", "SQL Server", "Other"], response.Brands.Select(brand => brand.Brand));
    }

    [Fact]
    public async Task GetCatalogTypes_ReturnsTheSeededTypes()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

        var response = await app.Client.GetCatalogTypesAsync(new Empty());

        Assert.Equal(4, response.CatalogTypes.Count);
        Assert.Equal(["Mug", "T-Shirt", "Sheet", "USB Memory Stick"], response.CatalogTypes.Select(type => type.Type));
    }

    [Fact]
    public async Task GetCatalogItems_ZeroFilters_ReturnEverythingOrderedById()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

        var response = await app.Client.GetCatalogItemsAsync(new GetCatalogItemsRequest());

        Assert.Equal(12, response.Items.Count);
        Assert.Equal(Enumerable.Range(1, 12), response.Items.Select(item => item.Id));
        // The legacy list operation does not populate the navigation properties.
        Assert.All(response.Items, item => Assert.Null(item.CatalogBrand));
        Assert.All(response.Items, item => Assert.Null(item.CatalogType));
    }

    [Fact]
    public async Task GetCatalogItems_FiltersOnBrandAndType()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

        var response = await app.Client.GetCatalogItemsAsync(
            new GetCatalogItemsRequest { BrandIdFilter = 5, TypeIdFilter = 2 });

        Assert.Equal([3, 7, 8, 12], response.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task GetAvailableStock_ReturnsTheStockForThatDate()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();
        app.Seed(new DomainStock { StockId = 1, CatalogItemId = 3, AvailableStock = 42, Date = new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Utc) });

        var response = await app.Client.GetAvailableStockAsync(
            new GetAvailableStockRequest { CatalogItemId = 3, Date = Date(2026, 5, 4) });

        Assert.Equal(42, response.AvailableStock);
    }

    [Fact]
    public async Task GetAvailableStock_WithoutAMatchingRow_ReturnsZero()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

        var response = await app.Client.GetAvailableStockAsync(
            new GetAvailableStockRequest { CatalogItemId = 3, Date = Date(2026, 5, 4) });

        Assert.Equal(0, response.AvailableStock);
    }

    [Fact]
    public async Task GetAvailableStock_WithoutADate_IsInvalidArgument()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => app.Client.GetAvailableStockAsync(new GetAvailableStockRequest { CatalogItemId = 3 }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task CreateAvailableStock_InsertsANewRow()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

        await app.Client.CreateAvailableStockAsync(new CatalogItemsStock
        {
            CatalogItemId = 3,
            AvailableStock = 7,
            Date = Date(2026, 5, 4),
        });

        using var context = app.CreateContext();
        var stock = Assert.Single(context.CatalogItemsStocks);
        Assert.Equal(1, stock.StockId);
        Assert.Equal(3, stock.CatalogItemId);
        Assert.Equal(7, stock.AvailableStock);
        Assert.Equal(new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Unspecified), stock.Date);
    }

    [Fact]
    public async Task CreateAvailableStock_OverwritesTheRowForThatItemAndDate()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();
        app.Seed(new DomainStock { StockId = 9, CatalogItemId = 3, AvailableStock = 42, Date = new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Utc) });

        await app.Client.CreateAvailableStockAsync(new CatalogItemsStock
        {
            CatalogItemId = 3,
            AvailableStock = 7,
            Date = Date(2026, 5, 4),
        });

        using var context = app.CreateContext();
        var stock = Assert.Single(context.CatalogItemsStocks);
        Assert.Equal(9, stock.StockId);
        Assert.Equal(7, stock.AvailableStock);
    }

    [Fact]
    public async Task CreateAvailableStock_WithoutACatalogItem_IsInvalidArgument()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => app.Client.CreateAvailableStockAsync(new CatalogItemsStock { Date = Date(2026, 5, 4) }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task CreateCatalogItem_AllocatesTheNextId()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

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

        using var context = app.CreateContext();
        var created = await context.CatalogItems.SingleAsync(item => item.Name == "Devin Mug");
        // The legacy service ignores the client-supplied id and allocates MAX(Id) + 1.
        Assert.Equal(13, created.Id);
        Assert.Equal(3.25m, created.Price);
        Assert.Equal("13.png", created.PictureFileName);
    }

    [Fact]
    public async Task CreateCatalogItem_WithoutAName_IsInvalidArgument()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => app.Client.CreateCatalogItemAsync(new CatalogItem { CatalogBrandId = 1, CatalogTypeId = 1 }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task UpdateCatalogItem_WritesTheContractFieldsAndKeepsTheRest()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

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

        using var context = app.CreateContext();
        var updated = await context.CatalogItems.SingleAsync(item => item.Id == 1);
        Assert.Equal("Renamed hoodie", updated.Name);
        Assert.Equal(21.75m, updated.Price);
        Assert.Equal(5, updated.CatalogBrandId);
        // AvailableStock is outside the legacy WCF data contract and must survive the update.
        Assert.Equal(100, updated.AvailableStock);
    }

    [Fact]
    public async Task UpdateCatalogItem_UnknownId_IsNotFound()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => app.Client.UpdateCatalogItemAsync(new CatalogItem { Id = 4242, Name = "Nope" }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task RemoveCatalogItem_DeletesTheRow()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

        await app.Client.RemoveCatalogItemAsync(new CatalogItem { Id = 4, Name = "irrelevant" });

        using var context = app.CreateContext();
        Assert.False(await context.CatalogItems.AnyAsync(item => item.Id == 4));
        Assert.Equal(11, await context.CatalogItems.CountAsync());
    }

    [Fact]
    public async Task RemoveCatalogItem_UnknownId_IsNotFound()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => app.Client.RemoveCatalogItemAsync(new CatalogItem { Id = 4242 }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetDiscount_ReturnsTheDiscountCoveringTheDay()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();
        app.Seed(new DomainDiscountItem
        {
            Id = 1,
            Size = 0.15,
            Start = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
        });

        var discount = await app.Client.GetDiscountAsync(new GetDiscountRequest { Day = Date(2026, 5, 4) });

        Assert.Equal(1, discount.Id);
        Assert.Equal(0.15, discount.Size);
        Assert.Equal(Date(2026, 5, 1), discount.Start);
        Assert.Equal(Date(2026, 5, 31), discount.End);
    }

    [Fact]
    public async Task GetDiscount_OutsideEveryRange_IsNotFound()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();
        app.Seed(new DomainDiscountItem
        {
            Id = 1,
            Size = 0.15,
            Start = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
        });

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => app.Client.GetDiscountAsync(new GetDiscountRequest { Day = Date(2026, 6, 1) }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }
}
