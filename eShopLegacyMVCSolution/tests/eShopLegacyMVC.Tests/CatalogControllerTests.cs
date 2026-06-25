using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using eShopLegacyMVC.Controllers;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.Services;
using eShopLegacyMVC.ViewModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eShopLegacyMVC.Tests
{
    [TestClass]
    public class CatalogControllerTests
    {
        private Mock<ICatalogService> _mockService;
        private CatalogController _controller;

        private List<CatalogBrand> _brands;
        private List<CatalogType> _types;
        private List<CatalogItem> _items;

        [TestInitialize]
        public void Setup()
        {
            _mockService = new Mock<ICatalogService>();

            _brands = new List<CatalogBrand>
            {
                new CatalogBrand { Id = 1, Brand = "Azure" },
                new CatalogBrand { Id = 2, Brand = ".NET" }
            };

            _types = new List<CatalogType>
            {
                new CatalogType { Id = 1, Type = "Mug" },
                new CatalogType { Id = 2, Type = "T-Shirt" }
            };

            _items = new List<CatalogItem>
            {
                new CatalogItem { Id = 1, Name = "Item 1", Price = 10.0M, CatalogBrandId = 1, CatalogTypeId = 1, CatalogBrand = _brands[0], CatalogType = _types[0] },
                new CatalogItem { Id = 2, Name = "Item 2", Price = 20.0M, CatalogBrandId = 2, CatalogTypeId = 2, CatalogBrand = _brands[1], CatalogType = _types[1] }
            };

            _mockService.Setup(s => s.GetCatalogBrands()).Returns(_brands);
            _mockService.Setup(s => s.GetCatalogTypes()).Returns(_types);

            _controller = new CatalogController(_mockService.Object);

            var mockUrlHelper = new Mock<UrlHelper>();
            mockUrlHelper.Setup(u => u.RouteUrl(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()))
                .Returns("http://localhost/items/1/pic");
            _controller.Url = mockUrlHelper.Object;

            var mockHttpContext = new Mock<HttpContextBase>();
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.Setup(r => r.Url).Returns(new Uri("http://localhost"));
            mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);

            var routeData = new RouteData();
            var controllerContext = new ControllerContext(mockHttpContext.Object, routeData, _controller);
            _controller.ControllerContext = controllerContext;
        }

        [TestCleanup]
        public void Cleanup()
        {
            _controller.Dispose();
        }

        [TestMethod]
        public void Index_ReturnsViewWithPaginatedItems()
        {
            var paginated = new PaginatedItemsViewModel<CatalogItem>(0, 10, 2, _items);
            _mockService.Setup(s => s.GetCatalogItemsPaginated(10, 0)).Returns(paginated);

            var result = _controller.Index(10, 0) as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as PaginatedItemsViewModel<CatalogItem>;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.TotalItems);
            Assert.AreEqual(2, model.Data.Count());
        }

        [TestMethod]
        public void Index_UsesDefaultPageSizeOf10()
        {
            var paginated = new PaginatedItemsViewModel<CatalogItem>(0, 10, 0, new List<CatalogItem>());
            _mockService.Setup(s => s.GetCatalogItemsPaginated(10, 0)).Returns(paginated);

            _controller.Index();

            _mockService.Verify(s => s.GetCatalogItemsPaginated(10, 0), Times.Once);
        }

        [TestMethod]
        public void Details_ExistingId_ReturnsViewWithItem()
        {
            _mockService.Setup(s => s.FindCatalogItem(1)).Returns(_items[0]);

            var result = _controller.Details(1) as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as CatalogItem;
            Assert.IsNotNull(model);
            Assert.AreEqual("Item 1", model.Name);
        }

        [TestMethod]
        public void Details_NullId_ReturnsBadRequest()
        {
            var result = _controller.Details(null);

            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));
        }

        [TestMethod]
        public void Details_NonExistingId_ReturnsNotFound()
        {
            _mockService.Setup(s => s.FindCatalogItem(999)).Returns((CatalogItem)null);

            var result = _controller.Details(999);

            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Create_Get_ReturnsViewWithSelectLists()
        {
            var result = _controller.Create() as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ViewBag.CatalogBrandId);
            Assert.IsNotNull(result.ViewBag.CatalogTypeId);
            Assert.IsInstanceOfType(result.Model, typeof(CatalogItem));
        }

        [TestMethod]
        public void Create_Post_ValidModel_RedirectsToIndex()
        {
            var newItem = new CatalogItem
            {
                Name = "New Item",
                Description = "Desc",
                Price = 15.0M,
                CatalogTypeId = 1,
                CatalogBrandId = 1,
                PictureFileName = "test.png"
            };

            var result = _controller.Create(newItem) as RedirectToRouteResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
            _mockService.Verify(s => s.CreateCatalogItem(newItem), Times.Once);
        }

        [TestMethod]
        public void Create_Post_InvalidModel_ReturnsView()
        {
            _controller.ModelState.AddModelError("Name", "Required");
            var item = new CatalogItem();

            var result = _controller.Create(item) as ViewResult;

            Assert.IsNotNull(result);
            _mockService.Verify(s => s.CreateCatalogItem(It.IsAny<CatalogItem>()), Times.Never);
        }

        [TestMethod]
        public void Edit_Get_ExistingId_ReturnsViewWithItem()
        {
            _mockService.Setup(s => s.FindCatalogItem(1)).Returns(_items[0]);

            var result = _controller.Edit(1) as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as CatalogItem;
            Assert.AreEqual("Item 1", model.Name);
            Assert.IsNotNull(result.ViewBag.CatalogBrandId);
            Assert.IsNotNull(result.ViewBag.CatalogTypeId);
        }

        [TestMethod]
        public void Edit_Get_NullId_ReturnsBadRequest()
        {
            var result = _controller.Edit((int?)null);

            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));
        }

        [TestMethod]
        public void Edit_Get_NonExistingId_ReturnsNotFound()
        {
            _mockService.Setup(s => s.FindCatalogItem(999)).Returns((CatalogItem)null);

            var result = _controller.Edit(999);

            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Edit_Post_ValidModel_RedirectsToIndex()
        {
            var item = new CatalogItem
            {
                Id = 1,
                Name = "Updated",
                Price = 25.0M,
                CatalogTypeId = 1,
                CatalogBrandId = 1,
                PictureFileName = "1.png"
            };

            var result = _controller.Edit(item) as RedirectToRouteResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
            _mockService.Verify(s => s.UpdateCatalogItem(item), Times.Once);
        }

        [TestMethod]
        public void Edit_Post_InvalidModel_ReturnsView()
        {
            _controller.ModelState.AddModelError("Price", "Invalid");
            var item = new CatalogItem { Id = 1 };

            var result = _controller.Edit(item) as ViewResult;

            Assert.IsNotNull(result);
            _mockService.Verify(s => s.UpdateCatalogItem(It.IsAny<CatalogItem>()), Times.Never);
        }

        [TestMethod]
        public void Delete_Get_ExistingId_ReturnsViewWithItem()
        {
            _mockService.Setup(s => s.FindCatalogItem(1)).Returns(_items[0]);

            var result = _controller.Delete(1) as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as CatalogItem;
            Assert.AreEqual("Item 1", model.Name);
        }

        [TestMethod]
        public void Delete_Get_NullId_ReturnsBadRequest()
        {
            var result = _controller.Delete(null);

            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));
        }

        [TestMethod]
        public void Delete_Get_NonExistingId_ReturnsNotFound()
        {
            _mockService.Setup(s => s.FindCatalogItem(999)).Returns((CatalogItem)null);

            var result = _controller.Delete(999);

            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void DeleteConfirmed_RemovesItemAndRedirects()
        {
            var item = _items[0];
            _mockService.Setup(s => s.FindCatalogItem(1)).Returns(item);

            var result = _controller.DeleteConfirmed(1) as RedirectToRouteResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
            _mockService.Verify(s => s.RemoveCatalogItem(item), Times.Once);
        }
    }
}
