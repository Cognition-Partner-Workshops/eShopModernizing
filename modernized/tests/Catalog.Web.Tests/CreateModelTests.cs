using Catalog.Web.Models;
using Catalog.Web.Pages.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Catalog.Web.Tests;

public class CreateModelTests : IDisposable
{
    private readonly CatalogDbContextFixture _fixture = new();

    private CreateModel CreateSut() =>
        new(_fixture.CreateContext(), NullLogger<CreateModel>.Instance);

    [Fact]
    public async Task OnPost_Adds_Item_And_Redirects_To_Index()
    {
        var sut = CreateSut();
        sut.Input = new CatalogItemInputModel
        {
            Name = "New Widget",
            Description = "A brand new widget",
            CatalogBrandId = 1,
            CatalogTypeId = 1,
            Price = 42.75M,
            AvailableStock = 5,
            RestockThreshold = 1,
            MaxStockThreshold = 10,
        };

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Index", redirect.PageName);

        using var context = _fixture.CreateContext();
        Assert.Equal(13, await context.CatalogItems.CountAsync());
        var created = await context.CatalogItems.SingleAsync(i => i.Name == "New Widget");
        Assert.Equal(42.75M, created.Price);
        Assert.Equal(1, created.CatalogBrandId);
        Assert.Equal(1, created.CatalogTypeId);
    }

    [Fact]
    public async Task OnPost_Invalid_Model_Does_Not_Persist()
    {
        var sut = CreateSut();
        sut.ModelState.AddModelError("Input.Name", "The Name field is required.");
        sut.Input = new CatalogItemInputModel { Name = string.Empty };

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        using var context = _fixture.CreateContext();
        Assert.Equal(12, await context.CatalogItems.CountAsync());
    }

    public void Dispose() => _fixture.Dispose();
}
