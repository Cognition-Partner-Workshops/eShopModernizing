using eShop.Catalog.Domain.Entities;

namespace eShop.Catalog.Domain.Tests;

public class PaginatedItemsViewModelTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var items = new List<CatalogItem>
        {
            new() { Id = 1, Name = "Item 1" },
            new() { Id = 2, Name = "Item 2" },
        };

        var vm = new PaginatedItemsViewModel<CatalogItem>(2, 10, 25, items);

        Assert.Equal(2, vm.ActualPage);
        Assert.Equal(10, vm.ItemsPerPage);
        Assert.Equal(25, vm.TotalItems);
        Assert.Equal(2, vm.Data.Count());
    }

    [Theory]
    [InlineData(30, 10, 3)]
    [InlineData(25, 10, 3)]
    [InlineData(5, 10, 1)]
    [InlineData(0, 10, 0)]
    [InlineData(1000000, 10, 100000)]
    public void TotalPages_MatchesLegacyRounding(long count, int pageSize, int expected)
    {
        var vm = new PaginatedItemsViewModel<CatalogItem>(0, pageSize, count, new List<CatalogItem>());

        Assert.Equal(expected, vm.TotalPages);
    }

    [Fact]
    public void TotalItems_PreservesLargeCounts()
    {
        var vm = new PaginatedItemsViewModel<CatalogItem>(0, 10, 1000000, new List<CatalogItem>());

        Assert.Equal(1000000, vm.TotalItems);
    }
}
