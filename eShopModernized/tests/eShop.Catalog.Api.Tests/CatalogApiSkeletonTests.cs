namespace eShop.Catalog.Api.Tests;

public class CatalogApiSkeletonTests
{
    [Fact]
    public void ApiAssembly_IsReferencedAndNamedCorrectly()
    {
        var assembly = typeof(Program).Assembly;

        Assert.Equal("eShop.Catalog.Api", assembly.GetName().Name);
    }
}
