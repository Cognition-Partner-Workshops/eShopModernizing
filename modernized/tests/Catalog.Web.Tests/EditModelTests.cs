using Catalog.Web.Models;
using Catalog.Web.Pages.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Catalog.Web.Tests;

public class EditModelTests : IDisposable
{
    private readonly CatalogDbContextFixture _fixture = new();

    private EditModel CreateSut() =>
        new(_fixture.CreateContext(), NullLogger<EditModel>.Instance);

    [Fact]
    public async Task OnGet_Loads_Existing_Item_Into_Input()
    {
        var sut = CreateSut();

        var result = await sut.OnGetAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Equal(1, sut.Input.Id);
        Assert.Equal(".NET Bot Black Hoodie", sut.Input.Name);
        Assert.Equal("1.png", sut.Input.PictureFileName);
    }

    [Fact]
    public async Task OnGet_Returns_NotFound_For_Unknown_Id()
    {
        var sut = CreateSut();

        var result = await sut.OnGetAsync(99999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPost_Updates_Item_And_Redirects()
    {
        var sut = CreateSut();
        sut.Input = new CatalogItemInputModel
        {
            Id = 1,
            Name = "Renamed Hoodie",
            Description = "Updated description",
            CatalogBrandId = 3,
            CatalogTypeId = 3,
            Price = 99.99M,
            PictureFileName = "1.png",
            AvailableStock = 7,
            RestockThreshold = 2,
            MaxStockThreshold = 20,
        };

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Index", redirect.PageName);

        using var context = _fixture.CreateContext();
        var updated = await context.CatalogItems.SingleAsync(i => i.Id == 1);
        Assert.Equal("Renamed Hoodie", updated.Name);
        Assert.Equal(99.99M, updated.Price);
        Assert.Equal(3, updated.CatalogBrandId);
        Assert.Equal(3, updated.CatalogTypeId);
    }

    public void Dispose() => _fixture.Dispose();
}
