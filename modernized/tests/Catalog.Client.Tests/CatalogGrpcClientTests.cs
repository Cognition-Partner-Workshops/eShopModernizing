using Catalog.Client.Protos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Xunit;

namespace Catalog.Client.Tests;

/// <summary>
/// Exercises <see cref="CatalogGrpcClient"/> end-to-end over the full gRPC
/// pipeline: the real Catalog.Service host runs in-process (via
/// <see cref="CatalogServiceFactory"/>) against a seeded Sqlite database, and the
/// client talks to it through a <see cref="GrpcChannel"/> backed by the test
/// server handler. Assertions are pinned to the same behavioral baseline as the
/// legacy WCF client (5 brands, 4 types, 12 items, seeded stock/discounts).
/// </summary>
public class CatalogGrpcClientTests : IDisposable
{
    private readonly CatalogServiceFactory _factory;
    private readonly GrpcChannel _channel;
    private readonly CatalogGrpcClient _client;

    public CatalogGrpcClientTests()
    {
        _factory = new CatalogServiceFactory();

        // Force the in-process host to start so the handler is available.
        _ = _factory.Server;

        _channel = GrpcChannel.ForAddress(
            _factory.Server.BaseAddress,
            new GrpcChannelOptions { HttpHandler = _factory.Server.CreateHandler() });

        _client = new CatalogGrpcClient(_channel);
    }

