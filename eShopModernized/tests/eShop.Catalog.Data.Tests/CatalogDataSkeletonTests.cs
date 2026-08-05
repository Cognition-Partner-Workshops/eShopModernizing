using eShop.Catalog.Data;

namespace eShop.Catalog.Data.Tests;

public class CatalogDataSkeletonTests
{
    [Fact]
    public void DataAssembly_IsReferencedAndNamedCorrectly()
    {
        var assembly = typeof(CatalogDataAssemblyMarker).Assembly;

        Assert.Equal("eShop.Catalog.Data", assembly.GetName().Name);
    }
}
