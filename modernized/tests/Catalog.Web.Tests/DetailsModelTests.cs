using Catalog.Web.Pages.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Catalog.Web.Tests;

public class DetailsModelTests : IDisposable
{
    private readonly CatalogDbContextFixture _fixture = new();

    private DetailsModel CreateSut() =>
        new(_fixture.CreateContext(), NullLogger<DetailsModel>.Instance);

    [Fact]
    public async Task OnGet_Returns_The_Requested_Item_With_Navigation()
    {
        var sut = CreateSut();

        var result = await sut.OnGetAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Equal(".NET Bot Black Hoodie", sut.Product.Name);
        Assert.Equal(".NET", sut.Product.CatalogBrand?.Brand);
        Assert.Equal("T-Shirt", sut.Product.CatalogType?.Type);
        Assert.Equal(19.5M, sut.Product.Price);
    }

    [Fact]
    public async Task OnGet_Returns_NotFound_For_Unknown_Id()
    {
        var sut = CreateSut();

        var result = await sut.OnGetAsync(99999);

        Assert.IsType<NotFoundResult>(result);
    }

    public void Dispose() => _fixture.Dispose();
}
