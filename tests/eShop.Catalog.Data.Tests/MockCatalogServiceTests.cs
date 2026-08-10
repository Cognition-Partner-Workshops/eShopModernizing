using eShop.Catalog.Data;
using eShop.Catalog.Domain;
using Xunit;

namespace eShop.Catalog.Data.Tests;

public class MockCatalogServiceTests
{
    private readonly ICatalogService _service = new MockCatalogService();

    [Fact]
    public void GetCatalogItemsPaginated_ReturnsTheFirstPageAndTheTotalCount()
    {
        var page = _service.GetCatalogItemsPaginated(pageSize: 10, pageIndex: 0);

        Assert.Equal(12, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(10, page.Data.Count());
        Assert.Equal(1, page.Data.First().Id);
    }

    [Fact]
    public void GetCatalogItemsPaginated_ComposesBrandAndType()
    {
        var first = _service.GetCatalogItemsPaginated(10, 0).Data.First();

        Assert.Equal(".NET", first.CatalogBrand?.Brand);
        Assert.Equal("T-Shirt", first.CatalogType?.Type);
    }

    [Fact]
    public void GetCatalogItems_FiltersByBrandAndType()
    {
        var mugs = _service.GetCatalogItems(brandIdFilter: 0, typeIdFilter: 1);

        Assert.Equal(2, mugs.Count);
        Assert.All(mugs, i => Assert.Equal(1, i.CatalogTypeId));
    }

    [Fact]
    public void CreateCatalogItem_AssignsTheNextId()
    {
        var item = new CatalogItem { Name = "New item", CatalogBrandId = 1, CatalogTypeId = 1 };

        _service.CreateCatalogItem(item);

        Assert.Equal(13, item.Id);
        Assert.Same(item, _service.FindCatalogItem(13));
    }

    [Fact]
    public void RemoveCatalogItem_DropsTheItem()
    {
        var item = _service.FindCatalogItem(1);
        Assert.NotNull(item);

        _service.RemoveCatalogItem(item);

        Assert.Null(_service.FindCatalogItem(1));
    }

    [Fact]
    public void PreconfiguredData_MatchesTheLegacyMockDataSet()
    {
        Assert.Equal(5, _service.GetCatalogBrands().Count());
        Assert.Equal(4, _service.GetCatalogTypes().Count());
    }
}
