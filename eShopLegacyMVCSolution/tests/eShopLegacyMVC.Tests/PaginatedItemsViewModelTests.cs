using System.Collections.Generic;
using System.Linq;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.ViewModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eShopLegacyMVC.Tests
{
    [TestClass]
    public class PaginatedItemsViewModelTests
    {
        [TestMethod]
        public void Constructor_SetsAllProperties()
        {
            var items = new List<CatalogItem>
            {
                new CatalogItem { Id = 1, Name = "Item 1" },
                new CatalogItem { Id = 2, Name = "Item 2" }
            };

            var vm = new PaginatedItemsViewModel<CatalogItem>(2, 10, 25, items);

            Assert.AreEqual(2, vm.ActualPage);
            Assert.AreEqual(10, vm.ItemsPerPage);
            Assert.AreEqual(25, vm.TotalItems);
            Assert.AreEqual(2, vm.Data.Count());
        }

        [TestMethod]
        public void TotalPages_CalculatesCorrectly_ExactDivision()
        {
            var vm = new PaginatedItemsViewModel<CatalogItem>(0, 10, 30, new List<CatalogItem>());

            Assert.AreEqual(3, vm.TotalPages);
        }

        [TestMethod]
        public void TotalPages_RoundsUp_WhenNotExactDivision()
        {
            var vm = new PaginatedItemsViewModel<CatalogItem>(0, 10, 25, new List<CatalogItem>());

            Assert.AreEqual(3, vm.TotalPages);
        }

        [TestMethod]
        public void TotalPages_ReturnsOne_WhenItemsFitOnSinglePage()
        {
            var vm = new PaginatedItemsViewModel<CatalogItem>(0, 10, 5, new List<CatalogItem>());

            Assert.AreEqual(1, vm.TotalPages);
        }

        [TestMethod]
        public void TotalPages_ReturnsZero_WhenNoItems()
        {
            var vm = new PaginatedItemsViewModel<CatalogItem>(0, 10, 0, new List<CatalogItem>());

            Assert.AreEqual(0, vm.TotalPages);
        }

        [TestMethod]
        public void Constructor_HandlesLargeTotalItems()
        {
            var vm = new PaginatedItemsViewModel<CatalogItem>(0, 10, 1000000, new List<CatalogItem>());

            Assert.AreEqual(100000, vm.TotalPages);
            Assert.AreEqual(1000000, vm.TotalItems);
        }
    }
}
