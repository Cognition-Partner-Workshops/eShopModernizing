using eShop.Catalog.Data.Infrastructure;
using eShop.Catalog.Data.Services;
using eShop.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Data.Tests;

public class CatalogServiceTests : IDisposable
{
    private readonly SqliteCatalogDatabase _database = new();

    [Fact]
    public async Task GetCatalogItemsPaginated_UsesLegacyDefaults()
    {
        await using var context = _database.CreateSeededContext();
        var service = new CatalogService(context);

        var page = await service.GetCatalogItemsPaginatedAsync();

        Assert.Equal(0, page.ActualPage);
        Assert.Equal(10, page.ItemsPerPage);
        Assert.Equal(12, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(10, page.Data.Count());
    }

    [Fact]
    public async Task GetCatalogItemsPaginated_SkipsAndTakesTheRequestedPage()
    {
        await using var context = _database.CreateSeededContext();
        var service = new CatalogService(context);

        var page = await service.GetCatalogItemsPaginatedAsync(pageSize: 5, pageIndex: 2);

        Assert.Equal(12, page.TotalItems);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal([11, 12], page.Data.Select(i => i.Id));
    }

    [Fact]
    public async Task GetCatalogItemsPaginated_LoadsBrandAndTypeNavigations()
    {
        await using var context = _database.CreateSeededContext();
        var service = new CatalogService(context);

        var page = await service.GetCatalogItemsPaginatedAsync(pageSize: 3, pageIndex: 0);

        Assert.All(page.Data, item =>
        {
            Assert.NotNull(item.CatalogBrand);
            Assert.NotNull(item.CatalogType);
            Assert.Equal(item.CatalogBrandId, item.CatalogBrand!.Id);
            Assert.Equal(item.CatalogTypeId, item.CatalogType!.Id);
        });
    }

    [Fact]
    public async Task GetCatalogItemsPaginated_SyncAndAsyncAgree()
    {
        await using var context = _database.CreateSeededContext();
        var service = new CatalogService(context);

        var sync = service.GetCatalogItemsPaginated(pageSize: 4, pageIndex: 1);
        var async = await service.GetCatalogItemsPaginatedAsync(pageSize: 4, pageIndex: 1);

        Assert.Equal(sync.TotalItems, async.TotalItems);
        Assert.Equal(sync.TotalPages, async.TotalPages);
        Assert.Equal(sync.Data.Select(i => i.Id), async.Data.Select(i => i.Id));
    }

    [Fact]
    public async Task FindCatalogItem_ReturnsItemWithNavigations()
    {
        await using var context = _database.CreateSeededContext();
        var service = new CatalogService(context);

        var item = await service.FindCatalogItemAsync(3);

        Assert.NotNull(item);
        Assert.Equal("Prism White T-Shirt", item.Name);
        Assert.Equal("Other", item.CatalogBrand!.Brand);
        Assert.Equal("T-Shirt", item.CatalogType!.Type);
    }

    [Fact]
    public async Task FindCatalogItem_ReturnsNullForUnknownId()
    {
        await using var context = _database.CreateSeededContext();
        var service = new CatalogService(context);

        Assert.Null(await service.FindCatalogItemAsync(4711));
    }

    [Fact]
    public async Task CreateUpdateAndRemoveCatalogItem_PersistThroughTheContext()
    {
        await using var context = _database.CreateSeededContext();
        var service = new CatalogService(context);

        var item = new CatalogItem
        {
            Id = 100,
            Name = "Devin Sticker",
            Description = "Devin Sticker",
            Price = 3.5M,
            PictureFileName = "100.png",
            CatalogBrandId = 1,
            CatalogTypeId = 1,
        };

        await service.CreateCatalogItemAsync(item);
        Assert.Equal(100, item.Id);
        Assert.Equal(13, (await service.GetCatalogItemsPaginatedAsync(pageSize: 20)).TotalItems);

        item.Name = "Devin Sticker Pack";
        await service.UpdateCatalogItemAsync(item);
        Assert.Equal("Devin Sticker Pack", (await service.FindCatalogItemAsync(100))!.Name);

        await service.RemoveCatalogItemAsync(item);
        Assert.Null(await service.FindCatalogItemAsync(100));
    }

    [Fact]
    public async Task GetCatalogBrandsAndTypes_ReturnEveryRow()
    {
        await using var context = _database.CreateSeededContext();
        var service = new CatalogService(context);

        Assert.Equal(5, (await service.GetCatalogBrandsAsync()).Count());
        Assert.Equal(4, (await service.GetCatalogTypesAsync()).Count());
    }

    [Fact]
    public async Task GetCatalogItemsPaginated_CountsAllRowsNotJustThePage()
    {
        await using var context = _database.CreateSeededContext();
        var service = new CatalogService(context);

        var page = await service.GetCatalogItemsPaginatedAsync(pageSize: 3, pageIndex: 0);

        Assert.Equal(await context.CatalogItems.LongCountAsync(), page.TotalItems);
        Assert.Equal(3, page.Data.Count());
    }

    [Fact]
    public void Mock_UsesThePreconfiguredData()
    {
        var mock = new CatalogServiceMock();

        var page = mock.GetCatalogItemsPaginated();

        Assert.Equal(PreconfiguredData.GetPreconfiguredCatalogItems().Count, page.TotalItems);
        Assert.Equal(10, page.Data.Count());
    }

    [Fact]
    public void Mock_CreateAssignsTheNextId()
    {
        var mock = new CatalogServiceMock();

        var item = new CatalogItem { Name = "New", Price = 1M, PictureFileName = "n.png", CatalogBrandId = 1, CatalogTypeId = 1 };
        mock.CreateCatalogItem(item);

        Assert.Equal(13, item.Id);
        Assert.Equal("New", mock.FindCatalogItem(13)!.Name);

        mock.RemoveCatalogItem(item);
        Assert.Null(mock.FindCatalogItem(13));
    }

    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}
