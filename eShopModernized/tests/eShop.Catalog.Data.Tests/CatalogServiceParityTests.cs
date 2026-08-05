using eShop.Catalog.Data.Services;
using eShop.Catalog.Domain;
using eShop.Catalog.Domain.Abstractions;
using eShop.Catalog.Domain.Entities;

namespace eShop.Catalog.Data.Tests;

/// <summary>
/// The mock and the EF Core implementation are the two halves of the legacy UseMockData switch,
/// so their read paths must return identical data for the preconfigured catalog.
/// </summary>
public class CatalogServiceParityTests : IDisposable
{
    private readonly SqliteCatalogDatabase _database = new();
    private readonly CatalogDbContext _context;
    private readonly ICatalogService _efService;
    private readonly ICatalogService _mockService = new CatalogServiceMock();

    public CatalogServiceParityTests()
    {
        _context = _database.CreateSeededContext();
        _efService = new CatalogService(_context);
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(5, 1)]
    [InlineData(3, 3)]
    [InlineData(20, 0)]
    public void PaginatedItems_AreIdentical(int pageSize, int pageIndex)
    {
        AssertSamePage(
            _mockService.GetCatalogItemsPaginated(pageSize, pageIndex),
            _efService.GetCatalogItemsPaginated(pageSize, pageIndex));
    }

    [Fact]
    public async Task PaginatedItems_AreIdenticalForTheAsyncOverloads()
    {
        AssertSamePage(
            await _mockService.GetCatalogItemsPaginatedAsync(),
            await _efService.GetCatalogItemsPaginatedAsync());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(12)]
    public void FindCatalogItem_IsIdentical(int id)
    {
        var expected = _mockService.FindCatalogItem(id);
        var actual = _efService.FindCatalogItem(id);

        Assert.NotNull(expected);
        Assert.NotNull(actual);
        AssertSameItem(expected, actual);
    }

    [Fact]
    public void CatalogBrands_AreIdentical()
    {
        Assert.Equal(
            _mockService.GetCatalogBrands().Select(b => (b.Id, b.Brand)),
            _efService.GetCatalogBrands().Select(b => (b.Id, b.Brand)));
    }

    [Fact]
    public void CatalogTypes_AreIdentical()
    {
        Assert.Equal(
            _mockService.GetCatalogTypes().Select(t => (t.Id, t.Type)),
            _efService.GetCatalogTypes().Select(t => (t.Id, t.Type)));
    }

    private static void AssertSamePage(PaginatedItemsViewModel<CatalogItem> expected, PaginatedItemsViewModel<CatalogItem> actual)
    {
        Assert.Equal(expected.ActualPage, actual.ActualPage);
        Assert.Equal(expected.ItemsPerPage, actual.ItemsPerPage);
        Assert.Equal(expected.TotalItems, actual.TotalItems);
        Assert.Equal(expected.TotalPages, actual.TotalPages);

        var expectedItems = expected.Data.ToList();
        var actualItems = actual.Data.ToList();

        Assert.Equal(expectedItems.Count, actualItems.Count);
        for (var i = 0; i < expectedItems.Count; i++)
        {
            AssertSameItem(expectedItems[i], actualItems[i]);
        }
    }

    private static void AssertSameItem(CatalogItem expected, CatalogItem actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Price, actual.Price);
        Assert.Equal(expected.PictureFileName, actual.PictureFileName);
        Assert.Equal(expected.AvailableStock, actual.AvailableStock);
        Assert.Equal(expected.CatalogBrandId, actual.CatalogBrandId);
        Assert.Equal(expected.CatalogTypeId, actual.CatalogTypeId);
        Assert.Equal(expected.CatalogBrand!.Brand, actual.CatalogBrand!.Brand);
        Assert.Equal(expected.CatalogType!.Type, actual.CatalogType!.Type);
    }

    public void Dispose()
    {
        _context.Dispose();
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}
