using Catalog.Web.Pages.Catalog;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Catalog.Web.Tests;

/// <summary>
/// Parity tests for the catalog listing page (<c>Default.aspx</c> port),
/// covering the baseline paging (size 10 / index 0, ordered by Id) and the
/// new brand/type filter dropdowns.
/// </summary>
public class IndexModelTests : IDisposable
{
    private readonly CatalogDbContextFixture _fixture = new();

    private IndexModel CreateSut() =>
        new(_fixture.CreateContext(), NullLogger<IndexModel>.Instance);

    [Fact]
    public void Defaults_Match_Legacy_Baseline()
    {
        Assert.Equal(0, IndexModel.DefaultPageIndex);
        Assert.Equal(10, IndexModel.DefaultPageSize);
    }

    [Fact]
    public async Task OnGet_Default_Returns_First_Page_Of_Ten_Ordered_By_Id()
    {
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal(12, sut.Model.TotalItems);
        Assert.Equal(2, sut.Model.TotalPages);
        Assert.Equal(10, sut.Model.ItemsPerPage);
        Assert.Equal(0, sut.Model.ActualPage);

        var ids = sut.Model.Data.Select(i => i.Id).ToArray();
        Assert.Equal(Enumerable.Range(1, 10).ToArray(), ids);
    }

    [Fact]
    public async Task OnGet_Pages_With_Skip_Take()
    {
        var sut = CreateSut();
        sut.PageSize = 5;
        sut.PageIndex = 1;

        await sut.OnGetAsync();

        var ids = sut.Model.Data.Select(i => i.Id).ToArray();
        Assert.Equal(new[] { 6, 7, 8, 9, 10 }, ids);
        Assert.Equal(3, sut.Model.TotalPages);
        Assert.Equal(12, sut.Model.TotalItems);
    }

    [Fact]
    public async Task OnGet_Loads_Brand_And_Type_Navigation()
    {
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.All(sut.Model.Data, item =>
        {
            Assert.NotNull(item.CatalogBrand);
            Assert.NotNull(item.CatalogType);
        });
    }

    [Fact]
    public async Task OnGet_Filters_By_Brand()
    {
        var sut = CreateSut();
        sut.BrandFilter = 2; // .NET

        await sut.OnGetAsync();

        Assert.Equal(6, sut.Model.TotalItems);
        Assert.All(sut.Model.Data, item => Assert.Equal(2, item.CatalogBrandId));
    }

    [Fact]
    public async Task OnGet_Filters_By_Type()
    {
        var sut = CreateSut();
        sut.TypeFilter = 1; // Mug

        await sut.OnGetAsync();

        Assert.Equal(2, sut.Model.TotalItems);
        Assert.All(sut.Model.Data, item => Assert.Equal(1, item.CatalogTypeId));
    }

    [Fact]
    public async Task OnGet_Filters_By_Brand_And_Type_Combined()
    {
        var sut = CreateSut();
        sut.BrandFilter = 2; // .NET
        sut.TypeFilter = 1;  // Mug

        await sut.OnGetAsync();

        Assert.Equal(1, sut.Model.TotalItems);
        var item = Assert.Single(sut.Model.Data);
        Assert.Equal(2, item.CatalogBrandId);
        Assert.Equal(1, item.CatalogTypeId);
    }

    [Fact]
    public async Task OnGet_Populates_Filter_Dropdowns()
    {
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal(5, sut.Brands.Count());
        Assert.Equal(4, sut.Types.Count());
    }

    public void Dispose() => _fixture.Dispose();
}
