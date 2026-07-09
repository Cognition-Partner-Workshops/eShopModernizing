using Catalog.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Mvc.Tests;

/// <summary>
/// Builds <see cref="CatalogDbContext"/> instances backed by a shared in-memory
/// Sqlite connection seeded with the behavioral-baseline dataset (12 items,
/// 5 brands, 4 types). Keeping the connection open for the lifetime of the test
/// preserves the in-memory database, and every context is created from the same
/// options so the controller sees the seeded data. Runs on CI with no external DB.
/// </summary>
public sealed class CatalogDbContextFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<CatalogDbContext> _options;

    public CatalogDbContextFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new CatalogDbContext(_options);
        context.Database.EnsureCreated();
    }

    public CatalogDbContext CreateContext() => new(_options);

    public void Dispose()
    {
        _connection.Dispose();
    }
}
