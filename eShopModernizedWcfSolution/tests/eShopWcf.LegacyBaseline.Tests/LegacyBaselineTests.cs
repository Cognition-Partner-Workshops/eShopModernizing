using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using eShopWCFService;
using eShopWCFService.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

namespace eShopWcf.LegacyBaseline.Tests
{
    [TestClass]
    public sealed class LegacyBaselineTests
    {
        private static string previousConnectionString;
        private static string connectionString;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            previousConnectionString = Environment.GetEnvironmentVariable("ConnectionString");
            connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=eShopWcf_LegacyTest_"
                + Guid.NewGuid().ToString("N") + ";Integrated Security=True";
            Environment.SetEnvironmentVariable("ConnectionString", connectionString);
            context.WriteLine("Newly added legacy BEFORE baseline harness; production CatalogService and EF 6.1.3.");
            context.WriteLine(connectionString);
            using (var service = new CatalogService(new EntityModel()))
            {
                Assert.AreEqual(5, service.GetCatalogBrands().Count);
            }
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            try
            {
                using (var database = new DatabaseOnlyContext(connectionString))
                {
                    Assert.IsTrue(database.Database.Delete(), "The throwaway legacy database must be dropped.");
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("ConnectionString", previousConnectionString);
            }
        }

        [TestMethod]
        public void SeededBrandsAndTypesHaveOriginalIdsAndValues()
        {
            using (var service = new CatalogService(new EntityModel()))
            {
                CollectionAssert.AreEqual(new[] { "1:Azure", "2:.NET", "3:Visual Studio", "4:SQL Server", "5:Other" },
                    service.GetCatalogBrands().OrderBy(x => x.Id).Select(x => x.Id + ":" + x.Brand).ToArray());
                CollectionAssert.AreEqual(new[] { "1:Mug", "2:T-Shirt", "3:Sheet", "4:USB Memory Stick" },
                    service.GetCatalogTypes().OrderBy(x => x.Id).Select(x => x.Id + ":" + x.Type).ToArray());
                Assert.AreEqual(12, service.GetCatalogItems(0, 0).Count);
            }
        }

        [DataTestMethod]
        [DataRow(1, 2, 2, ".NET Bot Black Hoodie", "19.5", "2.png")]
        [DataRow(2, 2, 1, ".NET Black & White Mug", "8.5", "11.png")]
        [DataRow(3, 5, 2, "Prism White T-Shirt", "12", "7.png")]
        [DataRow(4, 2, 2, ".NET Foundation T-shirt", "12", "5.png")]
        [DataRow(5, 5, 3, "Roslyn Red Sheet", "8.5", "9.png")]
        [DataRow(6, 2, 2, ".NET Blue Hoodie", "12", "1.png")]
        [DataRow(7, 5, 2, "Roslyn Red T-Shirt", "12", "6.png")]
        [DataRow(8, 5, 2, "Kudu Purple Hoodie", "8.5", "3.png")]
        [DataRow(9, 5, 1, "Cup<T> White Mug", "12", "12.png")]
        [DataRow(10, 2, 3, ".NET Foundation Sheet", "12", "8.png")]
        [DataRow(11, 2, 3, "Cup<T> Sheet", "8.5", "10.png")]
        [DataRow(12, 5, 2, "Cup<T> TShirt", "12", "4.png")]
        public void FindReturnsSeededScalarsAndPopulatedNavigations(int id, int brandId, int typeId, string name, string price, string picture)
        {
            using (var service = new CatalogService(new EntityModel()))
            {
                var item = service.FindCatalogItem(id);
                Assert.IsNotNull(item);
                Assert.AreEqual(id, item.Id);
                Assert.AreEqual(brandId, item.CatalogBrandId);
                Assert.AreEqual(typeId, item.CatalogTypeId);
                Assert.AreEqual(name, item.Name);
                Assert.AreEqual(name, item.Description);
                Assert.AreEqual(decimal.Parse(price, CultureInfo.InvariantCulture), item.Price);
                Assert.AreEqual(picture, item.Picturefilename);
                Assert.IsNotNull(item.CatalogBrand);
                Assert.IsNotNull(item.CatalogType);
                Assert.AreEqual(brandId, item.CatalogBrand.Id);
                Assert.AreEqual(typeId, item.CatalogType.Id);
            }
        }

        [DataTestMethod]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(99999)]
        public void MissingFindReturnsNull(int id)
        {
            using (var service = new CatalogService(new EntityModel()))
            {
                Assert.IsNull(service.FindCatalogItem(id));
            }
        }

