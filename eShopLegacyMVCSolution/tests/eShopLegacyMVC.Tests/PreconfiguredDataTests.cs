using System.Linq;
using eShopLegacyMVC.Models.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eShopLegacyMVC.Tests
{
    [TestClass]
    public class PreconfiguredDataTests
    {
        [TestMethod]
        public void GetPreconfiguredCatalogItems_Returns12Items()
        {
            var items = PreconfiguredData.GetPreconfiguredCatalogItems();

            Assert.AreEqual(12, items.Count);
        }

        [TestMethod]
        public void GetPreconfiguredCatalogItems_AllHaveUniqueIds()
        {
            var items = PreconfiguredData.GetPreconfiguredCatalogItems();
            var uniqueIds = items.Select(i => i.Id).Distinct().Count();

            Assert.AreEqual(items.Count, uniqueIds);
        }

        [TestMethod]
        public void GetPreconfiguredCatalogItems_AllHaveNames()
        {
            var items = PreconfiguredData.GetPreconfiguredCatalogItems();

            foreach (var item in items)
            {
                Assert.IsFalse(string.IsNullOrEmpty(item.Name),
                    $"Item {item.Id} has no name");
            }
        }

        [TestMethod]
        public void GetPreconfiguredCatalogItems_AllHavePositivePrices()
        {
            var items = PreconfiguredData.GetPreconfiguredCatalogItems();

            foreach (var item in items)
            {
                Assert.IsTrue(item.Price > 0,
                    $"Item {item.Id} ({item.Name}) has non-positive price: {item.Price}");
            }
        }

        [TestMethod]
        public void GetPreconfiguredCatalogItems_AllHavePictureFileNames()
        {
            var items = PreconfiguredData.GetPreconfiguredCatalogItems();

            foreach (var item in items)
            {
                Assert.IsFalse(string.IsNullOrEmpty(item.PictureFileName),
                    $"Item {item.Id} has no picture file name");
            }
        }

        [TestMethod]
        public void GetPreconfiguredCatalogItems_AllReferenceValidBrandIds()
        {
            var items = PreconfiguredData.GetPreconfiguredCatalogItems();
            var brandIds = PreconfiguredData.GetPreconfiguredCatalogBrands()
                .Select(b => b.Id).ToHashSet();

            foreach (var item in items)
            {
                Assert.IsTrue(brandIds.Contains(item.CatalogBrandId),
                    $"Item {item.Id} references invalid BrandId {item.CatalogBrandId}");
            }
        }

        [TestMethod]
        public void GetPreconfiguredCatalogItems_AllReferenceValidTypeIds()
        {
            var items = PreconfiguredData.GetPreconfiguredCatalogItems();
            var typeIds = PreconfiguredData.GetPreconfiguredCatalogTypes()
                .Select(t => t.Id).ToHashSet();

            foreach (var item in items)
            {
                Assert.IsTrue(typeIds.Contains(item.CatalogTypeId),
                    $"Item {item.Id} references invalid TypeId {item.CatalogTypeId}");
            }
        }

        [TestMethod]
        public void GetPreconfiguredCatalogBrands_Returns5Brands()
        {
            var brands = PreconfiguredData.GetPreconfiguredCatalogBrands();

            Assert.AreEqual(5, brands.Count());
        }

        [TestMethod]
        public void GetPreconfiguredCatalogTypes_Returns4Types()
        {
            var types = PreconfiguredData.GetPreconfiguredCatalogTypes();

            Assert.AreEqual(4, types.Count());
        }

        [TestMethod]
        public void GetPreconfiguredCatalogItems_AllHavePositiveStock()
        {
            var items = PreconfiguredData.GetPreconfiguredCatalogItems();

            foreach (var item in items)
            {
                Assert.IsTrue(item.AvailableStock >= 0,
                    $"Item {item.Id} has negative stock: {item.AvailableStock}");
            }
        }
    }
}
