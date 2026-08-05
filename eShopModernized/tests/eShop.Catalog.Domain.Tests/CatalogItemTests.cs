using eShop.Catalog.Domain.Entities;

namespace eShop.Catalog.Domain.Tests;

public class CatalogItemTests
{
    [Fact]
    public void Constructor_DefaultsPictureFileNameToDummyPng()
    {
        var item = new CatalogItem();

        Assert.Equal("dummy.png", item.PictureFileName);
        Assert.Equal(CatalogItem.DefaultPictureName, item.PictureFileName);
    }

    [Fact]
    public void Constructor_LeavesNumericAndFlagDefaultsAtZero()
    {
        var item = new CatalogItem();

        Assert.Equal(0, item.Id);
        Assert.Equal(0m, item.Price);
        Assert.Equal(0, item.CatalogBrandId);
        Assert.Equal(0, item.CatalogTypeId);
        Assert.Equal(0, item.AvailableStock);
        Assert.Equal(0, item.RestockThreshold);
        Assert.Equal(0, item.MaxStockThreshold);
        Assert.False(item.OnReorder);
        Assert.Null(item.CatalogBrand);
        Assert.Null(item.CatalogType);
    }

    [Fact]
    public void Properties_RoundTripAssignedValues()
    {
        var brand = new CatalogBrand { Id = 1, Brand = "Azure" };
        var type = new CatalogType { Id = 2, Type = "Mug" };

        var item = new CatalogItem
        {
            Id = 7,
            Name = ".NET Bot Black Hoodie",
            Description = "Hoodie",
            Price = 19.5m,
            PictureFileName = "7.png",
            PictureUri = "http://localhost/items/7/pic",
            CatalogBrandId = brand.Id,
            CatalogBrand = brand,
            CatalogTypeId = type.Id,
            CatalogType = type,
            AvailableStock = 100,
            RestockThreshold = 10,
            MaxStockThreshold = 200,
            OnReorder = true,
        };

        Assert.Equal(7, item.Id);
        Assert.Equal(".NET Bot Black Hoodie", item.Name);
        Assert.Equal("Hoodie", item.Description);
        Assert.Equal(19.5m, item.Price);
        Assert.Equal("7.png", item.PictureFileName);
        Assert.Equal("http://localhost/items/7/pic", item.PictureUri);
        Assert.Same(brand, item.CatalogBrand);
        Assert.Same(type, item.CatalogType);
        Assert.Equal(100, item.AvailableStock);
        Assert.Equal(10, item.RestockThreshold);
        Assert.Equal(200, item.MaxStockThreshold);
        Assert.True(item.OnReorder);
    }

    [Fact]
    public void Type_HasNoEntityFrameworkDependency()
    {
        var referenced = typeof(CatalogItem).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToArray();

        Assert.DoesNotContain("EntityFramework", referenced);
        Assert.DoesNotContain("System.Data.Entity", referenced);
    }
}
