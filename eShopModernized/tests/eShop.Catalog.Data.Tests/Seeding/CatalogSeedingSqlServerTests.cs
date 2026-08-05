using eShop.Catalog.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eShop.Catalog.Data.Tests.Seeding;

/// <summary>
/// End-to-end check of "migrate then seed" against a Testcontainers SQL Server: the real
/// <c>dbo.catalog_hilo</c> hands out the catalog item ids and the real IDENTITY columns hand out
/// the brand/type ids.
/// </summary>
public class CatalogSeedingSqlServerTests : IClassFixture<SqlServerSeedingFixture>
{
    private readonly SqlServerSeedingFixture _sqlServer;

    public CatalogSeedingSqlServerTests(SqlServerSeedingFixture sqlServer) => _sqlServer = sqlServer;

    [RequiresDockerFact]
    public async Task Migrating_and_seeding_a_fresh_database_reproduces_the_legacy_catalog()
    {
        var state = new CatalogSeedingState();
        await using var context = CreateContext(nameof(Migrating_and_seeding_a_fresh_database_reproduces_the_legacy_catalog));

        await InitializeAsync(context, state);

        Assert.True(state.IsInitialized);
        Assert.Equal(
            ["Azure", ".NET", "Visual Studio", "SQL Server", "Other"],
            await context.CatalogBrands.OrderBy(b => b.Id).Select(b => b.Brand).ToListAsync());
        Assert.Equal([1, 2, 3, 4, 5], await context.CatalogBrands.OrderBy(b => b.Id).Select(b => b.Id).ToListAsync());
        Assert.Equal([1, 2, 3, 4], await context.CatalogTypes.OrderBy(t => t.Id).Select(t => t.Id).ToListAsync());

        var items = await context.CatalogItems.OrderBy(i => i.Id).ToListAsync();
        Assert.Equal(Enumerable.Range(1, 12), items.Select(i => i.Id));
        Assert.Equal(".NET Bot Black Hoodie", items[0].Name);
        Assert.Equal("1.png", items[0].PictureFileName);
        Assert.Equal("Prism White TShirt", items[11].Name);
    }

    [RequiresDockerFact]
    public async Task The_hi_lo_sequence_hands_out_ids_in_blocks_of_ten_starting_at_one()
    {
        await using var context = CreateContext(nameof(The_hi_lo_sequence_hands_out_ids_in_blocks_of_ten_starting_at_one));
        await context.Database.MigrateAsync();

        var sequence = new CatalogHiLoSequence();

        Assert.Equal(1, await sequence.GetNextValueAsync(context, CancellationToken.None));
        Assert.Equal(11, await sequence.GetNextValueAsync(context, CancellationToken.None));
        Assert.Equal(21, await sequence.GetNextValueAsync(context, CancellationToken.None));

        var sequences = await context.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sys.sequences")
            .ToListAsync();
        Assert.Equal([CatalogDbContext.CatalogItemHiLoSequenceName], sequences);
    }

    [RequiresDockerFact]
    public async Task Running_the_initializer_twice_does_not_duplicate_data()
    {
        var state = new CatalogSeedingState();
        await using var context = CreateContext(nameof(Running_the_initializer_twice_does_not_duplicate_data));

        await InitializeAsync(context, state);
        await InitializeAsync(context, state);

        Assert.Equal(5, await context.CatalogBrands.CountAsync());
        Assert.Equal(4, await context.CatalogTypes.CountAsync());
        Assert.Equal(12, await context.CatalogItems.CountAsync());
    }

    private static async Task InitializeAsync(CatalogDbContext context, CatalogSeedingState state)
    {
        var folders = new CatalogSeedFolders(
            Path.Combine(AppContext.BaseDirectory, "Setup"),
            Path.Combine(AppContext.BaseDirectory, "Pics"));

        // Customization is off, so the pictures folder in the build output is left untouched.
        var seeder = new CatalogSeeder(
            context,
            new CatalogItemHiLoGenerator(new CatalogHiLoSequence()),
            folders,
            useCustomizationData: false);

        var initializer = new CatalogDataInitializer(
            context,
            seeder,
            state,
            NullLogger<CatalogDataInitializer>.Instance);

        await initializer.InitializeAsync(CancellationToken.None);
    }

    private CatalogDbContext CreateContext(string databaseName)
        => new(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlServer(_sqlServer.ConnectionStringFor(databaseName))
            .Options);
}
