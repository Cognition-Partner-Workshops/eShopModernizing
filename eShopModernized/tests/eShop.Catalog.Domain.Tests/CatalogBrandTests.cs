using System.Text.Json;
using eShop.Catalog.Domain.Entities;

namespace eShop.Catalog.Domain.Tests;

public class CatalogBrandTests
{
    [Fact]
    public void Defaults_AreIdAndEmptyBrand()
    {
        var brand = new CatalogBrand();

        Assert.Equal(0, brand.Id);
        Assert.Equal(string.Empty, brand.Brand);
    }

    [Fact]
    public void Serializes_WithLegacyBaselineShape()
    {
        var json = JsonSerializer.Serialize(new CatalogBrand { Id = 1, Brand = "Azure" });

        Assert.Equal("{\"Id\":1,\"Brand\":\"Azure\"}", json);
    }
}
