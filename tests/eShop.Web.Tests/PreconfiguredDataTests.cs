using eShop.Catalog.Data;
using Xunit;

namespace eShop.Web.Tests;

/// <summary>
/// Port of the legacy MSTest <c>eShopLegacyMVC.Tests.PreconfiguredDataTests</c> against the
/// modernized <see cref="PreconfiguredData"/>.
/// </summary>
public class PreconfiguredDataTests
{
    [Fact]
    public void GetPreconfiguredCatalogItems_Returns12Items() =>
        Assert.Equal(12, PreconfiguredData.GetPreconfiguredCatalogItems().Count);

    [Fact]
    public void GetPreconfiguredCatalogItems_AllHaveUniqueIds()
    {
        var items = PreconfiguredData.GetPreconfiguredCatalogItems();

        Assert.Equal(items.Count, items.Select(i => i.Id).Distinct().Count());
    }

    [Fact]
    public void GetPreconfiguredCatalogItems_AllHaveNames()
    {
        foreach (var item in PreconfiguredData.GetPreconfiguredCatalogItems())
        {
            Assert.False(string.IsNullOrEmpty(item.Name), $"Item {item.Id} has no name");
        }
    }

    [Fact]
    public void GetPreconfiguredCatalogItems_AllHavePositivePrices()
    {
        foreach (var item in PreconfiguredData.GetPreconfiguredCatalogItems())
        {
            Assert.True(item.Price > 0, $"Item {item.Id} ({item.Name}) has non-positive price: {item.Price}");
        }
    }

    [Fact]
    public void GetPreconfiguredCatalogItems_AllHavePictureFileNames()
    {
        foreach (var item in PreconfiguredData.GetPreconfiguredCatalogItems())
        {
            Assert.False(string.IsNullOrEmpty(item.PictureFileName), $"Item {item.Id} has no picture file name");
        }
    }

    [Fact]
    public void GetPreconfiguredCatalogItems_AllReferenceValidBrandIds()
    {
        var brandIds = PreconfiguredData.GetPreconfiguredCatalogBrands().Select(b => b.Id).ToHashSet();

        foreach (var item in PreconfiguredData.GetPreconfiguredCatalogItems())
        {
            Assert.True(brandIds.Contains(item.CatalogBrandId), $"Item {item.Id} references invalid BrandId {item.CatalogBrandId}");
        }
    }

    [Fact]
    public void GetPreconfiguredCatalogItems_AllReferenceValidTypeIds()
    {
        var typeIds = PreconfiguredData.GetPreconfiguredCatalogTypes().Select(t => t.Id).ToHashSet();

        foreach (var item in PreconfiguredData.GetPreconfiguredCatalogItems())
        {
            Assert.True(typeIds.Contains(item.CatalogTypeId), $"Item {item.Id} references invalid TypeId {item.CatalogTypeId}");
        }
    }

    [Fact]
    public void GetPreconfiguredCatalogBrands_Returns5Brands() =>
        Assert.Equal(5, PreconfiguredData.GetPreconfiguredCatalogBrands().Count);

    [Fact]
    public void GetPreconfiguredCatalogTypes_Returns4Types() =>
        Assert.Equal(4, PreconfiguredData.GetPreconfiguredCatalogTypes().Count);

    [Fact]
    public void GetPreconfiguredCatalogItems_AllHavePositiveStock()
    {
        foreach (var item in PreconfiguredData.GetPreconfiguredCatalogItems())
        {
            Assert.True(item.AvailableStock >= 0, $"Item {item.Id} has negative stock: {item.AvailableStock}");
        }
    }
}
