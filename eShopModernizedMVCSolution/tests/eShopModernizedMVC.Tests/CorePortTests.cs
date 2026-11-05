using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using eShopModernizedMVC.Controllers.WebApi;
using eShopModernizedMVC.Models;
using eShopModernizedMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eShopModernizedMVC.Tests
{
    [TestClass]
    public class CorePortTests
    {
        [TestMethod]
        public void Files_ReturnsJsonBrands()
        {
            var service = new Mock<ICatalogService>();
            service.Setup(x => x.GetCatalogBrands()).Returns(new[] { new CatalogBrand { Id = 1, Brand = "Azure" } });
            var result = new FilesController(service.Object).Get() as FileContentResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("application/json", result.ContentType);
            var brands = JsonSerializer.Deserialize<List<FilesController.BrandDTO>>(result.FileContents);
            Assert.AreEqual(1, brands.Count);
            Assert.AreEqual(1, brands[0].Id);
            Assert.AreEqual("Azure", brands[0].Brand);
        }

        [TestMethod]
        public void Files_EmptyBrands_ReturnsEmptyJsonArray()
        {
            var service = new Mock<ICatalogService>();
            service.Setup(x => x.GetCatalogBrands()).Returns(Array.Empty<CatalogBrand>());
            var result = new FilesController(service.Object).Get() as FileContentResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("[]", System.Text.Encoding.UTF8.GetString(result.FileContents));
        }

        [TestMethod]
        public void Files_ServiceFailure_PropagatesToErrorMiddleware()
        {
            var service = new Mock<ICatalogService>();
            service.Setup(x => x.GetCatalogBrands()).Throws(new InvalidOperationException("unavailable"));
            var error = Assert.ThrowsException<InvalidOperationException>(() => new FilesController(service.Object).Get());
            Assert.AreEqual("unavailable", error.Message);
        }

        [TestMethod]
        public void Brands_GetAndDelete_PreserveDemoData()
        {
            var service = new CatalogServiceMock();
            var controller = new BrandsController(service);
            var result = controller.Get(1) as OkObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Azure", ((CatalogBrand)result.Value).Brand);
            Assert.IsInstanceOfType(controller.Delete(1), typeof(OkResult));
            Assert.AreEqual(5, controller.Get().Count());
        }

        [TestMethod]
        public void Brands_UnknownId_ReturnsNotFound()
        {
            var controller = new BrandsController(new CatalogServiceMock());
            Assert.IsInstanceOfType(controller.Get(999), typeof(NotFoundResult));
            Assert.IsInstanceOfType(controller.Delete(999), typeof(NotFoundResult));
        }

        [TestMethod]
        public void SqlServerCreateScript_MatchesLegacySchema()
        {
            using var db = new CatalogDBContext(new DbContextOptionsBuilder<CatalogDBContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ModelOnly;Integrated Security=True").Options);
            var script = db.Database.GenerateCreateScript().Replace("\r\n", "\n");
            foreach (var table in new[] { "Catalog", "CatalogBrand", "CatalogType" })
                StringAssert.Contains(script, $"CONSTRAINT [PK_dbo.{table}] PRIMARY KEY ([Id])");
            foreach (var table in new[] { "CatalogBrand", "CatalogType" })
            {
                StringAssert.Contains(script, $"CREATE TABLE [{table}] (\n    [Id] int NOT NULL IDENTITY,\n");
                StringAssert.Contains(script, $"CONSTRAINT [FK_dbo.Catalog_dbo.{table}_{table}Id] FOREIGN KEY ([{table}Id]) REFERENCES [{table}] ([Id]) ON DELETE CASCADE");
                StringAssert.Contains(script, $"CREATE INDEX [IX_{table}Id] ON [Catalog] ([{table}Id])");
            }
            var columns = new[] { "Id", "Name", "Description", "Price", "PictureFileName", "CatalogTypeId", "CatalogBrandId", "AvailableStock", "RestockThreshold", "MaxStockThreshold", "OnReorder" };
            var catalog = script.Substring(script.IndexOf("CREATE TABLE [Catalog]", StringComparison.Ordinal));
            var previous = -1;
            foreach (var column in columns)
            {
                var position = catalog.IndexOf($"[{column}]", StringComparison.Ordinal);
                Assert.IsTrue(position > previous, column);
                previous = position;
            }
            Assert.IsFalse(catalog.Substring(0, catalog.IndexOf("CONSTRAINT", StringComparison.Ordinal)).Contains("IDENTITY"));
            Assert.AreEqual("catalog_hilo", db.Model.FindEntityType(typeof(CatalogItem)).FindProperty("Id").GetHiLoSequenceName());
            Assert.IsFalse(script.Contains("__MigrationHistory"));
        }

        [TestMethod]
        public void SqlServerModel_RetainsTablesPrecisionAndHiLoSequences()
        {
            using var db = new CatalogDBContext(new DbContextOptionsBuilder<CatalogDBContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ModelOnly;Integrated Security=True").Options);
            var item = db.Model.FindEntityType(typeof(CatalogItem));
            Assert.AreEqual("Catalog", item.GetTableName());
            Assert.IsNull(item.FindProperty(nameof(CatalogItem.PictureUri)));
            Assert.AreEqual(18, item.FindProperty(nameof(CatalogItem.Price)).GetPrecision());
            Assert.AreEqual(2, item.FindProperty(nameof(CatalogItem.Price)).GetScale());
            foreach (var name in new[] { "catalog_hilo", "catalog_brand_hilo", "catalog_type_hilo" })
            {
                var sequence = db.Model.FindSequence(name);
                Assert.IsNotNull(sequence);
                Assert.AreEqual(1L, sequence.StartValue);
                Assert.AreEqual(10, sequence.IncrementBy);
            }
        }
    }
}
