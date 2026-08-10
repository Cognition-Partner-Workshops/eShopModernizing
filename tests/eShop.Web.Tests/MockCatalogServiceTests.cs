using eShop.Catalog.Data;
using eShop.Catalog.Domain;
using Xunit;

namespace eShop.Web.Tests;

/// <summary>
/// Port of the legacy MSTest <c>eShopLegacyMVC.Tests.CatalogServiceMockTests</c> against the
/// modernized <see cref="MockCatalogService"/>.
/// </summary>
public class MockCatalogServiceTests
{
    private readonly MockCatalogService _service = new();

    [Fact]
    public void GetCatalogItemsPaginated_ReturnsCorrectPageSize()
    {
        var result = _service.GetCatalogItemsPaginated(5, 0);

        Assert.Equal(5, result.Data.Count());
        Assert.Equal(5, result.ItemsPerPage);
        Assert.Equal(0, result.ActualPage);
    }

    [Fact]
    public void GetCatalogItemsPaginated_ReturnsCorrectTotalItems()
    {
        var result = _service.GetCatalogItemsPaginated(10, 0);

        Assert.Equal(12, result.TotalItems);
    }

    [Fact]
    public void GetCatalogItemsPaginated_SecondPageReturnsRemainingItems()
    {
        var result = _service.GetCatalogItemsPaginated(10, 1);

        Assert.Equal(2, result.Data.Count());
        Assert.Equal(1, result.ActualPage);
    }

    [Fact]
    public void GetCatalogItemsPaginated_ItemsAreOrderedById()
    {
        var ids = _service.GetCatalogItemsPaginated(12, 0).Data.Select(i => i.Id).ToList();

        for (var i = 1; i < ids.Count; i++)
        {
            Assert.True(ids[i] > ids[i - 1], $"Items not ordered by Id: {ids[i - 1]} should be less than {ids[i]}");
        }
    }

    [Fact]
    public void GetCatalogItemsPaginated_ItemsHaveBrandsPopulated()
    {
        foreach (var item in _service.GetCatalogItemsPaginated(12, 0).Data)
        {
            Assert.NotNull(item.CatalogBrand);
            Assert.False(string.IsNullOrEmpty(item.CatalogBrand!.Brand));
        }
    }

    [Fact]
    public void GetCatalogItemsPaginated_ItemsHaveTypesPopulated()
    {
        foreach (var item in _service.GetCatalogItemsPaginated(12, 0).Data)
        {
            Assert.NotNull(item.CatalogType);
            Assert.False(string.IsNullOrEmpty(item.CatalogType!.Type));
        }
    }

    [Fact]
    public void FindCatalogItem_ExistingId_ReturnsItem()
    {
        var item = _service.FindCatalogItem(1);

        Assert.NotNull(item);
        Assert.Equal(1, item!.Id);
        Assert.Equal(".NET Bot Black Hoodie", item.Name);
    }

    [Fact]
    public void FindCatalogItem_NonExistingId_ReturnsNull() =>
        Assert.Null(_service.FindCatalogItem(999));

    [Fact]
    public void GetCatalogTypes_ReturnsAllTypes()
    {
        var types = _service.GetCatalogTypes().ToList();

        Assert.Equal(4, types.Count);
        Assert.Contains(types, t => t.Type == "Mug");
        Assert.Contains(types, t => t.Type == "T-Shirt");
        Assert.Contains(types, t => t.Type == "Sheet");
        Assert.Contains(types, t => t.Type == "USB Memory Stick");
    }

    [Fact]
    public void GetCatalogBrands_ReturnsAllBrands()
    {
        var brands = _service.GetCatalogBrands().ToList();

        Assert.Equal(5, brands.Count);
        Assert.Contains(brands, b => b.Brand == "Azure");
        Assert.Contains(brands, b => b.Brand == ".NET");
        Assert.Contains(brands, b => b.Brand == "Other");
    }

    [Fact]
    public void CreateCatalogItem_AssignsNewId()
    {
        var newItem = new CatalogItem
        {
            Name = "Test Item",
            Description = "Test",
            Price = 9.99M,
            CatalogTypeId = 1,
            CatalogBrandId = 1,
            AvailableStock = 50,
        };

        _service.CreateCatalogItem(newItem);

        Assert.Equal(13, newItem.Id);
        Assert.Equal(13, _service.GetCatalogItemsPaginated(20, 0).TotalItems);
    }

    [Fact]
    public void CreateCatalogItem_ItemIsRetrievable()
    {
        var newItem = new CatalogItem
        {
            Name = "Retrievable Item",
            Description = "Should be findable",
            Price = 5.00M,
            CatalogTypeId = 1,
            CatalogBrandId = 1,
        };

        _service.CreateCatalogItem(newItem);
        var found = _service.FindCatalogItem(newItem.Id);

        Assert.NotNull(found);
        Assert.Equal("Retrievable Item", found!.Name);
    }

    [Fact]
    public void UpdateCatalogItem_ModifiesExistingItem()
    {
        var item = _service.FindCatalogItem(1)!;
        item.Name = "Updated Name";
        item.Price = 99.99M;

        _service.UpdateCatalogItem(item);
        var updated = _service.FindCatalogItem(1);

        Assert.Equal("Updated Name", updated!.Name);
        Assert.Equal(99.99M, updated.Price);
    }

    [Fact]
    public void UpdateCatalogItem_NonExistingItem_DoesNotThrow() =>
        _service.UpdateCatalogItem(new CatalogItem { Id = 999, Name = "Ghost Item", Price = 1.00M });

    [Fact]
    public void RemoveCatalogItem_DecreasesCount()
    {
        var item = _service.FindCatalogItem(1)!;

        _service.RemoveCatalogItem(item);

        Assert.Equal(11, _service.GetCatalogItemsPaginated(20, 0).TotalItems);
        Assert.Null(_service.FindCatalogItem(1));
    }
}
