using eShop.Catalog.Grpc.Protos;
using eShopWcf.Catalog.Grpc.Mapping;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace eShopWcf.Catalog.Grpc.Tests;

public sealed class CatalogReadTests(CatalogGrpcTestHost host) : IClassFixture<CatalogGrpcTestHost>
{
    private static readonly string[] ExpectedBrands = ["Azure", ".NET", "Visual Studio", "SQL Server", "Other"];
    private static readonly string[] ExpectedTypes = ["Mug", "T-Shirt", "Sheet", "USB Memory Stick"];
    private eShop.Catalog.Grpc.Protos.Catalog.CatalogClient Client => host.Client;

    [Fact]
    public async Task FindPopulatesBrandAndTypeButListDoesNot()
    {
        var item = (await Client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = 1 })).Item;
        Assert.Equal(".NET Bot Black Hoodie", item.Name);
        Assert.Equal(item.Name, item.Description);
        Assert.Equal("2.png", item.PictureFilename);
        Assert.Equal(19.5M, CatalogProtoMapper.ToDecimal(item.Price));
        Assert.Equal(".NET", item.CatalogBrand.Brand);
        Assert.Equal("T-Shirt", item.CatalogType.Type);
        var items = (await Client.GetCatalogItemsAsync(new GetCatalogItemsRequest())).Items;
        Assert.Equal(Enumerable.Range(1, 12), items.Select(x => x.Id));
        Assert.All(items, x => { Assert.Null(x.CatalogBrand); Assert.Null(x.CatalogType); });
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(9999)]
    public async Task MissingFindIsNotFound(int id) =>
        await RpcAssert.Fault(StatusCode.NotFound, () => Client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = id }).ResponseAsync);

    [Fact]
    public async Task BrandsAndTypesAreOrdered()
    {
        var brands = (await Client.GetCatalogBrandsAsync(new GetCatalogBrandsRequest())).Brands;
        Assert.Equal(Enumerable.Range(1, 5), brands.Select(x => x.Id));
        Assert.Equal(ExpectedBrands, brands.Select(x => x.Brand));
        var types = (await Client.GetCatalogTypesAsync(new GetCatalogTypesRequest())).Types_;
        Assert.Equal(Enumerable.Range(1, 4), types.Select(x => x.Id));
        Assert.Equal(ExpectedTypes, types.Select(x => x.Type));
    }

    [Theory]
    [InlineData(5, 2, "3,7,8,12")]
    [InlineData(0, 1, "2,9")]
    [InlineData(2, 0, "1,2,4,6,10,11")]
    [InlineData(-1, 0, "")]
    [InlineData(0, -1, "")]
    [InlineData(9999, 9999, "")]
    public async Task FiltersPreserveLegacySemantics(int brand, int type, string expected)
    {
        var response = await Client.GetCatalogItemsAsync(new GetCatalogItemsRequest { BrandIdFilter = brand, TypeIdFilter = type });
        Assert.Equal(expected, string.Join(",", response.Items.Select(x => x.Id)));
    }

    [Theory]
    [InlineData(9, 18, 0.3f)]
    [InlineData(9, 21, 0.3f)]
    [InlineData(9, 22, 0.25f)]
    [InlineData(9, 26, 0.25f)]
    [InlineData(9, 27, 0.1f)]
    [InlineData(9, 30, 0.1f)]
    [InlineData(10, 5, 0.5f)]
    [InlineData(10, 20, 0.5f)]
    [InlineData(11, 13, 0.3f)]
    [InlineData(11, 25, 0.3f)]
    [InlineData(12, 20, 0.25f)]
    [InlineData(12, 25, 0.25f)]
    public async Task DiscountBoundariesAreInclusiveAndFloatLiteralIsPreserved(int month, int day, float size)
    {
        var result = await Client.GetDiscountAsync(new GetDiscountRequest { Day = RpcAssert.Date(2017, month, day, 23) });
        Assert.Equal((double)size, result.Discount.Size);
        Assert.InRange(RpcAssert.Date(2017, month, day).Seconds, result.Discount.Start.Seconds, result.Discount.End.Seconds);
    }

    [Theory]
    [InlineData(9, 17)]
    [InlineData(10, 1)]
    [InlineData(10, 21)]
    [InlineData(12, 26)]
    public async Task NoDiscountIsNotFound(int month, int day) =>
        await RpcAssert.Fault(StatusCode.NotFound, () => Client.GetDiscountAsync(new GetDiscountRequest { Day = RpcAssert.Date(2017, month, day) }).ResponseAsync);

    [Fact]
    public async Task MissingDatesAreInvalidArguments()
    {
        await RpcAssert.Fault(StatusCode.InvalidArgument, () => Client.GetDiscountAsync(new GetDiscountRequest()).ResponseAsync);
        await RpcAssert.Fault(StatusCode.InvalidArgument, () => Client.GetAvailableStockAsync(new GetAvailableStockRequest()).ResponseAsync);
    }
}

