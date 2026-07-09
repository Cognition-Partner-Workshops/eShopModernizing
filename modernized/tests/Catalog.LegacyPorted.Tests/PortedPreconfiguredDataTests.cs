using Catalog.Domain;
using Xunit;

namespace Catalog.LegacyPorted.Tests;

/// <summary>
/// Ported from the legacy MSTest <c>eShopLegacyMVC.Tests.PreconfiguredDataTests</c>.
/// The legacy suite asserted against the static <c>PreconfiguredData</c> helper;
/// in the modernized stack that seed data was moved into the EF Core model via
/// <c>CatalogContextSeed.SeedCatalog</c> / <c>HasData</c>. These tests therefore
/// assert against the data materialized into a freshly created
/// <see cref="Catalog.Infrastructure.CatalogDbContext"/>, verifying the
/// <c>HasData</c> seed reproduces the baseline (12 items / 5 brands / 4 types)
/// and the same per-item invariants the legacy suite guarded.
/// </summary>
public class PortedPreconfiguredDataTests : IClassFixture<SeededSqliteFixture>
{
    private readonly SeededSqliteFixture _fixture;

    public PortedPreconfiguredDataTests(SeededSqliteFixture fixture) => _fixture = fixture;

    private List<CatalogItem> Items()
    {
        using var context = _fixture.CreateContext();
        return context.CatalogItems.ToList();
    }

    [Fact]
    public void Seed_Returns12Items()
    {
        Assert.Equal(12, Items().Count);
    }

    [Fact]
    public void Seed_AllItemsHaveUniqueIds()
    {
        var items = Items();
        var uniqueIds = items.Select(i => i.Id).Distinct().Count();

        Assert.Equal(items.Count, uniqueIds);
    }

    [Fact]
    public void Seed_AllItemsHaveNames()
    {
        foreach (var item in Items())
        {
            Assert.False(string.IsNullOrEmpty(item.Name), $"Item {item.Id} has no name");
        }
    }

    [Fact]
    public void Seed_AllItemsHavePositivePrices()
    {
        foreach (var item in Items())
        {
            Assert.True(item.Price > 0, $"Item {item.Id} ({item.Name}) has non-positive price: {item.Price}");
        }
    }

    [Fact]
    public void Seed_AllItemsHavePictureFileNames()
    {
        foreach (var item in Items())
        {
            Assert.False(string.IsNullOrEmpty(item.PictureFileName), $"Item {item.Id} has no picture file name");
        }
    }

    [Fact]
    public void Seed_AllItemsReferenceValidBrandIds()
    {
        using var context = _fixture.CreateContext();
        var brandIds = context.CatalogBrands.Select(b => b.Id).ToHashSet();

        foreach (var item in context.CatalogItems.ToList())
        {
            Assert.True(brandIds.Contains(item.CatalogBrandId), $"Item {item.Id} references invalid BrandId {item.CatalogBrandId}");
        }
    }

    [Fact]
    public void Seed_AllItemsReferenceValidTypeIds()
    {
        using var context = _fixture.CreateContext();
        var typeIds = context.CatalogTypes.Select(t => t.Id).ToHashSet();

        foreach (var item in context.CatalogItems.ToList())
        {
            Assert.True(typeIds.Contains(item.CatalogTypeId), $"Item {item.Id} references invalid TypeId {item.CatalogTypeId}");
        }
    }

    [Fact]
    public void Seed_Returns5Brands()
    {
        using var context = _fixture.CreateContext();

        Assert.Equal(5, context.CatalogBrands.Count());
    }

    [Fact]
    public void Seed_Returns4Types()
    {
        using var context = _fixture.CreateContext();

        Assert.Equal(4, context.CatalogTypes.Count());
    }

    [Fact]
    public void Seed_AllItemsHaveNonNegativeStock()
    {
        foreach (var item in Items())
        {
            Assert.True(item.AvailableStock >= 0, $"Item {item.Id} has negative stock: {item.AvailableStock}");
        }
    }

    /// <summary>
    /// Not present verbatim in the legacy suite, but required by the modernization
    /// ticket: the <c>HasData</c> seed must reproduce the exact baseline values,
    /// not merely the counts/invariants above.
    /// </summary>
    [Fact]
    public void Seed_Reproduces_Known_Baseline_Values()
    {
        using var context = _fixture.CreateContext();

        var brands = context.CatalogBrands.OrderBy(b => b.Id).Select(b => b.Brand).ToArray();
        Assert.Equal(new[] { "Azure", ".NET", "Visual Studio", "SQL Server", "Other" }, brands);

        var types = context.CatalogTypes.OrderBy(t => t.Id).Select(t => t.Type).ToArray();
        Assert.Equal(new[] { "Mug", "T-Shirt", "Sheet", "USB Memory Stick" }, types);

        var first = context.CatalogItems.Single(i => i.Id == 1);
        Assert.Equal(".NET Bot Black Hoodie", first.Name);
        Assert.Equal(19.5M, first.Price);
        Assert.Equal(2, first.CatalogBrandId);
        Assert.Equal(2, first.CatalogTypeId);
        Assert.Equal("1.png", first.PictureFileName);
    }
}
