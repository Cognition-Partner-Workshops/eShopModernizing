using Catalog.Domain;
using Catalog.Mvc.Controllers;
using Catalog.Mvc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Catalog.LegacyPorted.Tests;

/// <summary>
/// Ported from the legacy MSTest <c>eShopLegacyMVC.Tests.CatalogControllerTests</c>.
/// The legacy tests drove the MVC 5 controller through a mocked
/// <c>ICatalogService</c>; the modernized <see cref="CatalogController"/> talks
/// to EF Core directly, so these ports exercise it over a seeded in-memory Sqlite
/// <c>CatalogDbContext</c> instead of a Moq'd service. Behavioral intent is
/// preserved: the same actions return the same result kinds
/// (View/BadRequest/NotFound/RedirectToAction) and persist/omit the same data.
/// Assertions that referenced the legacy 2-item mock dataset are re-expressed
/// against the equivalent 12-item seed baseline.
/// </summary>
public class PortedCatalogControllerTests : IDisposable
{
    private readonly SeededSqliteFixture _fixture = new();

    private CatalogController CreateSut() =>
        new(_fixture.CreateContext(), NullLogger<CatalogController>.Instance);

    private static CatalogIndexViewModel IndexModel(IActionResult result) =>
        Assert.IsType<CatalogIndexViewModel>(Assert.IsType<ViewResult>(result).Model);

    [Fact]
    public async Task Index_ReturnsViewWithPaginatedItems()
    {
        var model = IndexModel(await CreateSut().Index(pageSize: 10, pageIndex: 0));

        // Legacy mock exposed 2 items; the modernized seed baseline has 12.
        Assert.Equal(12, model.Items.TotalItems);
        Assert.Equal(10, model.Items.Data.Count());
    }

    [Fact]
    public async Task Index_UsesDefaultPageSizeOf10()
    {
        var model = IndexModel(await CreateSut().Index());

        Assert.Equal(10, model.Items.ItemsPerPage);
        Assert.Equal(0, model.Items.ActualPage);
    }

    [Fact]
    public async Task Details_ExistingId_ReturnsViewWithItem()
    {
        var result = await CreateSut().Details(1);

        var model = Assert.IsType<CatalogItem>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(".NET Bot Black Hoodie", model.Name);
    }

    [Fact]
    public async Task Details_NullId_ReturnsBadRequest()
    {
        Assert.IsType<BadRequestResult>(await CreateSut().Details(null));
    }

    [Fact]
    public async Task Details_NonExistingId_ReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await CreateSut().Details(999));
    }

    [Fact]
    public async Task Create_Get_ReturnsViewWithSelectLists()
    {
        var sut = CreateSut();

        var result = await sut.Create();

        // Modernized Create GET returns a CatalogItemInputModel (the legacy view
        // model was the EF entity itself); the brand/type SelectLists live on
        // ViewBag as before.
        Assert.IsType<CatalogItemInputModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.NotNull(sut.ViewBag.CatalogBrandId);
        Assert.NotNull(sut.ViewBag.CatalogTypeId);
    }

    [Fact]
    public async Task Create_Post_ValidModel_RedirectsToIndex()
    {
        var input = new CatalogItemInputModel
        {
            Name = "New Item",
            Description = "Desc",
            Price = 15.0M,
            CatalogTypeId = 1,
            CatalogBrandId = 1,
            PictureFileName = "test.png",
        };

        var result = await CreateSut().Create(input);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CatalogController.Index), redirect.ActionName);

        using var context = _fixture.CreateContext();
        Assert.Equal(13, await context.CatalogItems.CountAsync());
        Assert.True(await context.CatalogItems.AnyAsync(i => i.Name == "New Item"));
    }

    [Fact]
    public async Task Create_Post_InvalidModel_ReturnsView()
    {
        var sut = CreateSut();
        sut.ModelState.AddModelError("Name", "Required");

        var result = await sut.Create(new CatalogItemInputModel());

        Assert.IsType<ViewResult>(result);
        using var context = _fixture.CreateContext();
        Assert.Equal(12, await context.CatalogItems.CountAsync());
    }

    [Fact]
    public async Task Edit_Get_ExistingId_ReturnsViewWithItem()
    {
        var sut = CreateSut();

        var result = await sut.Edit(1);

        var model = Assert.IsType<CatalogItemInputModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(".NET Bot Black Hoodie", model.Name);
        Assert.NotNull(sut.ViewBag.CatalogBrandId);
        Assert.NotNull(sut.ViewBag.CatalogTypeId);
    }

    [Fact]
    public async Task Edit_Get_NullId_ReturnsBadRequest()
    {
        Assert.IsType<BadRequestResult>(await CreateSut().Edit((int?)null));
    }

    [Fact]
    public async Task Edit_Get_NonExistingId_ReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await CreateSut().Edit(999));
    }

    [Fact]
    public async Task Edit_Post_ValidModel_RedirectsToIndex()
    {
        var input = new CatalogItemInputModel
        {
            Id = 1,
            Name = "Updated",
            Price = 25.0M,
            CatalogTypeId = 1,
            CatalogBrandId = 1,
            PictureFileName = "1.png",
        };

        var result = await CreateSut().Edit(input);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CatalogController.Index), redirect.ActionName);

        using var context = _fixture.CreateContext();
        var updated = await context.CatalogItems.SingleAsync(i => i.Id == 1);
        Assert.Equal("Updated", updated.Name);
        Assert.Equal(25.0M, updated.Price);
    }

    [Fact]
    public async Task Edit_Post_InvalidModel_ReturnsView()
    {
        var sut = CreateSut();
        sut.ModelState.AddModelError("Price", "Invalid");

        var result = await sut.Edit(new CatalogItemInputModel { Id = 1 });

        Assert.IsType<ViewResult>(result);
        using var context = _fixture.CreateContext();
        Assert.Equal(".NET Bot Black Hoodie", (await context.CatalogItems.SingleAsync(i => i.Id == 1)).Name);
    }

    [Fact]
    public async Task Delete_Get_ExistingId_ReturnsViewWithItem()
    {
        var result = await CreateSut().Delete(1);

        var model = Assert.IsType<CatalogItem>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(".NET Bot Black Hoodie", model.Name);
    }

    [Fact]
    public async Task Delete_Get_NullId_ReturnsBadRequest()
    {
        Assert.IsType<BadRequestResult>(await CreateSut().Delete(null));
    }

    [Fact]
    public async Task Delete_Get_NonExistingId_ReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(await CreateSut().Delete(999));
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesItemAndRedirects()
    {
        var result = await CreateSut().DeleteConfirmed(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CatalogController.Index), redirect.ActionName);

        using var context = _fixture.CreateContext();
        Assert.Equal(11, await context.CatalogItems.CountAsync());
        Assert.False(await context.CatalogItems.AnyAsync(i => i.Id == 1));
    }

    public void Dispose() => _fixture.Dispose();
}
