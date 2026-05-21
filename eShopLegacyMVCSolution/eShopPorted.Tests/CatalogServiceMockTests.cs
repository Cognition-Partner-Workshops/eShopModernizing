using System.Linq;
using eShopPorted.Services;
using Xunit;

namespace eShopPorted.Tests
{
    public class CatalogServiceMockTests
    {
        private readonly CatalogServiceMock _service;

        public CatalogServiceMockTests()
        {
            _service = new CatalogServiceMock();
        }

        [Fact]
        public void GetCatalogItemsPaginated_ReturnsItems()
        {
            var result = _service.GetCatalogItemsPaginated(10, 0);

            Assert.NotNull(result);
            Assert.True(result.TotalItems > 0);
            Assert.NotEmpty(result.Data);
        }

        [Fact]
        public void GetCatalogItemsPaginated_RespectsPageSize()
        {
            var result = _service.GetCatalogItemsPaginated(3, 0);

            Assert.Equal(3, result.Data.Count());
            Assert.Equal(3, result.ItemsPerPage);
        }

        [Fact]
        public void FindCatalogItem_ReturnsItem_WhenExists()
        {
            var item = _service.FindCatalogItem(1);

            Assert.NotNull(item);
            Assert.Equal(1, item.Id);
            Assert.Equal(".NET Bot Black Hoodie", item.Name);
        }

        [Fact]
        public void FindCatalogItem_ReturnsNull_WhenNotExists()
        {
            var item = _service.FindCatalogItem(999);

            Assert.Null(item);
        }

        [Fact]
        public void GetCatalogBrands_ReturnsBrands()
        {
            var brands = _service.GetCatalogBrands();

            Assert.NotEmpty(brands);
            Assert.Contains(brands, b => b.Brand == ".NET");
        }

        [Fact]
        public void GetCatalogTypes_ReturnsTypes()
        {
            var types = _service.GetCatalogTypes();

            Assert.NotEmpty(types);
            Assert.Contains(types, t => t.Type == "T-Shirt");
        }

        [Fact]
        public void CreateCatalogItem_AddsItem()
        {
            var initialCount = _service.GetCatalogItemsPaginated(100, 0).TotalItems;

            var newItem = new Models.CatalogItem
            {
                Name = "Test Item",
                Description = "Test",
                Price = 9.99M,
                CatalogTypeId = 1,
                CatalogBrandId = 1
            };
            _service.CreateCatalogItem(newItem);

            var afterCount = _service.GetCatalogItemsPaginated(100, 0).TotalItems;
            Assert.Equal(initialCount + 1, afterCount);
            Assert.True(newItem.Id > 0);
        }

        [Fact]
        public void UpdateCatalogItem_ModifiesItem()
        {
            var item = _service.FindCatalogItem(1);
            item.Name = "Updated Name";

            _service.UpdateCatalogItem(item);

            var updated = _service.FindCatalogItem(1);
            Assert.Equal("Updated Name", updated.Name);
        }

        [Fact]
        public void RemoveCatalogItem_DeletesItem()
        {
            var item = _service.FindCatalogItem(1);
            Assert.NotNull(item);

            _service.RemoveCatalogItem(item);

            var deleted = _service.FindCatalogItem(1);
            Assert.Null(deleted);
        }
    }
}
