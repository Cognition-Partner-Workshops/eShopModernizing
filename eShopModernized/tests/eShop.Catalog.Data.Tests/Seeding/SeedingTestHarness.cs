using eShop.Catalog.Data.Seeding;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Data.Tests.Seeding;

/// <summary>
/// Emulates <c>SELECT NEXT VALUE FOR catalog_hilo</c> (start 1, increment 10) so the HiLo id
/// allocation can be exercised without SQL Server.
/// </summary>
public sealed class FakeCatalogHiLoSequence : ICatalogHiLoSequence
{
    private long _next = 1;

    /// <summary>Number of times the sequence was read.</summary>
    public int ReadCount { get; private set; }

    public Task<long> GetNextValueAsync(CatalogDbContext context, CancellationToken cancellationToken)
    {
        ReadCount++;
        var value = _next;
        _next += CatalogItemHiLoGenerator.HiLoIncrement;

        return Task.FromResult(value);
    }
}

/// <summary>
/// A throwaway SQLite catalog database plus throwaway Setup/Pics folders, wired into a
/// <see cref="CatalogSeeder"/>. SQLite is used because the migrations' <c>CREATE SEQUENCE</c> is
/// SQL Server-only; the real provider is covered by <see cref="CatalogSeedingSqlServerTests"/>.
/// </summary>
public sealed class SeedingTestHarness : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly string _root;
    private readonly CatalogItemHiLoGenerator _idGenerator;

    public SeedingTestHarness()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        using (var context = CreateContext())
        {
            context.Database.EnsureCreated();
        }

        _root = Path.Combine(Path.GetTempPath(), "eshop-seeding-tests", Guid.NewGuid().ToString("N"));
        Folders = new CatalogSeedFolders(Path.Combine(_root, "Setup"), Path.Combine(_root, "Pics"));
        Directory.CreateDirectory(Folders.SetupFolder);

        _idGenerator = new CatalogItemHiLoGenerator(Sequence);
    }

    /// <summary>Setup and Pics folders handed to the seeder.</summary>
    public CatalogSeedFolders Folders { get; }

    /// <summary>Sequence stand-in shared by every seeder this harness creates.</summary>
    public FakeCatalogHiLoSequence Sequence { get; } = new();

    public CatalogDbContext CreateContext()
        => new(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(_connection)
            .Options);

    /// <summary>Creates a seeder over a fresh context, sharing the harness' id generator state.</summary>
    public CatalogSeeder CreateSeeder(CatalogDbContext context, bool useCustomizationData = false)
        => new(context, _idGenerator, Folders, useCustomizationData);

    /// <summary>Copies a seed asset shipped in the build output into the harness' Setup folder.</summary>
    public void CopySetupAsset(string fileName, string? contents = null)
    {
        var destination = Path.Combine(Folders.SetupFolder, fileName);

        if (contents is null)
        {
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Setup", fileName), destination, overwrite: true);
        }
        else
        {
            File.WriteAllText(destination, contents);
        }
    }

    public void Dispose()
    {
        _idGenerator.Dispose();
        _connection.Dispose();

        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
