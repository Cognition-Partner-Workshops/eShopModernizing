using System.Text.Json;
using eShop.Catalog.Domain.Entities;

namespace eShop.Catalog.Domain.Tests;

public class CatalogTypeTests
{
    [Fact]
    public void Defaults_AreIdAndEmptyType()
    {
        var type = new CatalogType();

        Assert.Equal(0, type.Id);
        Assert.Equal(string.Empty, type.Type);
    }

    [Fact]
    public void Serializes_WithLegacyBaselineShape()
    {
        var json = JsonSerializer.Serialize(new CatalogType { Id = 2, Type = "Mug" });

        Assert.Equal("{\"Id\":2,\"Type\":\"Mug\"}", json);
    }
}
