using eShop.Catalog.Domain;
using eShop.Catalog.Domain.Entities;

namespace eShop.Web.Tests.Legacy;

/// <summary>
/// The legacy <c>eShopLegacyMVC.Tests.PaginatedItemsViewModelTests</c> MSTest suite ported to
/// xunit; the paging envelope drives the catalog list pager.
/// </summary>
public class PaginatedItemsViewModelTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        List<CatalogItem> items =
        [
            new CatalogItem { Id = 1, Name = "Item 1" },
            new CatalogItem { Id = 2, Name = "Item 2" },
        ];

        var vm = new PaginatedItemsViewModel<CatalogItem>(2, 10, 25, items);

        Assert.Equal(2, vm.ActualPage);
        Assert.Equal(10, vm.ItemsPerPage);
        Assert.Equal(25, vm.TotalItems);
        Assert.Equal(2, vm.Data.Count());
    }

    [Fact]
    public void TotalPages_CalculatesCorrectly_ExactDivision()
    {
        var vm = new PaginatedItemsViewModel<CatalogItem>(0, 10, 30, []);

        Assert.Equal(3, vm.TotalPages);
    }

    [Fact]
    public void TotalPages_RoundsUp_WhenNotExactDivision()
    {
        var vm = new PaginatedItemsViewModel<CatalogItem>(0, 10, 25, []);

        Assert.Equal(3, vm.TotalPages);
    }

    [Fact]
    public void TotalPages_ReturnsOne_WhenItemsFitOnSinglePage()
    {
        var vm = new PaginatedItemsViewModel<CatalogItem>(0, 10, 5, []);

        Assert.Equal(1, vm.TotalPages);
    }

    [Fact]
    public void TotalPages_ReturnsZero_WhenNoItems()
    {
        var vm = new PaginatedItemsViewModel<CatalogItem>(0, 10, 0, []);

        Assert.Equal(0, vm.TotalPages);
    }

    [Fact]
    public void Constructor_HandlesLargeTotalItems()
    {
        var vm = new PaginatedItemsViewModel<CatalogItem>(0, 10, 1000000, []);

        Assert.Equal(100000, vm.TotalPages);
        Assert.Equal(1000000, vm.TotalItems);
    }
}
