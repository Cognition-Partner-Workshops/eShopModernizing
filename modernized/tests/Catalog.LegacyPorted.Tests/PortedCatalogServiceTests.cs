using Catalog.Mvc.Controllers;
using Catalog.Mvc.Models;
using Catalog.Service.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Proto = Catalog.Service.Protos;

namespace Catalog.LegacyPorted.Tests;

/// <summary>
/// Ported from the legacy MSTest <c>eShopLegacyMVC.Tests.CatalogServiceMockTests</c>.
/// The legacy <c>CatalogServiceMock</c> was a single in-memory implementation of
/// <c>ICatalogService</c> covering both paging and CRUD. In the modernized stack
/// those responsibilities were split:
///   * catalog CRUD / lookups map to the gRPC <see cref="CatalogGrpcService"/>
///     (the direct WCF-service replacement), exercised here over a seeded Sqlite
///     <c>CatalogDbContext</c>; and
///   * server-side paging moved out of the service into the presentation layer,
///     so the paging-oriented tests map to <see cref="CatalogController.Index"/>,
///     the closest modernized equivalent of <c>GetCatalogItemsPaginated</c>.
/// Each mapping is annotated inline. Assertions preserve the legacy intent.
/// </summary>
public class PortedCatalogServiceTests : IDisposable
{
    private readonly SeededSqliteFixture _fixture = new();

    private CatalogGrpcService CreateService() => new(_fixture.CreateContext());

    private CatalogController CreateController() =>
        new(_fixture.CreateContext(), NullLogger<CatalogController>.Instance);

    private async Task<PaginatedItemsViewModel<Catalog.Domain.CatalogItem>> PageAsync(int pageSize, int pageIndex)
    {
        var result = await CreateController().Index(pageSize: pageSize, pageIndex: pageIndex);
        var model = Assert.IsType<CatalogIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        return model.Items;
    }

    // ----- Paging (legacy GetCatalogItemsPaginated -> modernized MVC Index) -----

    [Fact]
    public async Task GetCatalogItemsPaginated_ReturnsCorrectPageSize()
    {
        var page = await PageAsync(5, 0);

        Assert.Equal(5, page.Data.Count());
        Assert.Equal(5, page.ItemsPerPage);
        Assert.Equal(0, page.ActualPage);
    }

    [Fact]
    public async Task GetCatalogItemsPaginated_ReturnsCorrectTotalItems()
    {
        var page = await PageAsync(10, 0);

        Assert.Equal(12, page.TotalItems);
    }

    [Fact]
    public async Task GetCatalogItemsPaginated_SecondPageReturnsRemainingItems()
    {
        var page = await PageAsync(10, 1);

        Assert.Equal(2, page.Data.Count());
        Assert.Equal(1, page.ActualPage);
    }

    [Fact]
    public async Task GetCatalogItemsPaginated_ItemsAreOrderedById()
    {
        var page = await PageAsync(12, 0);
        var ids = page.Data.Select(i => i.Id).ToList();

        for (int i = 1; i < ids.Count; i++)
        {
            Assert.True(ids[i] > ids[i - 1], $"Items not ordered by Id: {ids[i - 1]} should be less than {ids[i]}");
        }
    }

    [Fact]
    public async Task GetCatalogItemsPaginated_ItemsHaveBrandsPopulated()
    {
        var page = await PageAsync(12, 0);

        foreach (var item in page.Data)
        {
            Assert.NotNull(item.CatalogBrand);
            Assert.False(string.IsNullOrEmpty(item.CatalogBrand!.Brand));
        }
    }

    [Fact]
    public async Task GetCatalogItemsPaginated_ItemsHaveTypesPopulated()
    {
        var page = await PageAsync(12, 0);

        foreach (var item in page.Data)
        {
            Assert.NotNull(item.CatalogType);
            Assert.False(string.IsNullOrEmpty(item.CatalogType!.Type));
        }
    }

    // ----- Lookups / CRUD (legacy CatalogServiceMock -> modernized gRPC service) -----

    [Fact]
    public async Task FindCatalogItem_ExistingId_ReturnsItem()
    {
        var response = await CreateService()
            .FindCatalogItem(new Proto.FindCatalogItemRequest { Id = 1 }, null!);

        Assert.NotNull(response.Item);
        Assert.Equal(1, response.Item.Id);
        Assert.Equal(".NET Bot Black Hoodie", response.Item.Name);
    }