        [DataTestMethod]
        [DataRow(0, 0, "1,2,3,4,5,6,7,8,9,10,11,12")]
        [DataRow(2, 0, "1,2,4,6,10,11")]
        [DataRow(0, 1, "2,9")]
        [DataRow(5, 2, "3,7,8,12")]
        [DataRow(1, 4, "")]
        [DataRow(-1, 0, "")]
        [DataRow(0, -1, "")]
        [DataRow(-1, -1, "")]
        public void ItemFiltersAndUnpopulatedNavigations(int brand, int type, string ids)
        {
            using (var service = new CatalogService(new EntityModel()))
            {
                var items = service.GetCatalogItems(brand, type);
                Assert.AreEqual(ids, string.Join(",", items.OrderBy(x => x.Id).Select(x => x.Id)));
                foreach (var item in items)
                {
                    Assert.IsNull(item.CatalogBrand);
                    Assert.IsNull(item.CatalogType);
                }
            }
        }

        [DataTestMethod]
        [DataRow(2017, 9, 20, 1, 100)]
        [DataRow(2017, 9, 21, 1, 120)]
        [DataRow(2017, 9, 22, 1, 80)]
        [DataRow(2017, 9, 20, 2, 45)]
        [DataRow(2017, 9, 25, 4, 65)]
        [DataRow(2017, 9, 28, 5, 22)]
        [DataRow(2017, 9, 20, -1, 0)]
        [DataRow(2017, 9, 20, 0, 0)]
        [DataRow(2017, 9, 20, 99999, 0)]
        [DataRow(2020, 1, 1, 1, 0)]
        public void StockLookupIgnoresTimeAndMissingReturnsZero(int year, int month, int day, int itemId, int expected)
        {
            using (var service = new CatalogService(new EntityModel()))
            {
                Assert.AreEqual(expected, service.GetAvailableStock(new DateTime(year, month, day, 23, 59, 59), itemId));
            }
        }

        [TestMethod]
        public void StockInsertUsesMaximumPlusOneAndUpsertKeepsIdentity()
        {
            var date = new DateTime(2030, 1, 2);
            try
            {
                using (var db = new EntityModel())
                using (var service = new CatalogService(db))
                {
                    int expectedId = db.CatalogItemsStocks.Max(x => x.StockId) + 1;
                    var stock = new CatalogItemsStock { StockId = 99999, CatalogItemId = 1, Date = date.AddHours(3), AvailableStock = 17 };
                    service.CreateAvailableStock(stock);
                    Assert.AreEqual(expectedId, stock.StockId);
                }
                using (var db = new EntityModel())
                using (var service = new CatalogService(db))
                {
                    var before = db.CatalogItemsStocks.Single(x => x.CatalogItemId == 1 && x.Date == date);
                    int count = db.CatalogItemsStocks.Count();
                    service.CreateAvailableStock(new CatalogItemsStock { StockId = 12345, CatalogItemId = 1, Date = date.AddHours(21), AvailableStock = 29 });
                    Assert.AreEqual(29, service.GetAvailableStock(date, 1));
                    Assert.AreEqual(count, db.CatalogItemsStocks.Count());
                    Assert.AreEqual(before.StockId, db.CatalogItemsStocks.Single(x => x.CatalogItemId == 1 && x.Date == date).StockId);
                }
                using (var service = new CatalogService(new EntityModel()))
                {
                    Assert.AreEqual(29, service.GetAvailableStock(date, 1));
                }
            }
            finally
            {
                using (var db = new EntityModel())
                {
                    db.CatalogItemsStocks.RemoveRange(db.CatalogItemsStocks.Where(x => x.CatalogItemId == 1 && x.Date == date));
                    db.SaveChanges();
                }
            }
        }

        [TestMethod]
        public void CreateIgnoresCallerIdUsesMaximumPlusOneAndPreservesMoneyAndEmptyStrings()
        {
            var item = NewItem();
            try
            {
                using (var db = new EntityModel())
                using (var service = new CatalogService(db))
                {
                    int expectedId = db.CatalogItems.Max(x => x.Id) + 1;
                    service.CreateCatalogItem(item);
                    Assert.AreEqual(expectedId, item.Id);
                }
                using (var service = new CatalogService(new EntityModel()))
                {
                    var stored = service.FindCatalogItem(item.Id);
                    Assert.AreEqual(12.3456M, stored.Price);
                    Assert.AreEqual("", stored.Name);
                    Assert.AreEqual("", stored.Picturefilename);
                    Assert.AreEqual("baseline description", stored.Description);
                    Assert.AreEqual(2, stored.CatalogBrandId);
                    Assert.AreEqual(2, stored.CatalogTypeId);
                }
            }
            finally { DeleteItem(item.Id); }
        }

        [TestMethod]
        public void UpdateDetachedItemOverwritesEveryScalarIncludingNulls()
        {
            var item = InsertItem();
            try
            {
                using (var service = new CatalogService(new EntityModel()))
                {
                    service.UpdateCatalogItem(new CatalogItem
                    {
                        Id = item.Id, CatalogBrandId = 5, CatalogTypeId = 3,
                        Description = null, Name = "changed", Price = -1.25M, Picturefilename = null
                    });
                }
                using (var service = new CatalogService(new EntityModel()))
                {
                    var stored = service.FindCatalogItem(item.Id);
                    Assert.AreEqual(item.Id, stored.Id);
                    Assert.AreEqual(5, stored.CatalogBrandId);
                    Assert.AreEqual(3, stored.CatalogTypeId);
                    Assert.IsNull(stored.Description);
                    Assert.AreEqual("changed", stored.Name);
                    Assert.AreEqual(-1.25M, stored.Price);
                    Assert.IsNull(stored.Picturefilename);
                }
            }
            finally { DeleteItem(item.Id); }
        }

