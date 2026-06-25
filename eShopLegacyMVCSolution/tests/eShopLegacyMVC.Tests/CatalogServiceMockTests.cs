using System.Linq;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eShopLegacyMVC.Tests
{
    [TestClass]
    public class CatalogServiceMockTests
    {
        private CatalogServiceMock _service;

        [TestInitialize]
        public void Setup()
        {
            _service = new CatalogServiceMock();
        }

        [TestMethod]
        public void GetCatalogItemsPaginated_ReturnsCorrectPageSize()
        {
            var result = _service.GetCatalogItemsPaginated(5, 0);

            Assert.AreEqual(5, result.Data.Count());
            Assert.AreEqual(5, result.ItemsPerPage);
            Assert.AreEqual(0, result.ActualPage);
        }

        [TestMethod]
        public void GetCatalogItemsPaginated_ReturnsCorrectTotalItems()
        {
            var result = _service.GetCatalogItemsPaginated(10, 0);

            Assert.AreEqual(12, result.TotalItems);
        }

        [TestMethod]
        public void GetCatalogItemsPaginated_SecondPageReturnsRemainingItems()
        {
            var result = _service.GetCatalogItemsPaginated(10, 1);

            Assert.AreEqual(2, result.Data.Count());
            Assert.AreEqual(1, result.ActualPage);
        }

        [TestMethod]
        public void GetCatalogItemsPaginated_ItemsAreOrderedById()
        {
            var result = _service.GetCatalogItemsPaginated(12, 0);
            var ids = result.Data.Select(i => i.Id).ToList();

            for (int i = 1; i < ids.Count; i++)
            {
                Assert.IsTrue(ids[i] > ids[i - 1],
                    $"Items not ordered by Id: {ids[i - 1]} should be less than {ids[i]}");
            }
        }

        [TestMethod]
        public void GetCatalogItemsPaginated_ItemsHaveBrandsPopulated()
        {
            var result = _service.GetCatalogItemsPaginated(12, 0);

            foreach (var item in result.Data)
            {
                Assert.IsNotNull(item.CatalogBrand,
                    $"CatalogBrand not populated for item {item.Id}");
                Assert.IsFalse(string.IsNullOrEmpty(item.CatalogBrand.Brand));
            }
        }

        [TestMethod]
        public void GetCatalogItemsPaginated_ItemsHaveTypesPopulated()
        {
            var result = _service.GetCatalogItemsPaginated(12, 0);

            foreach (var item in result.Data)
            {
                Assert.IsNotNull(item.CatalogType,
                    $"CatalogType not populated for item {item.Id}");
                Assert.IsFalse(string.IsNullOrEmpty(item.CatalogType.Type));
            }
        }

        [TestMethod]
        public void FindCatalogItem_ExistingId_ReturnsItem()
        {
            var item = _service.FindCatalogItem(1);

            Assert.IsNotNull(item);
            Assert.AreEqual(1, item.Id);
            Assert.AreEqual(".NET Bot Black Hoodie", item.Name);
        }

        [TestMethod]
        public void FindCatalogItem_NonExistingId_ReturnsNull()
        {
            var item = _service.FindCatalogItem(999);

            Assert.IsNull(item);
        }

        [TestMethod]
        public void GetCatalogTypes_ReturnsAllTypes()
        {
            var types = _service.GetCatalogTypes().ToList();

            Assert.AreEqual(4, types.Count);
            Assert.IsTrue(types.Any(t => t.Type == "Mug"));
            Assert.IsTrue(types.Any(t => t.Type == "T-Shirt"));
            Assert.IsTrue(types.Any(t => t.Type == "Sheet"));
            Assert.IsTrue(types.Any(t => t.Type == "USB Memory Stick"));
        }

        [TestMethod]
        public void GetCatalogBrands_ReturnsAllBrands()
        {
            var brands = _service.GetCatalogBrands().ToList();

            Assert.AreEqual(5, brands.Count);
            Assert.IsTrue(brands.Any(b => b.Brand == "Azure"));
            Assert.IsTrue(brands.Any(b => b.Brand == ".NET"));
            Assert.IsTrue(brands.Any(b => b.Brand == "Other"));
        }

        [TestMethod]
        public void CreateCatalogItem_AssignsNewId()
        {
            var newItem = new CatalogItem
            {
                Name = "Test Item",
                Description = "Test",
                Price = 9.99M,
                CatalogTypeId = 1,
                CatalogBrandId = 1,
                AvailableStock = 50
            };

            _service.CreateCatalogItem(newItem);

            Assert.AreEqual(13, newItem.Id);
            Assert.AreEqual(13, _service.GetCatalogItemsPaginated(20, 0).TotalItems);
        }

        [TestMethod]
        public void CreateCatalogItem_ItemIsRetrievable()
        {
            var newItem = new CatalogItem
            {
                Name = "Retrievable Item",
                Description = "Should be findable",
                Price = 5.00M,
                CatalogTypeId = 1,
                CatalogBrandId = 1
            };

            _service.CreateCatalogItem(newItem);
            var found = _service.FindCatalogItem(newItem.Id);

            Assert.IsNotNull(found);
            Assert.AreEqual("Retrievable Item", found.Name);
        }

        [TestMethod]
        public void UpdateCatalogItem_ModifiesExistingItem()
        {
            var item = _service.FindCatalogItem(1);
            item.Name = "Updated Name";
            item.Price = 99.99M;

            _service.UpdateCatalogItem(item);
            var updated = _service.FindCatalogItem(1);

            Assert.AreEqual("Updated Name", updated.Name);
            Assert.AreEqual(99.99M, updated.Price);
        }

        [TestMethod]
        public void UpdateCatalogItem_NonExistingItem_DoesNotThrow()
        {
            var item = new CatalogItem
            {
                Id = 999,
                Name = "Ghost Item",
                Price = 1.00M
            };

            _service.UpdateCatalogItem(item);
        }

        [TestMethod]
        public void RemoveCatalogItem_DecreasesCount()
        {
            var item = _service.FindCatalogItem(1);

            _service.RemoveCatalogItem(item);

            Assert.AreEqual(11, _service.GetCatalogItemsPaginated(20, 0).TotalItems);
            Assert.IsNull(_service.FindCatalogItem(1));
        }
    }
}