public sealed class CatalogWriteTests(CatalogGrpcTestHost host) : IClassFixture<CatalogGrpcTestHost>
{
    private eShop.Catalog.Grpc.Protos.Catalog.CatalogClient Client => host.Client;

    [Fact]
    public async Task CreateUpdateRemoveRoundTripAllScalarsAndIgnoreCallerId()
    {
        await Client.CreateCatalogItemAsync(new CreateCatalogItemRequest
        {
            CatalogItem = new CatalogItem
            {
                Id = 9999,
                Name = "",
                Description = "created",
                Price = CatalogProtoMapper.ToDecimalValue(922337203685477.5807M),
                PictureFilename = "",
                CatalogBrandId = 2,
                CatalogTypeId = 2,
            },
        });
        var created = (await Client.GetCatalogItemsAsync(new GetCatalogItemsRequest())).Items.Single(x => x.Description == "created");
        Assert.Equal(13, created.Id);
        Assert.Equal("", created.Name);
        Assert.Equal("", created.PictureFilename);
        Assert.Equal(922337203685477.5807M, CatalogProtoMapper.ToDecimal(created.Price));
        await Client.UpdateCatalogItemAsync(new UpdateCatalogItemRequest
        {
            CatalogItem = new CatalogItem
            {
                Id = created.Id,
                Name = "  ",
                Description = "",
                Price = CatalogProtoMapper.ToDecimalValue(-1.25M),
                PictureFilename = "",
                CatalogBrandId = 3,
                CatalogTypeId = 1,
            },
        });
        var updated = (await Client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = created.Id })).Item;
        Assert.Equal("  ", updated.Name);
        Assert.Equal("", updated.Description);
        Assert.Equal(-1.25M, CatalogProtoMapper.ToDecimal(updated.Price));
        Assert.Equal("", updated.PictureFilename);
        Assert.Equal("Visual Studio", updated.CatalogBrand.Brand);
        Assert.Equal("Mug", updated.CatalogType.Type);
        await Client.RemoveCatalogItemAsync(new RemoveCatalogItemRequest { CatalogItem = new CatalogItem { Id = created.Id } });
        await RpcAssert.Fault(StatusCode.NotFound, () => Client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = created.Id }).ResponseAsync);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(9999)]
    public async Task UpdateAndRemoveUnknownIdAreNotFound(int id)
    {
        await RpcAssert.Fault(StatusCode.NotFound, () => Client.UpdateCatalogItemAsync(new UpdateCatalogItemRequest { CatalogItem = new CatalogItem { Id = id } }).ResponseAsync);
        await RpcAssert.Fault(StatusCode.NotFound, () => Client.RemoveCatalogItemAsync(new RemoveCatalogItemRequest { CatalogItem = new CatalogItem { Id = id } }).ResponseAsync);
    }

    [Fact]
    public async Task MissingPayloadsAreInvalidArguments()
    {
        await RpcAssert.Fault(StatusCode.InvalidArgument, () => Client.CreateCatalogItemAsync(new CreateCatalogItemRequest()).ResponseAsync);
        await RpcAssert.Fault(StatusCode.InvalidArgument, () => Client.UpdateCatalogItemAsync(new UpdateCatalogItemRequest()).ResponseAsync);
        await RpcAssert.Fault(StatusCode.InvalidArgument, () => Client.RemoveCatalogItemAsync(new RemoveCatalogItemRequest()).ResponseAsync);
        await RpcAssert.Fault(StatusCode.InvalidArgument, () => Client.CreateAvailableStockAsync(new CreateAvailableStockRequest()).ResponseAsync);
        await RpcAssert.Fault(StatusCode.InvalidArgument, () => Client.CreateAvailableStockAsync(new CreateAvailableStockRequest { CatalogItemsStock = new CatalogItemsStock() }).ResponseAsync);
    }

    [Theory]
    [InlineData(0, 1000000000)]
    [InlineData(0, -1000000000)]
    [InlineData(1, -1)]
    [InlineData(-1, 1)]
    public async Task InvalidDecimalIsInvalidArgumentOnCreateAndUpdate(long units, int nanos)
    {
        var item = new CatalogItem { Id = 1, CatalogBrandId = 2, CatalogTypeId = 2, Price = new DecimalValue { Units = units, Nanos = nanos } };
        await RpcAssert.Fault(StatusCode.InvalidArgument, () => Client.CreateCatalogItemAsync(new CreateCatalogItemRequest { CatalogItem = item }).ResponseAsync);
        await RpcAssert.Fault(StatusCode.InvalidArgument, () => Client.UpdateCatalogItemAsync(new UpdateCatalogItemRequest { CatalogItem = item }).ResponseAsync);
    }
}

