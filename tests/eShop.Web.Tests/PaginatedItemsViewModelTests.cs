using eShop.Catalog.Domain;
using Xunit;

namespace eShop.Web.Tests;

/// <summary>
/// Port of the legacy MSTest <c>eShopLegacyMVC.Tests.PaginatedItemsViewModelTests</c>. The view
/// model itself moved to the domain as <see cref="PaginatedItems{TEntity}"/>.
/// </summary>
public class PaginatedItemsViewModelTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        List<CatalogItem> items =
        [
            new() { Id = 1, Name = "Item 1" },
            new() { Id = 2, Name = "Item 2" },
        ];

        var vm = new PaginatedItems<CatalogItem>(2, 10, 25, items);

        Assert.Equal(2, vm.ActualPage);
        Assert.Equal(10, vm.ItemsPerPage);
        Assert.Equal(25, vm.TotalItems);
        Assert.Equal(2, vm.Data.Count());
    }

    [Fact]
    public void TotalPages_CalculatesCorrectly_ExactDivision() =>
        Assert.Equal(3, new PaginatedItems<CatalogItem>(0, 10, 30, []).TotalPages);

    [Fact]
    public void TotalPages_RoundsUp_WhenNotExactDivision() =>
        Assert.Equal(3, new PaginatedItems<CatalogItem>(0, 10, 25, []).TotalPages);

    [Fact]
    public void TotalPages_ReturnsOne_WhenItemsFitOnSinglePage() =>
        Assert.Equal(1, new PaginatedItems<CatalogItem>(0, 10, 5, []).TotalPages);

    [Fact]
    public void TotalPages_ReturnsZero_WhenNoItems() =>
        Assert.Equal(0, new PaginatedItems<CatalogItem>(0, 10, 0, []).TotalPages);

    [Fact]
    public void Constructor_HandlesLargeTotalItems()
    {
        var vm = new PaginatedItems<CatalogItem>(0, 10, 1000000, []);

        Assert.Equal(100000, vm.TotalPages);
        Assert.Equal(1000000, vm.TotalItems);
    }
}
