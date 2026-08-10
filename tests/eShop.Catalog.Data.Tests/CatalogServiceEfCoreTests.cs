using eShop.Catalog.Data;
using eShop.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eShop.Catalog.Data.Tests;

/// <summary>
/// The EF Core implementation of <see cref="ICatalogService"/> against a real (SQLite) database:
/// CRUD, eager-loaded navigation properties and the legacy pagination semantics. A fresh database
/// is created for every test because xUnit instantiates the class per test.
/// </summary>
public class CatalogServiceEfCoreTests : IDisposable
{
    private readonly CatalogDbContextFixture _fixture = new();
    private readonly CatalogDbContext _context;
    private readonly ICatalogService _service;

    public CatalogServiceEfCoreTests()
    {
        _context = _fixture.CreateContext();
        _service = _fixture.CreateService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _fixture.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void GetCatalogItemsPaginated_ReturnsTheFirstPageAndTheTotalCount()
    {
        var page = _service.GetCatalogItemsPaginated(pageSize: 10, pageIndex: 0);

        Assert.Equal(12, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(10, page.Data.Count());
        Assert.Equal(Enumerable.Range(1, 10), page.Data.Select(i => i.Id));
    }

    [Fact]
    public void GetCatalogItemsPaginated_SkipsWholePages()
    {
        var page = _service.GetCatalogItemsPaginated(pageSize: 10, pageIndex: 1);

        Assert.Equal(12, page.TotalItems);
        Assert.Equal(new[] { 11, 12 }, page.Data.Select(i => i.Id));
    }

    [Fact]
    public void GetCatalogItemsPaginated_EagerLoadsBrandAndType()
    {
        var first = _service.GetCatalogItemsPaginated(10, 0).Data.First();

        Assert.Equal(".NET", first.CatalogBrand?.Brand);
        Assert.Equal("T-Shirt", first.CatalogType?.Type);
    }

    [Fact]
    public void FindCatalogItem_EagerLoadsBrandAndType_AndReturnsNullWhenMissing()
    {
        var item = _service.FindCatalogItem(3);

        Assert.NotNull(item);
        Assert.Equal("Prism White T-Shirt", item.Name);
        Assert.Equal("Other", item.CatalogBrand?.Brand);
        Assert.Equal("T-Shirt", item.CatalogType?.Type);
        Assert.Null(_service.FindCatalogItem(9999));
    }

    [Fact]
    public void GetCatalogItems_FiltersByBrandAndType()
    {
        var dotNetMugs = _service.GetCatalogItems(brandIdFilter: 2, typeIdFilter: 1);

        Assert.Equal(new[] { 2 }, dotNetMugs.Select(i => i.Id));
        Assert.Equal(12, _service.GetCatalogItems(0, 0).Count);
    }

    [Fact]
    public void CreateCatalogItem_PersistsWithAnApplicationAssignedId()
    {
        var item = new CatalogItem
        {
            Name = "New item",
            CatalogBrandId = 1,
            CatalogTypeId = 1,
            Price = 9.99M,
            PictureFileName = "13.png",
        };

        _service.CreateCatalogItem(item);

        Assert.Equal(13, item.Id);

        using var verification = _fixture.CreateContext();
        var persisted = verification.CatalogItems.Single(i => i.Id == 13);
        Assert.Equal("New item", persisted.Name);
        Assert.Equal(9.99M, persisted.Price);
    }

    [Fact]
    public void UpdateCatalogItem_PersistsTheModifiedValues()
    {
        var item = _service.FindCatalogItem(1)!;
        item.Name = "Renamed";
        item.Price = 1.23M;

        _service.UpdateCatalogItem(item);

        using var verification = _fixture.CreateContext();
        var persisted = verification.CatalogItems.Single(i => i.Id == 1);
        Assert.Equal("Renamed", persisted.Name);
        Assert.Equal(1.23M, persisted.Price);
    }

    [Fact]
    public void RemoveCatalogItem_DeletesTheRow()
    {
        var item = _service.FindCatalogItem(1)!;

        _service.RemoveCatalogItem(item);

        using var verification = _fixture.CreateContext();
        Assert.Null(verification.CatalogItems.SingleOrDefault(i => i.Id == 1));
        Assert.Equal(11, verification.CatalogItems.Count());
    }

    [Fact]
    public void GetCatalogBrandsAndTypes_ReturnTheReferenceData()
    {
        Assert.Equal(5, _service.GetCatalogBrands().Count());
        Assert.Equal(4, _service.GetCatalogTypes().Count());
    }

    [Fact]
    public void WcfOnlyEntities_RoundTripThroughTheContext()
    {
        using var context = _fixture.CreateContext();

        var stock = context.CatalogItemsStocks.Single();
        Assert.Equal(42, stock.AvailableStock);
        Assert.Equal(new DateTime(2026, 1, 1), stock.Date);

        var discount = context.DiscountItems.Single();
        Assert.Equal(0.25, discount.Size);
        Assert.Equal(new DateTime(2026, 12, 31), discount.End);
    }

    [Fact]
    public void PictureUri_IsNotMappedToAColumn()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(CatalogItem))!;

        Assert.Null(entityType.FindProperty(nameof(CatalogItem.PictureUri)));
    }
}
