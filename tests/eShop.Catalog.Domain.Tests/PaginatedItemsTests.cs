using eShop.Catalog.Domain;
using Xunit;

namespace eShop.Catalog.Domain.Tests;

public class PaginatedItemsTests
{
    [Theory]
    [InlineData(12, 10, 2)]
    [InlineData(10, 10, 1)]
    [InlineData(0, 10, 0)]
    public void TotalPages_RoundsUp(long count, int pageSize, int expectedPages)
    {
        var page = new PaginatedItems<CatalogItem>(0, pageSize, count, []);

        Assert.Equal(expectedPages, page.TotalPages);
    }

    [Fact]
    public void Constructor_CopiesThePagingArguments()
    {
        var data = new[] { new CatalogItem { Id = 1, Name = "Item" } };

        var page = new PaginatedItems<CatalogItem>(pageIndex: 1, pageSize: 5, count: 6, data: data);

        Assert.Equal(1, page.ActualPage);
        Assert.Equal(5, page.ItemsPerPage);
        Assert.Equal(6, page.TotalItems);
        Assert.Same(data, page.Data);
    }
}
