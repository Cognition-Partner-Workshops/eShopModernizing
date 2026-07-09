using Catalog.Domain;
using Catalog.Mvc.Controllers;
using Catalog.Mvc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Catalog.Mvc.Tests;

/// <summary>
/// Parity tests for the ported MVC <see cref="CatalogController"/>, covering the
/// baseline paging (size 10 / index 0, ordered by Id), the brand/type filters,
/// and the Details/Create/Edit/Delete CRUD actions.
/// </summary>
public class CatalogControllerTests : IDisposable
{
    private readonly CatalogDbContextFixture _fixture = new();

    private CatalogController CreateSut() =>
        new(_fixture.CreateContext(), NullLogger<CatalogController>.Instance);

    private static CatalogIndexViewModel IndexModel(IActionResult result) =>
        Assert.IsType<CatalogIndexViewModel>(Assert.IsType<ViewResult>(result).Model);

    [Fact]
    public void Defaults_Match_Legacy_Baseline()
    {
        Assert.Equal(0, CatalogController.DefaultPageIndex);
        Assert.Equal(10, CatalogController.DefaultPageSize);
    }

    [Fact]
    public async Task Index_Default_Returns_First_Page_Of_Ten_Ordered_By_Id()
    {
        var sut = CreateSut();

        var model = IndexModel(await sut.Index());

        Assert.Equal(12, model.Items.TotalItems);
        Assert.Equal(2, model.Items.TotalPages);
        Assert.Equal(10, model.Items.ItemsPerPage);
        Assert.Equal(0, model.Items.ActualPage);

        var ids = model.Items.Data.Select(i => i.Id).ToArray();
        Assert.Equal(Enumerable.Range(1, 10).ToArray(), ids);
    }

    [Fact]
    public async Task Index_Pages_With_Skip_Take()
    {
        var sut = CreateSut();

        var model = IndexModel(await sut.Index(pageSize: 5, pageIndex: 1));

        var ids = model.Items.Data.Select(i => i.Id).ToArray();
        Assert.Equal(new[] { 6, 7, 8, 9, 10 }, ids);
        Assert.Equal(3, model.Items.TotalPages);
        Assert.Equal(12, model.Items.TotalItems);
    }

    [Fact]
    public async Task Index_NonPositive_PageSize_Falls_Back_To_Default()
    {
        var sut = CreateSut();

        var model = IndexModel(await sut.Index(pageSize: 0, pageIndex: -3));

        Assert.Equal(10, model.Items.ItemsPerPage);
        Assert.Equal(0, model.Items.ActualPage);
        Assert.Equal(10, model.Items.Data.Count());
    }

    [Fact]
    public async Task Index_Loads_Brand_And_Type_Navigation()
    {
        var sut = CreateSut();

        var model = IndexModel(await sut.Index());

        Assert.All(model.Items.Data, item =>
        {
            Assert.NotNull(item.CatalogBrand);
            Assert.NotNull(item.CatalogType);
        });
    }

    [Fact]
    public async Task Index_Filters_By_Brand()
    {
        var sut = CreateSut();

        var model = IndexModel(await sut.Index(brandFilter: 2)); // .NET

        Assert.Equal(6, model.Items.TotalItems);
        Assert.All(model.Items.Data, item => Assert.Equal(2, item.CatalogBrandId));
    }

    [Fact]
    public async Task Index_Filters_By_Type()
    {
        var sut = CreateSut();

        var model = IndexModel(await sut.Index(typeFilter: 1)); // Mug

        Assert.Equal(2, model.Items.TotalItems);
        Assert.All(model.Items.Data, item => Assert.Equal(1, item.CatalogTypeId));
    }

    [Fact]
    public async Task Index_Filters_By_Brand_And_Type_Combined()
    {
        var sut = CreateSut();

        var model = IndexModel(await sut.Index(brandFilter: 2, typeFilter: 1));

        Assert.Equal(1, model.Items.TotalItems);
        var item = Assert.Single(model.Items.Data);
        Assert.Equal(2, item.CatalogBrandId);
        Assert.Equal(1, item.CatalogTypeId);
    }

    [Fact]
    public async Task Index_Populates_Filter_Dropdowns()
    {
        var sut = CreateSut();

        var model = IndexModel(await sut.Index());

        Assert.Equal(5, model.Brands.Count());
        Assert.Equal(4, model.Types.Count());
    }

    [Fact]
    public async Task Details_Returns_Item_With_Navigation()
    {
        var sut = CreateSut();

        var result = await sut.Details(1);

        var item = Assert.IsType<CatalogItem>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(".NET Bot Black Hoodie", item.Name);
        Assert.Equal(".NET", item.CatalogBrand?.Brand);
    }

    [Fact]
    public async Task Details_Null_Id_Returns_BadRequest()
    {
        var sut = CreateSut();

        Assert.IsType<BadRequestResult>(await sut.Details(null));
    }

