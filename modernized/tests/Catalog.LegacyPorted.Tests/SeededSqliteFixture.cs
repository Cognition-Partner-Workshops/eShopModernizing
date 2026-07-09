using Catalog.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Catalog.LegacyPorted.Tests;

/// <summary>
/// Shared test infrastructure for the ported legacy suite. Builds
/// <see cref="CatalogDbContext"/> instances over a single open in-memory Sqlite
/// connection seeded from the EF Core <c>HasData</c> baseline (12 items /
/// 5 brands / 4 types). Keeping the connection open for the fixture lifetime
/// preserves the in-memory database across contexts, and every context is built
/// from the same options so writes made through one are visible to the next.
///
/// This replaces the legacy <c>CatalogServiceMock</c> in-memory store that the
/// original MSTest suite relied on; the modernized stack reads/writes through
/// EF Core instead, and Sqlite lets the ported tests run on CI with no external
/// SQL Server.
/// </summary>
public sealed class SeededSqliteFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<CatalogDbContext> _options;

    public SeededSqliteFixture()
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

    public void Dispose() => _connection.Dispose();
}
