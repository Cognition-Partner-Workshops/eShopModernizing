using eShop.Catalog.Domain;
using eShop.Web.Configuration;
using eShop.Web.Controllers;
using eShop.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace eShop.Web.Tests;

/// <summary>
/// Port of the legacy MSTest <c>eShopLegacyMVC.Tests.CatalogControllerTests</c>. The legacy
/// <c>ViewBag.CatalogBrandId</c> / <c>ViewBag.CatalogTypeId</c> assertions became assertions on the
/// <see cref="CatalogItemFormViewModel"/> select lists that replaced them.
/// </summary>
public class CatalogControllerTests
{
    private readonly RecordingCatalogService _service = new();
    private readonly CatalogController _controller;
    private readonly List<CatalogItem> _items;

    public CatalogControllerTests()
    {
        _items =
        [
            new() { Id = 1, Name = "Item 1", Price = 10.0M, CatalogBrandId = 1, CatalogTypeId = 1 },
            new() { Id = 2, Name = "Item 2", Price = 20.0M, CatalogBrandId = 2, CatalogTypeId = 2 },
        ];
        _items[0].CatalogBrand = _service.Brands[0];
        _items[0].CatalogType = _service.Types[0];
        _items[1].CatalogBrand = _service.Brands[1];
        _items[1].CatalogType = _service.Types[1];

        _controller = new CatalogController(
            _service,
            Options.Create(new CatalogWebOptions()),
            NullLogger<CatalogController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Request = { Scheme = "http", Host = new HostString("localhost") },
                },
            },
        };
    }

    [Fact]
    public void Index_ReturnsViewWithPaginatedItems()
    {
        _service.Page = new PaginatedItems<CatalogItem>(0, 10, 2, _items);

        var result = Assert.IsType<ViewResult>(_controller.Index(10, 0));

        var model = Assert.IsType<PaginatedItems<CatalogItem>>(result.Model);
        Assert.Equal(2, model.TotalItems);
        Assert.Equal(2, model.Data.Count());
    }

    [Fact]
    public void Index_UsesDefaultPageSizeOf10()
    {
        _controller.Index();

        Assert.Equal([(10, 0)], _service.PaginatedCalls);
    }

    [Fact]
    public void Details_ExistingId_ReturnsViewWithItem()
    {
        _service.Items.Add(_items[0]);

        var result = Assert.IsType<ViewResult>(_controller.Details(1));

        var model = Assert.IsType<CatalogItem>(result.Model);
        Assert.Equal("Item 1", model.Name);
    }

    [Fact]
    public void Details_NullId_ReturnsBadRequest() =>
        Assert.IsType<BadRequestResult>(_controller.Details(null));

    [Fact]
    public void Details_NonExistingId_ReturnsNotFound() =>
        Assert.IsType<NotFoundResult>(_controller.Details(999));

    [Fact]
    public void Create_Get_ReturnsViewWithSelectLists()
    {
        var result = Assert.IsType<ViewResult>(_controller.Create());

        var model = Assert.IsType<CatalogItemFormViewModel>(result.Model);
        Assert.NotEmpty(model.Brands);
        Assert.NotEmpty(model.Types);
        Assert.IsType<CatalogItem>(model.Item);
    }

    [Fact]
    public void Create_Post_ValidModel_RedirectsToIndex()
    {
        var newItem = new CatalogItem
        {
            Name = "New Item",
            Description = "Desc",
            Price = 15.0M,
            CatalogTypeId = 1,
            CatalogBrandId = 1,
            PictureFileName = "test.png",
        };

        var result = Assert.IsType<RedirectToActionResult>(_controller.Create(newItem));

        Assert.Equal("Index", result.ActionName);
        Assert.Equal([newItem], _service.Created);
    }

    [Fact]
    public void Create_Post_InvalidModel_ReturnsView()
    {
        _controller.ModelState.AddModelError("Name", "Required");

        var result = Assert.IsType<ViewResult>(_controller.Create(new CatalogItem()));

        Assert.IsType<CatalogItemFormViewModel>(result.Model);
        Assert.Empty(_service.Created);
    }

    [Fact]
    public void Edit_Get_ExistingId_ReturnsViewWithItem()
    {
        _service.Items.Add(_items[0]);

        var result = Assert.IsType<ViewResult>(_controller.Edit(1));

        var model = Assert.IsType<CatalogItemFormViewModel>(result.Model);
        Assert.Equal("Item 1", model.Item.Name);
        Assert.NotEmpty(model.Brands);
        Assert.NotEmpty(model.Types);
    }

    [Fact]
    public void Edit_Get_NullId_ReturnsBadRequest() =>
        Assert.IsType<BadRequestResult>(_controller.Edit((int?)null));

    [Fact]
    public void Edit_Get_NonExistingId_ReturnsNotFound() =>
        Assert.IsType<NotFoundResult>(_controller.Edit(999));

    [Fact]
    public void Edit_Post_ValidModel_RedirectsToIndex()
    {
        var item = new CatalogItem
        {
            Id = 1,
            Name = "Updated",
            Price = 25.0M,
            CatalogTypeId = 1,
            CatalogBrandId = 1,
            PictureFileName = "1.png",
        };

        var result = Assert.IsType<RedirectToActionResult>(_controller.Edit(item));

        Assert.Equal("Index", result.ActionName);
        Assert.Equal([item], _service.Updated);
    }

    [Fact]
    public void Edit_Post_InvalidModel_ReturnsView()
    {
        _controller.ModelState.AddModelError("Price", "Invalid");

        var result = Assert.IsType<ViewResult>(_controller.Edit(new CatalogItem { Id = 1 }));

        Assert.IsType<CatalogItemFormViewModel>(result.Model);
        Assert.Empty(_service.Updated);
    }

    [Fact]
    public void Delete_Get_ExistingId_ReturnsViewWithItem()
    {
        _service.Items.Add(_items[0]);

        var result = Assert.IsType<ViewResult>(_controller.Delete(1));

        var model = Assert.IsType<CatalogItem>(result.Model);
        Assert.Equal("Item 1", model.Name);
    }

    [Fact]
    public void Delete_Get_NullId_ReturnsBadRequest() =>
        Assert.IsType<BadRequestResult>(_controller.Delete(null));

    [Fact]
    public void Delete_Get_NonExistingId_ReturnsNotFound() =>
        Assert.IsType<NotFoundResult>(_controller.Delete(999));

    [Fact]
    public void DeleteConfirmed_RemovesItemAndRedirects()
    {
        var item = _items[0];
        _service.Items.Add(item);

        var result = Assert.IsType<RedirectToActionResult>(_controller.DeleteConfirmed(1));

        Assert.Equal("Index", result.ActionName);
        Assert.Equal([item], _service.Removed);
    }

    [Fact]
    public void Index_SetsTheAbsolutePictureUriOnEveryItem()
    {
        _service.Page = new PaginatedItems<CatalogItem>(0, 10, 2, _items);

        _controller.Index(10, 0);

        Assert.Equal("http://localhost/items/1/pic", _items[0].PictureUri);
        Assert.Equal("http://localhost/items/2/pic", _items[1].PictureUri);
    }
}
