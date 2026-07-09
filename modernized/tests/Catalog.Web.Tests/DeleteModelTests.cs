using Catalog.Web.Pages.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Catalog.Web.Tests;

public class DeleteModelTests : IDisposable
{
    private readonly CatalogDbContextFixture _fixture = new();

    private DeleteModel CreateSut() =>
        new(_fixture.CreateContext(), NullLogger<DeleteModel>.Instance);

    [Fact]
    public async Task OnGet_Loads_Item_For_Confirmation()
    {
        var sut = CreateSut();

        var result = await sut.OnGetAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Equal(".NET Bot Black Hoodie", sut.ProductToDelete.Name);
        Assert.Equal(".NET", sut.ProductToDelete.CatalogBrand?.Brand);
    }

    [Fact]
    public async Task OnGet_Returns_NotFound_For_Unknown_Id()
    {
        var sut = CreateSut();

        var result = await sut.OnGetAsync(99999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPost_Removes_Item_And_Redirects()
    {
        var sut = CreateSut();

        var result = await sut.OnPostAsync(1);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Index", redirect.PageName);

        using var context = _fixture.CreateContext();
        Assert.Equal(11, await context.CatalogItems.CountAsync());
        Assert.False(await context.CatalogItems.AnyAsync(i => i.Id == 1));
    }

    public void Dispose() => _fixture.Dispose();
}
