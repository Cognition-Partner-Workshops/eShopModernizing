using eShopWcf.Catalog.Data;
using Microsoft.EntityFrameworkCore;

namespace eShopWcf.Catalog.Grpc.Tests;

public sealed class EmptyDatabaseInitializerTests : IAsyncLifetime
{
    private static readonly int[] EmptyCounts = [0, 0, 0, 0, 0];
    private static readonly int[] LegacyCounts = [5, 4, 12, 6, 6];
    private readonly DbContextOptions<CatalogDbContext> _options = new DbContextOptionsBuilder<CatalogDbContext>()
        .UseSqlServer($@"Server=(localdb)\MSSQLLocalDB;Database=eShopWcf_InitializerTest_{Guid.NewGuid():N};Integrated Security=true;TrustServerCertificate=true")
        .Options;

    public async Task InitializeAsync()
    {
        await using var db = new CatalogDbContext(_options);
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = new CatalogDbContext(_options);
        await db.Database.EnsureDeletedAsync();
    }

    [Theory]
    [InlineData("Legacy", 5, 4, 12, 6)]
    [InlineData("Csv", 7, 6, 13, 13)]
    public async Task SchemaCreatedBeforeInterruptedSeedIsRecovered(string dataset, int brands, int types, int items, int stock)
    {
        await using var db = new CatalogDbContext(_options);
        Assert.Equal(EmptyCounts, await CountsAsync(db));

        await CatalogDatabaseInitializer.InitializeAsync(db, dataset);
        Assert.Equal(new[] { brands, types, items, stock, 6 }, await CountsAsync(db));
        Assert.Equal(19.5M, (await db.CatalogItems.SingleAsync(x => x.Id == 1)).Price);

        await CatalogDatabaseInitializer.InitializeAsync(db, "invalid-but-unused-for-populated-db");
        Assert.Equal(new[] { brands, types, items, stock, 6 }, await CountsAsync(db));
    }

    [Theory]
    [InlineData("brand")]
    [InlineData("type")]
    [InlineData("item")]
    [InlineData("stock")]
    [InlineData("discount")]
    public async Task AnyPopulatedCatalogTablePreventsSeeding(string table)
    {
        await using var db = new CatalogDbContext(_options);
        object sentinel = table switch
        {
            "brand" => new CatalogBrand { Id = 99, Brand = "sentinel" },
            "type" => new CatalogType { Id = 99, Type = "sentinel" },
            "item" => new CatalogItem
            {
                Id = 99,
                Name = "sentinel",
                CatalogBrand = new CatalogBrand { Id = 99, Brand = "sentinel" },
                CatalogType = new CatalogType { Id = 99, Type = "sentinel" },
            },
            "stock" => new CatalogItemsStock { StockId = 99, CatalogItemId = 99, Date = new DateTime(2017, 9, 20), AvailableStock = 99 },
            "discount" => new DiscountItem { Start = new DateTime(2017, 9, 20), End = new DateTime(2017, 9, 21), Size = 0.99 },
            _ => throw new ArgumentOutOfRangeException(nameof(table)),
        };
        db.Add(sentinel);
        await db.SaveChangesAsync();
        var before = await CountsAsync(db);

        await CatalogDatabaseInitializer.InitializeAsync(db, "invalid-but-unused-for-populated-db");

        Assert.Equal(before, await CountsAsync(db));
    }

    [Fact]
    public async Task FailedRecoveryRollsBackAllSeedTablesAndCanBeRetried()
    {
        await using var db = new CatalogDbContext(_options);
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.DiscountItems ADD CONSTRAINT CK_RejectSeed CHECK (Size < 0)");

        await Assert.ThrowsAsync<DbUpdateException>(() => CatalogDatabaseInitializer.InitializeAsync(db));
        db.ChangeTracker.Clear();
        Assert.Equal(EmptyCounts, await CountsAsync(db));

        await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.DiscountItems DROP CONSTRAINT CK_RejectSeed");
        await using var retry = new CatalogDbContext(_options);
        await CatalogDatabaseInitializer.InitializeAsync(retry);
        Assert.Equal(LegacyCounts, await CountsAsync(retry));
    }

    [Fact]
    public async Task IncompleteExistingCatalogSchemaIsNotModified()
    {
        await using var db = new CatalogDbContext(_options);
        await db.Database.ExecuteSqlRawAsync("DROP TABLE dbo.CatalogItemsStock");

        await CatalogDatabaseInitializer.InitializeAsync(db, "invalid-but-unused-for-incomplete-schema");

        Assert.Empty(await db.CatalogBrands.ToListAsync());
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE object_id = OBJECT_ID(N'dbo.CatalogItemsStock')").SingleAsync());
    }

    private static async Task<int[]> CountsAsync(CatalogDbContext db) =>
    [
        await db.CatalogBrands.CountAsync(),
        await db.CatalogTypes.CountAsync(),
        await db.CatalogItems.CountAsync(),
        await db.CatalogItemsStocks.CountAsync(),
        await db.DiscountItems.CountAsync(),
    ];
}
