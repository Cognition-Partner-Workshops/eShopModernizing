using eShop.Web.Controllers;

namespace eShop.Web.Tests;

public class WebSkeletonTests
{
    [Fact]
    public void WebAssembly_IsReferencedAndNamedCorrectly()
    {
        var assembly = typeof(CatalogController).Assembly;

        Assert.Equal("eShop.Web", assembly.GetName().Name);
    }
}