    [Fact]
    public async Task FindCatalogItem_NonExistingId_ReturnsNull()
    {
        var response = await CreateService()
            .FindCatalogItem(new Proto.FindCatalogItemRequest { Id = 999 }, null!);

        Assert.Null(response.Item);
    }

    [Fact]
    public async Task GetCatalogTypes_ReturnsAllTypes()
    {
        var response = await CreateService().GetCatalogTypes(new Google.Protobuf.WellKnownTypes.Empty(), null!);
        var types = response.CatalogTypes.ToList();

        Assert.Equal(4, types.Count);
        Assert.Contains(types, t => t.Type == "Mug");
        Assert.Contains(types, t => t.Type == "T-Shirt");
        Assert.Contains(types, t => t.Type == "Sheet");
        Assert.Contains(types, t => t.Type == "USB Memory Stick");
    }

    [Fact]
    public async Task GetCatalogBrands_ReturnsAllBrands()
    {
        var response = await CreateService().GetCatalogBrands(new Google.Protobuf.WellKnownTypes.Empty(), null!);
        var brands = response.Brands.ToList();

        Assert.Equal(5, brands.Count);
        Assert.Contains(brands, b => b.Brand == "Azure");
        Assert.Contains(brands, b => b.Brand == ".NET");
        Assert.Contains(brands, b => b.Brand == "Other");
    }

    [Fact]
    public async Task CreateCatalogItem_AssignsNewId()
    {
        await CreateService().CreateCatalogItem(new Proto.CatalogItem
        {
            Name = "Test Item",
            Description = "Test",
            Price = 9.99M,
            CatalogTypeId = 1,
            CatalogBrandId = 1,
            AvailableStock = 50,
        }, null!);

        using var context = _fixture.CreateContext();
        Assert.Equal(13, await context.CatalogItems.CountAsync());
        var created = await context.CatalogItems.SingleAsync(i => i.Name == "Test Item");
        Assert.Equal(13, created.Id);
    }

    [Fact]
    public async Task CreateCatalogItem_ItemIsRetrievable()
    {
        await CreateService().CreateCatalogItem(new Proto.CatalogItem
        {
            Name = "Retrievable Item",
            Description = "Should be findable",
            Price = 5.00M,
            CatalogTypeId = 1,
            CatalogBrandId = 1,
        }, null!);

        int newId;
        using (var context = _fixture.CreateContext())
        {
            newId = (await context.CatalogItems.SingleAsync(i => i.Name == "Retrievable Item")).Id;
        }

        var found = await CreateService().FindCatalogItem(new Proto.FindCatalogItemRequest { Id = newId }, null!);

        Assert.NotNull(found.Item);
        Assert.Equal("Retrievable Item", found.Item.Name);
    }

    [Fact]
    public async Task UpdateCatalogItem_ModifiesExistingItem()
    {
        var find = await CreateService().FindCatalogItem(new Proto.FindCatalogItemRequest { Id = 1 }, null!);
        var item = find.Item!;
        item.Name = "Updated Name";
        item.Price = 99.99M;
        // Detach navigations so EF updates only the scalar columns (matches the
        // existing gRPC service test convention and avoids re-inserting brand/type).
        item.CatalogBrand = null;
        item.CatalogType = null;

        await CreateService().UpdateCatalogItem(item, null!);

        using var context = _fixture.CreateContext();
        var updated = await context.CatalogItems.SingleAsync(i => i.Id == 1);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal(99.99M, updated.Price);
    }

    [Fact]
    public async Task UpdateCatalogItem_NonExistingItem_IsHandledGracefully()
    {
        // The legacy mock silently no-op'd when updating an unknown id. The gRPC
        // service now uses EF Core `Update` semantics, which would throw for a
        // missing row; the graceful "unknown id" handling therefore lives in the
        // presentation layer, where Edit returns NotFound without mutating data.
        var sut = CreateController();

        var result = await sut.Edit(new CatalogItemInputModel { Id = 999, Name = "Ghost Item", Price = 1.00M });

        Assert.IsType<NotFoundResult>(result);
        using var context = _fixture.CreateContext();
        Assert.Equal(12, await context.CatalogItems.CountAsync());
    }

    [Fact]
    public async Task RemoveCatalogItem_DecreasesCount()
    {
        await CreateService().RemoveCatalogItem(new Proto.CatalogItem { Id = 1 }, null!);

        using var context = _fixture.CreateContext();
        Assert.Equal(11, await context.CatalogItems.CountAsync());
        Assert.False(await context.CatalogItems.AnyAsync(i => i.Id == 1));
    }

    public void Dispose() => _fixture.Dispose();
}
