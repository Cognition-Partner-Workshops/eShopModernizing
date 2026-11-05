using eShop.Catalog.Data.Seeding;
using eShop.Catalog.Data.Sequences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace eShop.Catalog.Data.Tests;

/// <summary>
/// Seeding parity with the legacy <c>CatalogDBInitializer</c>: a fresh database ends up with the
/// same rows and the same sequence-allocated ids, with and without the CSV customization data.
/// </summary>
public sealed class CatalogDatabaseInitializerTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public CatalogDatabaseInitializerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task FreshDatabase_SeedsTheLegacyDefaultDataSet()
    {
        await InitializeAsync(new CatalogSeedOptions());

        using var context = CreateContext();

        Assert.Equal(
            PreconfiguredData.GetPreconfiguredCatalogTypes().Select(t => (t.Id, t.Type)),
            context.CatalogTypes.OrderBy(t => t.Id).Select(t => new { t.Id, t.Type }).ToList()
                .Select(t => (t.Id, t.Type)));

        Assert.Equal(
            PreconfiguredData.GetPreconfiguredCatalogBrands().Select(b => (b.Id, b.Brand)),
            context.CatalogBrands.OrderBy(b => b.Id).Select(b => new { b.Id, b.Brand }).ToList()
                .Select(b => (b.Id, b.Brand)));

        var expectedItems = PreconfiguredData.GetPreconfiguredCatalogItems();
        var items = context.CatalogItems.OrderBy(i => i.Id).ToList();

        Assert.Equal(expectedItems.Count, items.Count);
        Assert.Equal(
            expectedItems.Select(i => (i.Id, i.Name, i.Price, i.CatalogTypeId, i.CatalogBrandId, i.AvailableStock)),
            items.Select(i => (i.Id, i.Name, i.Price, i.CatalogTypeId, i.CatalogBrandId, i.AvailableStock)));
    }

    [Fact]
    public async Task FreshDatabase_AllocatesTheSeededIdsFromTheSequences()
    {
        await InitializeAsync(new CatalogSeedOptions());

        using var context = CreateContext();

        // The three sequences start at 1: types and brands take one value each and count up from
        // it, items are handed out in HiLo blocks of ten.
        Assert.Equal([1, 2, 3, 4], context.CatalogTypes.Select(t => t.Id).ToList().Order().ToList());
        Assert.Equal([1, 2, 3, 4, 5], context.CatalogBrands.Select(b => b.Id).ToList().Order().ToList());
        Assert.Equal(Enumerable.Range(1, 12), context.CatalogItems.Select(i => i.Id).ToList().Order().ToList());
    }

    [Fact]
    public async Task RunningTwice_DoesNotDuplicateOrRenumberTheSeedData()
    {
        await InitializeAsync(new CatalogSeedOptions());

        using (var context = CreateContext())
        {
            Assert.Equal(12, context.CatalogItems.Count());
        }

        await InitializeAsync(new CatalogSeedOptions());

        using (var context = CreateContext())
        {
            Assert.Equal(4, context.CatalogTypes.Count());
            Assert.Equal(5, context.CatalogBrands.Count());
            Assert.Equal(Enumerable.Range(1, 12), context.CatalogItems.Select(i => i.Id).ToList().Order().ToList());
        }
    }

    [Fact]
    public async Task UseCustomizationData_SeedsFromTheCsvFiles()
    {
        await InitializeAsync(new CatalogSeedOptions { UseCustomizationData = true });

        using var context = CreateContext();

        Assert.Equal(
            ["Mug", "T-Shirt", "Sheet", "USB Memory Stick", "CatalogTypeTestOne", "CatalogTypeTestTwo"],
            context.CatalogTypes.OrderBy(t => t.Id).Select(t => t.Type).ToList());

        Assert.Equal(
            ["Azure", ".NET", "Visual Studio", "SQL Server", "Other", "CatalogBrandTestOne", "CatalogBrandTestTwo"],
            context.CatalogBrands.OrderBy(b => b.Id).Select(b => b.Brand).ToList());

        var items = context.CatalogItems.OrderBy(i => i.Id).ToList();

        Assert.Equal(13, items.Count);
        Assert.Equal(Enumerable.Range(1, 13), items.Select(i => i.Id));

        // The CSV overrides the hard-coded data: extra row, per-item stock and the OnReorder flag.
        var mug = items.Single(i => i.Name == ".NET Black & White Mug");
        Assert.Equal(89, mug.AvailableStock);
        Assert.True(mug.OnReorder);
        Assert.Equal(".NET Bot Black Hoodie, and more", items.First().Description);
        Assert.Equal("pepito", items.Last().Name);
    }

    [Fact]
    public async Task UseCustomizationData_ResolvesForeignKeysAgainstTheSeededCsvTypesAndBrands()
    {
        await InitializeAsync(new CatalogSeedOptions { UseCustomizationData = true });

        using var context = CreateContext();

        var tShirtId = context.CatalogTypes.Single(t => t.Type == "T-Shirt").Id;
        var dotNetBrandId = context.CatalogBrands.Single(b => b.Brand == ".NET").Id;
        var hoodie = context.CatalogItems.Single(i => i.Name == ".NET Bot Black Hoodie");

        Assert.Equal(tShirtId, hoodie.CatalogTypeId);
        Assert.Equal(dotNetBrandId, hoodie.CatalogBrandId);
    }

    private async Task InitializeAsync(CatalogSeedOptions options)
    {
        using var context = CreateContext();

        var initializer = new CatalogDatabaseInitializer(
            context,
            new HiLoCatalogItemIdGenerator(new SqliteCatalogSequenceProvider()),
            Options.Create(options),
            NullLogger<CatalogDatabaseInitializer>.Instance);

        await initializer.InitializeAsync(CancellationToken.None);
    }

    private CatalogDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);
}
