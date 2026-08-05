using eShop.Catalog.Domain;
using eShop.Catalog.Domain.Entities;
using eShop.Web.Controllers;
using eShop.Web.Models;
using eShop.Web.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;

namespace eShop.Web.Tests.Legacy;

/// <summary>
/// The legacy <c>eShopLegacyMVC.Tests.CatalogControllerTests</c> MSTest suite ported to xunit
/// against the ASP.NET Core controller. <c>HttpStatusCodeResult</c>/<c>HttpNotFoundResult</c>
/// become <c>BadRequestResult</c>/<c>NotFoundResult</c> and the <c>ViewBag</c> select lists become
/// the typed <see cref="CatalogItemViewModel" /> option lists; every assertion is otherwise kept.
/// </summary>
public class CatalogControllerTests
{
    private readonly RecordingCatalogService _service;
    private readonly CatalogController _controller;
    private readonly List<CatalogBrand> _brands;
    private readonly List<CatalogType> _types;
    private readonly List<CatalogItem> _items;

    public CatalogControllerTests()
    {
        _brands =
        [
            new CatalogBrand { Id = 1, Brand = "Azure" },
            new CatalogBrand { Id = 2, Brand = ".NET" },
        ];

        _types =
        [
            new CatalogType { Id = 1, Type = "Mug" },
            new CatalogType { Id = 2, Type = "T-Shirt" },
        ];

        _items =
        [
            new CatalogItem { Id = 1, Name = "Item 1", Price = 10.0M, CatalogBrandId = 1, CatalogTypeId = 1, CatalogBrand = _brands[0], CatalogType = _types[0] },
            new CatalogItem { Id = 2, Name = "Item 2", Price = 20.0M, CatalogBrandId = 2, CatalogTypeId = 2, CatalogBrand = _brands[1], CatalogType = _types[1] },
        ];

        _service = new RecordingCatalogService
        {
            Brands = _brands,
            Types = _types,
            Items = _items,
        };

        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ControllerActionDescriptor());

        _controller = new CatalogController(_service, NullLogger<CatalogController>.Instance)
        {
            ControllerContext = new ControllerContext(actionContext),
        };

        _controller.Url = new StubUrlHelper(actionContext);
    }

    [Fact]
    public async Task Index_ReturnsViewWithPaginatedItems()
    {
        _service.PaginatedResult = new PaginatedItemsViewModel<CatalogItem>(0, 10, 2, _items);

        var result = await _controller.Index(10, 0) as ViewResult;

        Assert.NotNull(result);
        var model = Assert.IsType<PaginatedItemsViewModel<CatalogItem>>(result.Model);
        Assert.Equal(2, model.TotalItems);
        Assert.Equal(2, model.Data.Count());
    }

    [Fact]
    public async Task Index_UsesDefaultPageSizeOf10()
    {
        _service.PaginatedResult = new PaginatedItemsViewModel<CatalogItem>(0, 10, 0, []);

        await _controller.Index();

        Assert.Equal([(10, 0)], _service.PaginationCalls);
    }

    [Fact]
    public async Task Details_ExistingId_ReturnsViewWithItem()
    {
        var result = await _controller.Details(1) as ViewResult;

        Assert.NotNull(result);
        var model = Assert.IsType<CatalogItem>(result.Model);
        Assert.Equal("Item 1", model.Name);
    }

    [Fact]
    public async Task Details_NullId_ReturnsBadRequest()
    {
        var result = await _controller.Details(null);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Details_NonExistingId_ReturnsNotFound()
    {
        var result = await _controller.Details(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_Get_ReturnsViewWithSelectLists()
    {
        var result = await _controller.Create() as ViewResult;

        Assert.NotNull(result);
        var model = Assert.IsType<CatalogItemViewModel>(result.Model);
        Assert.Equal(["Azure", ".NET"], model.Brands.Select(b => b.Text));
        Assert.Equal(["Mug", "T-Shirt"], model.Types.Select(t => t.Text));
    }

    [Fact]
    public async Task Create_Post_ValidModel_RedirectsToIndex()
    {
        var newItem = new CatalogItemViewModel
        {
            Name = "New Item",
            Description = "Desc",
            Price = 15.0M,
            CatalogTypeId = 1,
            CatalogBrandId = 1,
            PictureFileName = "test.png",
        };

        var result = await _controller.Create(newItem) as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        var created = Assert.Single(_service.Created);
        Assert.Equal("New Item", created.Name);
        Assert.Equal("Desc", created.Description);
        Assert.Equal(15.0M, created.Price);
        Assert.Equal(1, created.CatalogTypeId);
        Assert.Equal(1, created.CatalogBrandId);
        Assert.Equal("test.png", created.PictureFileName);
    }

    [Fact]
    public async Task Create_Post_InvalidModel_ReturnsView()
    {
        _controller.ModelState.AddModelError("Name", "Required");

        var result = await _controller.Create(new CatalogItemViewModel()) as ViewResult;

        Assert.NotNull(result);
        Assert.Empty(_service.Created);
    }

    [Fact]
    public async Task Edit_Get_ExistingId_ReturnsViewWithItem()
    {
        var result = await _controller.Edit(1) as ViewResult;

        Assert.NotNull(result);
        var model = Assert.IsType<CatalogItemViewModel>(result.Model);
        Assert.Equal("Item 1", model.Name);
        Assert.NotEmpty(model.Brands);
        Assert.NotEmpty(model.Types);
    }

    [Fact]
    public async Task Edit_Get_NullId_ReturnsBadRequest()
    {
        var result = await _controller.Edit((int?)null);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Edit_Get_NonExistingId_ReturnsNotFound()
    {
        var result = await _controller.Edit(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_ValidModel_RedirectsToIndex()
    {
        var item = new CatalogItemViewModel
        {
            Id = 1,
            Name = "Updated",
            Price = 25.0M,
            CatalogTypeId = 1,
            CatalogBrandId = 1,
            PictureFileName = "1.png",
        };

        var result = await _controller.Edit(item) as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        var updated = Assert.Single(_service.Updated);
        Assert.Equal(1, updated.Id);
        Assert.Equal("Updated", updated.Name);
        Assert.Equal(25.0M, updated.Price);
        Assert.Equal("1.png", updated.PictureFileName);
    }

    [Fact]
    public async Task Edit_Post_InvalidModel_ReturnsView()
    {
        _controller.ModelState.AddModelError("Price", "Invalid");

        var result = await _controller.Edit(new CatalogItemViewModel { Id = 1 }) as ViewResult;

        Assert.NotNull(result);
        Assert.Empty(_service.Updated);
    }

    [Fact]
    public async Task Delete_Get_ExistingId_ReturnsViewWithItem()
    {
        var result = await _controller.Delete(1) as ViewResult;

        Assert.NotNull(result);
        var model = Assert.IsType<CatalogItem>(result.Model);
        Assert.Equal("Item 1", model.Name);
    }

    [Fact]
    public async Task Delete_Get_NullId_ReturnsBadRequest()
    {
        var result = await _controller.Delete(null);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Delete_Get_NonExistingId_ReturnsNotFound()
    {
        var result = await _controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesItemAndRedirects()
    {
        var result = await _controller.DeleteConfirmed(1) as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Same(_items[0], Assert.Single(_service.Removed));
    }
}