        [TestMethod]
        public void UpdateUnknownIdThrowsDbUpdateConcurrencyException()
        {
            using (var service = new CatalogService(new EntityModel()))
            {
                var item = NewItem();
                var error = Assert.ThrowsException<DbUpdateConcurrencyException>(() => service.UpdateCatalogItem(item));
                TestContext.WriteLine("Observed UpdateCatalogItem unknown ID: " + error.GetType().FullName + ": " + error.Message);
            }
        }

        [TestMethod]
        public void RemoveDetachedItemThrowsInvalidOperationException()
        {
            var item = InsertItem();
            try
            {
                using (var service = new CatalogService(new EntityModel()))
                {
                    var error = Assert.ThrowsException<InvalidOperationException>(() => service.RemoveCatalogItem(new CatalogItem { Id = item.Id }));
                    TestContext.WriteLine("Observed RemoveCatalogItem detached item in a fresh context: " + error.GetType().FullName + ": " + error.Message);
                }
                using (var service = new CatalogService(new EntityModel()))
                {
                    Assert.IsNotNull(service.FindCatalogItem(item.Id));
                }
            }
            finally { DeleteItem(item.Id); }
        }

        [TestMethod]
        public void RemoveItemObtainedFromSameContextDeletesPersistedRow()
        {
            var item = InsertItem();
            try
            {
                using (var service = new CatalogService(new EntityModel()))
                {
                    service.RemoveCatalogItem(service.FindCatalogItem(item.Id));
                }
                using (var service = new CatalogService(new EntityModel()))
                {
                    Assert.IsNull(service.FindCatalogItem(item.Id));
                }
            }
            finally { DeleteItem(item.Id); }
        }

        [DataTestMethod]
        [DataRow(9, 18, 1, 9, 18, 9, 21, 0.3f)]
        [DataRow(9, 21, 1, 9, 18, 9, 21, 0.3f)]
        [DataRow(9, 22, 2, 9, 22, 9, 26, 0.25f)]
        [DataRow(9, 26, 2, 9, 22, 9, 26, 0.25f)]
        [DataRow(9, 27, 3, 9, 27, 9, 30, 0.1f)]
        [DataRow(9, 30, 3, 9, 27, 9, 30, 0.1f)]
        [DataRow(10, 5, 4, 10, 5, 10, 20, 0.5f)]
        [DataRow(10, 20, 4, 10, 5, 10, 20, 0.5f)]
        [DataRow(11, 13, 5, 11, 13, 11, 25, 0.3f)]
        [DataRow(11, 25, 5, 11, 13, 11, 25, 0.3f)]
        [DataRow(12, 20, 6, 12, 20, 12, 25, 0.25f)]
        [DataRow(12, 25, 6, 12, 20, 12, 25, 0.25f)]
        public void DiscountIncludesBothDateBoundaries(int month, int day, int id, int startMonth, int startDay, int endMonth, int endDay, float size)
        {
            using (var service = new CatalogService(new EntityModel()))
            {
                var discount = service.GetDiscount(new DateTime(2017, month, day, 23, 59, 59));
                Assert.IsNotNull(discount);
                Assert.AreEqual(id, discount.Id);
                Assert.AreEqual((double)size, discount.Size);
                Assert.AreEqual(new DateTime(2017, startMonth, startDay), discount.Start);
                Assert.AreEqual(new DateTime(2017, endMonth, endDay), discount.End);
            }
        }

        [DataTestMethod]
        [DataRow(9, 17)]
        [DataRow(10, 1)]
        [DataRow(12, 26)]
        public void DiscountGapReturnsNull(int month, int day)
        {
            using (var service = new CatalogService(new EntityModel()))
            {
                Assert.IsNull(service.GetDiscount(new DateTime(2017, month, day)));
            }
        }

        private static CatalogItem NewItem() => new CatalogItem
        {
            Id = 99999, CatalogBrandId = 2, CatalogTypeId = 2, Description = "baseline description",
            Name = "", Price = 12.3456M, Picturefilename = ""
        };

        private static CatalogItem InsertItem()
        {
            var item = NewItem();
            using (var service = new CatalogService(new EntityModel()))
            {
                service.CreateCatalogItem(item);
            }
            return item;
        }

        private static void DeleteItem(int id)
        {
            using (var service = new CatalogService(new EntityModel()))
            {
                var item = service.FindCatalogItem(id);
                if (item != null) service.RemoveCatalogItem(item);
            }
        }

        private sealed class DatabaseOnlyContext : DbContext
        {
            public DatabaseOnlyContext(string value) : base(value) { }
        }
    }
}