    private static DateTime Day(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GetCatalogBrands_Returns_Five_Seeded_Brands()
    {
        var brands = _client.GetCatalogBrands();

        Assert.Equal(5, brands.Count);
        Assert.Equal(
            new[] { "Azure", ".NET", "Visual Studio", "SQL Server", "Other" },
            brands.OrderBy(b => b.Id).Select(b => b.Brand));
    }

    [Fact]
    public void GetCatalogTypes_Returns_Four_Seeded_Types()
    {
        var types = _client.GetCatalogTypes();

        Assert.Equal(4, types.Count);
        Assert.Equal(
            new[] { "Mug", "T-Shirt", "Sheet", "USB Memory Stick" },
            types.OrderBy(t => t.Id).Select(t => t.Type));
    }

    [Fact]
    public void FindCatalogItem_Returns_Item_With_Navigations()
    {
        var item = _client.FindCatalogItem(1);

        Assert.NotNull(item);
        Assert.Equal(1, item!.Id);
        Assert.Equal(".NET Bot Black Hoodie", item.Name);
        Assert.Equal(19.5m, (decimal)item.Price);
        Assert.Equal("1.png", item.PictureFileName);
        Assert.NotNull(item.CatalogBrand);
        Assert.NotNull(item.CatalogType);
        Assert.Equal(".NET", item.CatalogBrand.Brand);
        Assert.Equal("T-Shirt", item.CatalogType.Type);
    }

    [Fact]
    public void FindCatalogItem_Unknown_Id_Returns_Null()
    {
        var item = _client.FindCatalogItem(99999);

        Assert.Null(item);
    }

    [Fact]
    public void GetCatalogItems_No_Filter_Returns_All_Twelve()
    {
        var items = _client.GetCatalogItems(0, 0);

        Assert.Equal(12, items.Count);
    }

    [Fact]
    public void GetCatalogItems_Brand_Filter_Applies()
    {
        var items = _client.GetCatalogItems(2, 0);

        Assert.Equal(6, items.Count);
        Assert.All(items, i => Assert.Equal(2, i.CatalogBrandId));
    }

    [Fact]
    public void GetCatalogItems_Type_Filter_Applies()
    {
        var items = _client.GetCatalogItems(0, 2);

        Assert.Equal(7, items.Count);
        Assert.All(items, i => Assert.Equal(2, i.CatalogTypeId));
    }

    [Fact]
    public void GetCatalogItems_Brand_And_Type_Filter_Applies()
    {
        var items = _client.GetCatalogItems(2, 2);

        Assert.Equal(3, items.Count);
        Assert.All(items, i =>
        {
            Assert.Equal(2, i.CatalogBrandId);
            Assert.Equal(2, i.CatalogTypeId);
        });
    }

    [Theory]
    [InlineData(2017, 9, 20, 1, 100)]
    [InlineData(2017, 9, 21, 1, 120)]
    [InlineData(2017, 9, 22, 1, 80)]
    [InlineData(2017, 9, 20, 2, 45)]
    [InlineData(2017, 9, 25, 4, 65)]
    public void GetAvailableStock_Returns_Seeded_Stock(int y, int m, int d, int itemId, int expected)
    {
        var stock = _client.GetAvailableStock(Day(y, m, d), itemId);

        Assert.Equal(expected, stock);
    }

    [Fact]
    public void GetAvailableStock_No_Row_For_Date_Returns_Zero()
    {
        Assert.Equal(0, _client.GetAvailableStock(Day(2017, 9, 25), 1));
    }

    [Fact]
    public void GetAvailableStock_Unknown_Item_Returns_Zero()
    {
        Assert.Equal(0, _client.GetAvailableStock(Day(2017, 9, 20), 99999));
    }

    [Fact]
    public void CreateAvailableStock_Inserts_New_Row()
    {
        _client.CreateAvailableStock(new CatalogItemsStock
        {
            CatalogItemId = 3,
            Date = Timestamp.FromDateTime(Day(2017, 10, 1)),
            AvailableStock = 55,
        });

        Assert.Equal(55, _client.GetAvailableStock(Day(2017, 10, 1), 3));
    }

    [Fact]
    public void CreateAvailableStock_Overwrites_Existing_Row_For_Date()
    {
        _client.CreateAvailableStock(new CatalogItemsStock
        {
            CatalogItemId = 1,
            Date = Timestamp.FromDateTime(Day(2017, 9, 21)),
            AvailableStock = 7,
        });

        Assert.Equal(7, _client.GetAvailableStock(Day(2017, 9, 21), 1));
    }

    [Fact]
    public void CreateCatalogItem_Persists_New_Item()
    {
        _client.CreateCatalogItem(new CatalogItem
        {
            Name = "New Widget",
            Description = "A brand new widget",
            Price = 3.25m,
            PictureFileName = "new.png",
            CatalogTypeId = 1,
            CatalogBrandId = 1,
            AvailableStock = 10,
        });

        var items = _client.GetCatalogItems(0, 0);
        Assert.Equal(13, items.Count);

        var created = items.Single(i => i.Name == "New Widget");
        Assert.True(created.Id > 0);
        Assert.Equal(3.25m, (decimal)created.Price);
    }

    [Fact]
    public void UpdateCatalogItem_Persists_Changes()
    {
        var item = _client.FindCatalogItem(2);
        Assert.NotNull(item);

        item!.Name = "Renamed Mug";
        item.Price = 9.99m;
        item.CatalogBrand = null;
        item.CatalogType = null;

        _client.UpdateCatalogItem(item);

        var updated = _client.FindCatalogItem(2);
        Assert.NotNull(updated);
        Assert.Equal("Renamed Mug", updated!.Name);
        Assert.Equal(9.99m, (decimal)updated.Price);
    }

    [Fact]
    public void RemoveCatalogItem_Deletes_Item()
    {
        _client.RemoveCatalogItem(new CatalogItem { Id = 5 });

        Assert.Null(_client.FindCatalogItem(5));
        Assert.Equal(11, _client.GetCatalogItems(0, 0).Count);
    }

    [Theory]
    [InlineData(2017, 9, 19, 1, 0.3)]
    [InlineData(2017, 9, 20, 1, 0.3)]
    [InlineData(2017, 9, 24, 2, 0.25)]
    [InlineData(2017, 10, 10, 4, 0.5)]
    public void GetDiscount_Returns_Discount_Covering_Day(int y, int m, int d, int expectedId, double expectedSize)
    {
        var discount = _client.GetDiscount(Day(y, m, d));

        Assert.NotNull(discount);
        Assert.Equal(expectedId, discount!.Id);
        Assert.Equal(expectedSize, discount.Size, 3);
    }

    [Fact]
    public void GetDiscount_No_Discount_For_Day_Returns_Null()
    {
        Assert.Null(_client.GetDiscount(Day(2017, 10, 1)));
    }

    public void Dispose()
    {
        _client.Dispose();
        _channel.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
