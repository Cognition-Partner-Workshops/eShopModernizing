using eShop.Catalog.Data;
using eShop.Catalog.Data.Infrastructure;
using eShop.Catalog.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Data.Tests;

/// <summary>
/// Shared SQLite in-memory database. SQLite (rather than the EF in-memory provider) is used so
/// the tests exercise real relational behaviour: keys, FKs, projections and Include joins.
/// SQL Server is not available on the Linux build agents.
/// </summary>
public sealed class SqliteCatalogDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteCatalogDatabase()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public CatalogDbContext CreateContext()
        => new(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(_connection)
            .Options);

    public CatalogDbContext CreateSeededContext()
    {
        using (var seed = CreateContext())
        {
            if (!seed.CatalogItems.Any())
            {
                seed.CatalogBrands.AddRange(PreconfiguredData.GetPreconfiguredCatalogBrands());
                seed.CatalogTypes.AddRange(PreconfiguredData.GetPreconfiguredCatalogTypes());
                seed.SaveChanges();

                seed.CatalogItems.AddRange(PreconfiguredData.GetPreconfiguredCatalogItems());
                seed.SaveChanges();
            }
        }

        return CreateContext();
    }

    public void Dispose() => _connection.Dispose();
}
