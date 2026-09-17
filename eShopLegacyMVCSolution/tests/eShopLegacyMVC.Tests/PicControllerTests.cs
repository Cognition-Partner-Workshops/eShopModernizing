using System;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using eShopLegacyMVC.Controllers;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eShopLegacyMVC.Tests
{
    [TestClass]
    public class PicControllerTests
    {
        private Mock<ICatalogService> _mockService;
        private PicController _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockService = new Mock<ICatalogService>();
            _controller = new PicController(_mockService.Object);

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

        private void SetupItemWithPicture(int id, string pictureFileName)
        {
            var item = new CatalogItem
            {
                Id = id,
                Name = "Item " + id,
                Price = 10.0M,
                CatalogBrandId = 1,
                CatalogTypeId = 1,
                PictureFileName = pictureFileName
            };
            _mockService.Setup(s => s.FindCatalogItem(id)).Returns(item);
        }

        [TestMethod]
        public void Index_ZeroId_ReturnsBadRequest()
        {
            var result = _controller.Index(0) as HttpStatusCodeResult;

            Assert.IsNotNull(result);
            Assert.AreEqual((int)HttpStatusCode.BadRequest, result.StatusCode);
            _mockService.Verify(s => s.FindCatalogItem(It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void Index_NegativeId_ReturnsBadRequest()
        {
            var result = _controller.Index(-5) as HttpStatusCodeResult;

            Assert.IsNotNull(result);
            Assert.AreEqual((int)HttpStatusCode.BadRequest, result.StatusCode);
            _mockService.Verify(s => s.FindCatalogItem(It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void Index_UnknownItem_ReturnsNotFound()
        {
            _mockService.Setup(s => s.FindCatalogItem(999)).Returns((CatalogItem)null);

            var result = _controller.Index(999);

            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Index_NullPictureFileName_ReturnsNotFound()
        {
            SetupItemWithPicture(1, null);

            var result = _controller.Index(1);

            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Index_EmptyPictureFileName_ReturnsNotFound()
        {
            SetupItemWithPicture(1, "");

            var result = _controller.Index(1);

            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Index_PictureFileNameWithBackslashTraversal_ReturnsNotFound()
        {
            SetupItemWithPicture(1, @"..\..\Web.config");

            var result = _controller.Index(1);

            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Index_PictureFileNameWithForwardSlashTraversal_ReturnsNotFound()
        {
            SetupItemWithPicture(1, "../../Web.config");

            var result = _controller.Index(1);

            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Index_PictureFileNameWithForwardSlashAndImageExtension_ReturnsNotFound()
        {
            SetupItemWithPicture(1, "../Pics/1.png");

            var result = _controller.Index(1);

            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Index_AbsoluteWindowsPath_ReturnsNotFound()
        {
            SetupItemWithPicture(1, @"C:\Windows\win.ini");

            var result = _controller.Index(1);

            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Index_AbsoluteUnixPath_ReturnsNotFound()
        {
            SetupItemWithPicture(1, "/etc/passwd");

            var result = _controller.Index(1);

            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Index_SvgExtension_ReturnsNotFound()
        {
            SetupItemWithPicture(1, "evil.svg");

            var result = _controller.Index(1);

            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Index_ConfigExtension_ReturnsNotFound()
        {
            SetupItemWithPicture(1, "Web.config");

            var result = _controller.Index(1);

            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Index_NoExtension_ReturnsNotFound()
        {
            SetupItemWithPicture(1, "noextension");

            var result = _controller.Index(1);

            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void TryGetSafeFileName_ValidPng_ReturnsTrueWithMimeType()
        {
            string fileName;
            string mimetype;

            var ok = PicController.TryGetSafeFileName("1.png", out fileName, out mimetype);

            Assert.IsTrue(ok);
            Assert.AreEqual("1.png", fileName);
            Assert.AreEqual("image/png", mimetype);
        }

        [TestMethod]
        public void TryGetSafeFileName_UpperCaseJpeg_IsCaseInsensitive()
        {
            string fileName;
            string mimetype;

            var ok = PicController.TryGetSafeFileName("Photo.JPEG", out fileName, out mimetype);

            Assert.IsTrue(ok);
            Assert.AreEqual("Photo.JPEG", fileName);
            Assert.AreEqual("image/jpeg", mimetype);
        }

        [TestMethod]
        public void TryGetSafeFileName_AllWhitelistedExtensions_Accepted()
        {
            string fileName;
            string mimetype;

            Assert.IsTrue(PicController.TryGetSafeFileName("a.png", out fileName, out mimetype));
            Assert.AreEqual("image/png", mimetype);
            Assert.IsTrue(PicController.TryGetSafeFileName("a.gif", out fileName, out mimetype));
            Assert.AreEqual("image/gif", mimetype);
            Assert.IsTrue(PicController.TryGetSafeFileName("a.jpg", out fileName, out mimetype));
            Assert.AreEqual("image/jpeg", mimetype);
            Assert.IsTrue(PicController.TryGetSafeFileName("a.jpeg", out fileName, out mimetype));
            Assert.AreEqual("image/jpeg", mimetype);
            Assert.IsTrue(PicController.TryGetSafeFileName("a.bmp", out fileName, out mimetype));
            Assert.AreEqual("image/bmp", mimetype);
        }

        [TestMethod]
        public void TryGetSafeFileName_TraversalAndSeparators_Rejected()
        {
            string fileName;
            string mimetype;

            Assert.IsFalse(PicController.TryGetSafeFileName(@"..\..\Web.config", out fileName, out mimetype));
            Assert.IsFalse(PicController.TryGetSafeFileName("../1.png", out fileName, out mimetype));
            Assert.IsFalse(PicController.TryGetSafeFileName(@"sub\1.png", out fileName, out mimetype));
            Assert.IsFalse(PicController.TryGetSafeFileName("sub/1.png", out fileName, out mimetype));
            Assert.IsFalse(PicController.TryGetSafeFileName(@"C:\1.png", out fileName, out mimetype));
            Assert.IsFalse(PicController.TryGetSafeFileName("/1.png", out fileName, out mimetype));
            Assert.IsNull(fileName);
            Assert.IsNull(mimetype);
        }

        [TestMethod]
        public void TryGetSafeFileName_DisallowedExtensions_Rejected()
        {
            string fileName;
            string mimetype;

            Assert.IsFalse(PicController.TryGetSafeFileName("evil.svg", out fileName, out mimetype));
            Assert.IsFalse(PicController.TryGetSafeFileName("Web.config", out fileName, out mimetype));
            Assert.IsFalse(PicController.TryGetSafeFileName("a.tiff", out fileName, out mimetype));
            Assert.IsFalse(PicController.TryGetSafeFileName("a.wmf", out fileName, out mimetype));
            Assert.IsFalse(PicController.TryGetSafeFileName("a.jp2", out fileName, out mimetype));
            Assert.IsFalse(PicController.TryGetSafeFileName("a.exe", out fileName, out mimetype));
            Assert.IsFalse(PicController.TryGetSafeFileName("noextension", out fileName, out mimetype));
            Assert.IsNull(mimetype);
        }

        [TestMethod]
        public void TryGetSafeFileName_NullOrWhitespace_Rejected()
        {
            string fileName;
            string mimetype;

            Assert.IsFalse(PicController.TryGetSafeFileName(null, out fileName, out mimetype));
            Assert.IsFalse(PicController.TryGetSafeFileName("", out fileName, out mimetype));
            Assert.IsFalse(PicController.TryGetSafeFileName("   ", out fileName, out mimetype));
        }

        [TestMethod]
        public void TryGetSafeFileName_InvalidFileNameChars_Rejected()
        {
            string fileName;
            string mimetype;

            Assert.IsFalse(PicController.TryGetSafeFileName("a\0b.png", out fileName, out mimetype));
        }

        [TestMethod]
        public void GetImageMimeTypeFromImageFileExtension_Svg_ReturnsNull()
        {
            Assert.IsNull(PicController.GetImageMimeTypeFromImageFileExtension(".svg"));
        }

        [TestMethod]
        public void GetImageMimeTypeFromImageFileExtension_Unknown_NeverReturnsOctetStream()
        {
            Assert.IsNull(PicController.GetImageMimeTypeFromImageFileExtension(".xyz"));
            Assert.IsNull(PicController.GetImageMimeTypeFromImageFileExtension(""));
            Assert.IsNull(PicController.GetImageMimeTypeFromImageFileExtension(null));
        }
    }
}