    [Fact]
    public async Task Details_Unknown_Id_Returns_NotFound()
    {
        var sut = CreateSut();

        Assert.IsType<NotFoundResult>(await sut.Details(99999));
    }

    [Fact]
    public async Task Create_Get_Populates_Dropdowns()
    {
        var sut = CreateSut();

        var result = await sut.Create();

        Assert.IsType<ViewResult>(result);
        Assert.NotNull(sut.ViewBag.CatalogBrandId);
        Assert.NotNull(sut.ViewBag.CatalogTypeId);
    }

    [Fact]
    public async Task Create_Post_Adds_Item_And_Redirects_To_Index()
    {
        var sut = CreateSut();
        var input = new CatalogItemInputModel
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

        var result = await sut.Create(input);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CatalogController.Index), redirect.ActionName);

        using var context = _fixture.CreateContext();
        Assert.Equal(13, await context.CatalogItems.CountAsync());
        var created = await context.CatalogItems.SingleAsync(i => i.Name == "New Widget");
        Assert.Equal(42.75M, created.Price);
        Assert.Equal(1, created.CatalogBrandId);
        Assert.Equal(1, created.CatalogTypeId);
    }

    [Fact]
    public async Task Create_Post_Invalid_Model_Does_Not_Persist()
    {
        var sut = CreateSut();
        sut.ModelState.AddModelError(nameof(CatalogItemInputModel.Name), "The Name field is required.");

        var result = await sut.Create(new CatalogItemInputModel { Name = string.Empty });

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<CatalogItemInputModel>(view.Model);
        using var context = _fixture.CreateContext();
        Assert.Equal(12, await context.CatalogItems.CountAsync());
    }

    [Fact]
    public async Task Edit_Get_Returns_Populated_Input_Model()
    {
        var sut = CreateSut();

        var result = await sut.Edit(1);

        var input = Assert.IsType<CatalogItemInputModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(1, input.Id);
        Assert.Equal(".NET Bot Black Hoodie", input.Name);
        Assert.NotNull(sut.ViewBag.CatalogBrandId);
    }

    [Fact]
    public async Task Edit_Get_Unknown_Id_Returns_NotFound()
    {
        var sut = CreateSut();

        Assert.IsType<NotFoundResult>(await sut.Edit(99999));
    }

    [Fact]
    public async Task Edit_Post_Updates_Item_And_Redirects()
    {
        var sut = CreateSut();
        var input = new CatalogItemInputModel
        {
            Id = 1,
            Name = "Updated Hoodie",
            Description = "Updated description",
            CatalogBrandId = 3,
            CatalogTypeId = 2,
            Price = 99.99M,
            PictureFileName = "1.png",
            AvailableStock = 7,
            RestockThreshold = 2,
            MaxStockThreshold = 20,
        };

        var result = await sut.Edit(input);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CatalogController.Index), redirect.ActionName);

        using var context = _fixture.CreateContext();
        var updated = await context.CatalogItems.SingleAsync(i => i.Id == 1);
        Assert.Equal("Updated Hoodie", updated.Name);
        Assert.Equal(3, updated.CatalogBrandId);
        Assert.Equal(99.99M, updated.Price);
    }

    [Fact]
    public async Task Edit_Post_Invalid_Model_Does_Not_Persist()
    {
        var sut = CreateSut();
        sut.ModelState.AddModelError(nameof(CatalogItemInputModel.Name), "The Name field is required.");

        var result = await sut.Edit(new CatalogItemInputModel { Id = 1, Name = string.Empty });

        Assert.IsType<ViewResult>(result);
        using var context = _fixture.CreateContext();
        Assert.Equal(".NET Bot Black Hoodie", (await context.CatalogItems.SingleAsync(i => i.Id == 1)).Name);
    }

    [Fact]
    public async Task Delete_Get_Loads_Item_For_Confirmation()
    {
        var sut = CreateSut();

        var result = await sut.Delete(1);

        var item = Assert.IsType<CatalogItem>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(".NET Bot Black Hoodie", item.Name);
        Assert.Equal(".NET", item.CatalogBrand?.Brand);
    }

    [Fact]
    public async Task Delete_Get_Unknown_Id_Returns_NotFound()
    {
        var sut = CreateSut();

        Assert.IsType<NotFoundResult>(await sut.Delete(99999));
    }

    [Fact]
    public async Task DeleteConfirmed_Post_Removes_Item_And_Redirects()
    {
        var sut = CreateSut();

        var result = await sut.DeleteConfirmed(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CatalogController.Index), redirect.ActionName);

        using var context = _fixture.CreateContext();
        Assert.Equal(11, await context.CatalogItems.CountAsync());
        Assert.False(await context.CatalogItems.AnyAsync(i => i.Id == 1));
    }

    public void Dispose() => _fixture.Dispose();
}
