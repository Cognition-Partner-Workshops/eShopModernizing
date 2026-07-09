using Catalog.Domain;
using Catalog.Mvc.Models;
using Xunit;

namespace Catalog.LegacyPorted.Tests;

/// <summary>
/// Ported from the legacy MSTest <c>eShopLegacyMVC.Tests.PaginatedItemsViewModelTests</c>.
/// Exercises the modernized <see cref="PaginatedItemsViewModel{TEntity}"/> in
/// <c>Catalog.Mvc.Models</c>. The only intentional shape change from the legacy
/// view model is that <c>TotalItems</c> is now a <see cref="long"/> (to support
/// large catalogs); the <c>TotalPages = ceil(count / pageSize)</c> calculation
/// and all other assertions are preserved verbatim.
/// </summary>
public class PortedPaginatedItemsViewModelTests
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
        Assert.Equal(25L, vm.TotalItems);
        Assert.Equal(2, vm.Data.Count());
    }

    [Fact]
    public void TotalPages_CalculatesCorrectly_ExactDivision()
    {
        var vm = new PaginatedItemsViewModel<CatalogItem>(0, 10, 30, new List<CatalogItem>());

        Assert.Equal(3, vm.TotalPages);
    }

    [Fact]
    public void TotalPages_RoundsUp_WhenNotExactDivision()
    {
        var vm = new PaginatedItemsViewModel<CatalogItem>(0, 10, 25, new List<CatalogItem>());

        Assert.Equal(3, vm.TotalPages);
    }

    [Fact]
    public void TotalPages_ReturnsOne_WhenItemsFitOnSinglePage()
    {
        var vm = new PaginatedItemsViewModel<CatalogItem>(0, 10, 5, new List<CatalogItem>());

        Assert.Equal(1, vm.TotalPages);
    }

    [Fact]
    public void TotalPages_ReturnsZero_WhenNoItems()
    {
        var vm = new PaginatedItemsViewModel<CatalogItem>(0, 10, 0, new List<CatalogItem>());

        Assert.Equal(0, vm.TotalPages);
    }

    [Fact]
    public void Constructor_HandlesLargeTotalItems()
    {
        var vm = new PaginatedItemsViewModel<CatalogItem>(0, 10, 1000000, new List<CatalogItem>());

        Assert.Equal(100000, vm.TotalPages);
        Assert.Equal(1000000L, vm.TotalItems);
    }
}
