using eShop.Catalog.Data;
using eShop.Catalog.Data.Sequences;
using eShop.Catalog.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Data.Tests;

/// <summary>
/// A throwaway SQLite in-memory database built from the EF Core model. SQLite is used instead of a
/// SQL Server testcontainer so the suite runs unattended on a Linux CI agent; the SQL Server
/// migration is verified separately (see modernization/README.md).
/// </summary>
public sealed class CatalogDbContextFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public CatalogDbContextFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
        Seed(context);
    }

    /// <summary>
    /// The HiLo generator that seeded the fixture, shared with the services under test exactly as
    /// the singleton registration shares it across requests.
    /// </summary>
    public ICatalogItemIdGenerator IdGenerator { get; } =
        new HiLoCatalogItemIdGenerator(new SqliteCatalogSequenceProvider());

    public CatalogDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(_connection)
            .Options);

    public ICatalogService CreateService(CatalogDbContext context) =>
        new CatalogService(context, IdGenerator);

    public void Dispose() => _connection.Dispose();

    private void Seed(CatalogDbContext context)
    {
        context.CatalogBrands.AddRange(PreconfiguredData.GetPreconfiguredCatalogBrands());
        context.CatalogTypes.AddRange(PreconfiguredData.GetPreconfiguredCatalogTypes());
        context.SaveChanges();

        foreach (var item in PreconfiguredData.GetPreconfiguredCatalogItems())
        {
            item.Id = IdGenerator.GetNextId(context);
            context.CatalogItems.Add(item);
        }

        context.CatalogItemsStocks.Add(new CatalogItemsStock
        {
            StockId = 1,
            CatalogItemId = 1,
            AvailableStock = 42,
            Date = new DateTime(2026, 1, 1),
        });
        context.DiscountItems.Add(new DiscountItem
        {
            Size = 0.25,
            Start = new DateTime(2026, 1, 1),
            End = new DateTime(2026, 12, 31),
        });
        context.SaveChanges();
    }
}
