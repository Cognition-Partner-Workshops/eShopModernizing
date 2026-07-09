using Catalog.Infrastructure;
using Catalog.Service.Protos;
using Catalog.Service.Services;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Catalog.Service.Tests;

/// <summary>
/// Exercises <see cref="CatalogGrpcService"/> directly against a seeded EF Core
/// Sqlite in-memory <see cref="CatalogDbContext"/>, asserting each ported WCF
/// operation reproduces the behavioral baseline. Runs on CI with no external DB.
/// </summary>
public class CatalogGrpcServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<CatalogDbContext> _options;

    public CatalogGrpcServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new CatalogDbContext(_options);
        context.Database.EnsureCreated();
    }

    private CatalogDbContext CreateContext() => new(_options);

    private CatalogGrpcService CreateService() => new(CreateContext());

    private static Timestamp Day(int year, int month, int day) =>
        Timestamp.FromDateTime(DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc));

    [Fact]
    public async Task GetCatalogBrands_Returns_Five_Seeded_Brands()
    {
        var response = await CreateService().GetCatalogBrands(new Empty(), null!);

        Assert.Equal(5, response.Brands.Count);
        Assert.Equal(
            new[] { "Azure", ".NET", "Visual Studio", "SQL Server", "Other" },
            response.Brands.OrderBy(b => b.Id).Select(b => b.Brand));
    }

    [Fact]
    public async Task GetCatalogTypes_Returns_Four_Seeded_Types()
    {
        var response = await CreateService().GetCatalogTypes(new Empty(), null!);

        Assert.Equal(4, response.CatalogTypes.Count);
        Assert.Equal(
            new[] { "Mug", "T-Shirt", "Sheet", "USB Memory Stick" },
            response.CatalogTypes.OrderBy(t => t.Id).Select(t => t.Type));
    }

    [Fact]
    public async Task FindCatalogItem_Returns_Item_With_Navigations()
    {
        var response = await CreateService()
            .FindCatalogItem(new FindCatalogItemRequest { Id = 1 }, null!);

        Assert.NotNull(response.Item);
        Assert.Equal(1, response.Item.Id);
        Assert.Equal(".NET Bot Black Hoodie", response.Item.Name);
        Assert.Equal(19.5m, (decimal)response.Item.Price);
        Assert.NotNull(response.Item.CatalogBrand);
        Assert.NotNull(response.Item.CatalogType);
        Assert.Equal(".NET", response.Item.CatalogBrand.Brand);
        Assert.Equal("T-Shirt", response.Item.CatalogType.Type);
    }

    [Fact]
    public async Task FindCatalogItem_Unknown_Id_Returns_Empty()
    {
        var response = await CreateService()
            .FindCatalogItem(new FindCatalogItemRequest { Id = 99999 }, null!);

        Assert.Null(response.Item);
    }

    [Fact]
    public async Task GetCatalogItems_No_Filter_Returns_All_Twelve()
    {
        var response = await CreateService()
            .GetCatalogItems(new GetCatalogItemsRequest { BrandIdFilter = 0, TypeIdFilter = 0 }, null!);

        Assert.Equal(12, response.Items.Count);
    }

    [Fact]
    public async Task GetCatalogItems_Brand_Filter_Applies()
    {
        var response = await CreateService()
            .GetCatalogItems(new GetCatalogItemsRequest { BrandIdFilter = 2, TypeIdFilter = 0 }, null!);

        Assert.Equal(6, response.Items.Count);
        Assert.All(response.Items, i => Assert.Equal(2, i.CatalogBrandId));
    }

    [Fact]
    public async Task GetCatalogItems_Type_Filter_Applies()
    {
        var response = await CreateService()
            .GetCatalogItems(new GetCatalogItemsRequest { BrandIdFilter = 0, TypeIdFilter = 2 }, null!);

        Assert.Equal(7, response.Items.Count);
        Assert.All(response.Items, i => Assert.Equal(2, i.CatalogTypeId));
    }

    [Fact]
    public async Task GetCatalogItems_Brand_And_Type_Filter_Applies()
    {
        var response = await CreateService()
            .GetCatalogItems(new GetCatalogItemsRequest { BrandIdFilter = 2, TypeIdFilter = 2 }, null!);

        Assert.Equal(3, response.Items.Count);
        Assert.All(response.Items, i =>
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
    public async Task GetAvailableStock_Returns_Seeded_Stock(int y, int m, int d, int itemId, int expected)
    {
        var response = await CreateService().GetAvailableStock(
            new GetAvailableStockRequest { Date = Day(y, m, d), CatalogItemId = itemId }, null!);

        Assert.Equal(expected, response.AvailableStock);
    }

    [Fact]
    public async Task GetAvailableStock_No_Row_For_Date_Returns_Zero()
    {
        var response = await CreateService().GetAvailableStock(
            new GetAvailableStockRequest { Date = Day(2017, 9, 25), CatalogItemId = 1 }, null!);

        Assert.Equal(0, response.AvailableStock);
    }

    [Fact]
    public async Task GetAvailableStock_Unknown_Item_Returns_Zero()
    {
        var response = await CreateService().GetAvailableStock(
            new GetAvailableStockRequest { Date = Day(2017, 9, 20), CatalogItemId = 99999 }, null!);

        Assert.Equal(0, response.AvailableStock);
    }

    [Fact]
    public async Task CreateAvailableStock_Inserts_New_Row()
    {
        await CreateService().CreateAvailableStock(new CatalogItemsStock
        {
            CatalogItemId = 3,
            Date = Day(2017, 10, 1),
            AvailableStock = 55,
        }, null!);

        var response = await CreateService().GetAvailableStock(
            new GetAvailableStockRequest { Date = Day(2017, 10, 1), CatalogItemId = 3 }, null!);

        Assert.Equal(55, response.AvailableStock);
    }

    [Fact]
    public async Task CreateAvailableStock_Overwrites_Existing_Row_For_Date()
    {
        await CreateService().CreateAvailableStock(new CatalogItemsStock
        {
            CatalogItemId = 1,
            Date = Day(2017, 9, 21),
            AvailableStock = 7,
        }, null!);

        var response = await CreateService().GetAvailableStock(
            new GetAvailableStockRequest { Date = Day(2017, 9, 21), CatalogItemId = 1 }, null!);

        Assert.Equal(7, response.AvailableStock);

        using var context = CreateContext();
        Assert.Equal(6, context.CatalogItemsStocks.Count());
    }

    [Fact]
    public async Task CreateCatalogItem_Persists_New_Item()
    {
        await CreateService().CreateCatalogItem(new CatalogItem
        {
            Name = "New Widget",
            Description = "A brand new widget",
            Price = 3.25m,
            PictureFileName = "new.png",
            CatalogTypeId = 1,
            CatalogBrandId = 1,
            AvailableStock = 10,
        }, null!);

        using var context = CreateContext();
        Assert.Equal(13, context.CatalogItems.Count());
        var created = context.CatalogItems.Single(i => i.Name == "New Widget");
        Assert.True(created.Id > 0);
        Assert.Equal(3.25m, created.Price);
    }

    [Fact]
    public async Task UpdateCatalogItem_Persists_Changes()
    {
        var find = await CreateService().FindCatalogItem(new FindCatalogItemRequest { Id = 2 }, null!);
        var item = find.Item!;
        item.Name = "Renamed Mug";
        item.Price = 9.99m;
        item.CatalogBrand = null;
        item.CatalogType = null;

        await CreateService().UpdateCatalogItem(item, null!);

        using var context = CreateContext();
        var updated = context.CatalogItems.Single(i => i.Id == 2);
        Assert.Equal("Renamed Mug", updated.Name);
        Assert.Equal(9.99m, updated.Price);
    }

    [Fact]
    public async Task RemoveCatalogItem_Deletes_Item()
    {
        await CreateService().RemoveCatalogItem(new CatalogItem { Id = 5 }, null!);

        using var context = CreateContext();
        Assert.False(context.CatalogItems.Any(i => i.Id == 5));
        Assert.Equal(11, context.CatalogItems.Count());
    }

    [Theory]
    [InlineData(2017, 9, 19, 1, 0.3)]
    [InlineData(2017, 9, 20, 1, 0.3)]
    [InlineData(2017, 9, 24, 2, 0.25)]
    [InlineData(2017, 10, 10, 4, 0.5)]
    public async Task GetDiscount_Returns_Discount_Covering_Day(int y, int m, int d, int expectedId, double expectedSize)
    {
        var response = await CreateService().GetDiscount(new GetDiscountRequest { Day = Day(y, m, d) }, null!);

        Assert.NotNull(response.Discount);
        Assert.Equal(expectedId, response.Discount.Id);
        Assert.Equal(expectedSize, response.Discount.Size, 3);
    }

    [Fact]
    public async Task GetDiscount_No_Discount_For_Day_Returns_Empty()
    {
        var response = await CreateService().GetDiscount(new GetDiscountRequest { Day = Day(2017, 10, 1) }, null!);

        Assert.Null(response.Discount);
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