public sealed class CatalogStockTests(CatalogGrpcTestHost host) : IClassFixture<CatalogGrpcTestHost>
{
    private eShop.Catalog.Grpc.Protos.Catalog.CatalogClient Client => host.Client;

    [Fact]
    public async Task ReadsKnownStockAndReturnsZeroForMissingRowsAndAnyId()
    {
        var known = await Client.GetAvailableStockAsync(new GetAvailableStockRequest { CatalogItemId = 1, Date = RpcAssert.Date(2017, 9, 21, 23) });
        Assert.Equal(120, known.AvailableStock);
        foreach (int id in new[] { -1, 0, 9999, 1 })
        {
            var missing = await Client.GetAvailableStockAsync(new GetAvailableStockRequest { CatalogItemId = id, Date = RpcAssert.Date(2020, 1, 1) });
            Assert.Equal(0, missing.AvailableStock);
        }
    }

    [Fact]
    public async Task UpsertUsesItemAndDateOnlyAndAssignsMaxPlusOne()
    {
        foreach (int value in new[] { 17, -3 })
        {
            await Client.CreateAvailableStockAsync(new CreateAvailableStockRequest
            {
                CatalogItemsStock = new CatalogItemsStock { StockId = 9999, CatalogItemId = -5, Date = RpcAssert.Date(2020, 2, 3, value == 17 ? 1 : 23), AvailableStock = value },
            });
        }

        Assert.Equal(-3, (await Client.GetAvailableStockAsync(new GetAvailableStockRequest { CatalogItemId = -5, Date = RpcAssert.Date(2020, 2, 3) })).AvailableStock);
        await using var db = host.CreateContext();
        var rows = await db.CatalogItemsStocks.Where(x => x.CatalogItemId == -5).ToListAsync();
        var row = Assert.Single(rows);
        Assert.Equal(7, row.StockId);
        Assert.Equal(new DateTime(2020, 2, 3), row.Date);
    }

    [Fact]
    public async Task ExistingStockIsOverwrittenWithoutAnotherRow()
    {
        await Client.CreateAvailableStockAsync(new CreateAvailableStockRequest
        {
            CatalogItemsStock = new CatalogItemsStock { CatalogItemId = 1, Date = RpcAssert.Date(2017, 9, 20, 12), AvailableStock = 5 },
        });
        Assert.Equal(5, (await Client.GetAvailableStockAsync(new GetAvailableStockRequest { CatalogItemId = 1, Date = RpcAssert.Date(2017, 9, 20) })).AvailableStock);
        await using var db = host.CreateContext();
        Assert.Single(await db.CatalogItemsStocks.Where(x => x.CatalogItemId == 1 && x.Date == new DateTime(2017, 9, 20)).ToListAsync());
    }
}

internal static class RpcAssert
{
    internal static async Task Fault(StatusCode expected, Func<Task> call)
    {
        var exception = await Assert.ThrowsAsync<RpcException>(call);
        Assert.Equal(expected, exception.StatusCode);
    }

    internal static Timestamp Date(int year, int month, int day, int hour = 0) =>
        Timestamp.FromDateTime(new DateTime(year, month, day, hour, 0, 0, DateTimeKind.Utc));
}
