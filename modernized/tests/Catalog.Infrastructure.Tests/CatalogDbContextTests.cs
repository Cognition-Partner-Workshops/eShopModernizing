using Catalog.Domain;
using Catalog.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Catalog.Infrastructure.Tests;

/// <summary>
/// Exercises <see cref="CatalogDbContext"/> against the EF Core Sqlite provider
/// using an in-memory connection, so the suite runs on CI with no external DB.
/// </summary>
public class CatalogDbContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<CatalogDbContext> _options;

    public CatalogDbContextTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new CatalogDbContext(_options);
        context.Database.EnsureCreated();
    }

    private CatalogDbContext CreateContext() => new(_options);

    [Fact]
    public void Seed_Produces_Expected_Counts()
    {
        using var context = CreateContext();

        Assert.Equal(12, context.CatalogItems.Count());
        Assert.Equal(5, context.CatalogBrands.Count());
        Assert.Equal(4, context.CatalogTypes.Count());
    }

    [Fact]
    public void Seed_Reproduces_Baseline_Brands()
    {
        using var context = CreateContext();

        var brands = context.CatalogBrands.OrderBy(b => b.Id).Select(b => b.Brand).ToArray();

        Assert.Equal(new[] { "Azure", ".NET", "Visual Studio", "SQL Server", "Other" }, brands);
    }

    [Fact]
    public void Seed_Reproduces_Baseline_Types()
    {
        using var context = CreateContext();

        var types = context.CatalogTypes.OrderBy(t => t.Id).Select(t => t.Type).ToArray();

        Assert.Equal(new[] { "Mug", "T-Shirt", "Sheet", "USB Memory Stick" }, types);
    }

    [Fact]
    public void CatalogItem_Crud_RoundTrips()
    {
        int newId;

        using (var context = CreateContext())
        {
            var item = new CatalogItem
            {
                Name = "Test Item",
                Description = "Test Description",
                Price = 42.75M,
                PictureFileName = "test.png",
                CatalogTypeId = 1,
                CatalogBrandId = 1,
                AvailableStock = 10,
            };

            context.CatalogItems.Add(item);
            context.SaveChanges();
            newId = item.Id;
        }

        Assert.True(newId > 0);

        using (var context = CreateContext())
        {
            var read = context.CatalogItems.Single(i => i.Id == newId);
            Assert.Equal("Test Item", read.Name);
            Assert.Equal(42.75M, read.Price);

            read.Price = 50.00M;
            read.Name = "Updated Item";
            context.SaveChanges();
        }

        using (var context = CreateContext())
        {
            var updated = context.CatalogItems.Single(i => i.Id == newId);
            Assert.Equal("Updated Item", updated.Name);
            Assert.Equal(50.00M, updated.Price);

            context.CatalogItems.Remove(updated);
            context.SaveChanges();
        }

        using (var context = CreateContext())
        {
            Assert.False(context.CatalogItems.Any(i => i.Id == newId));
        }
    }

    [Fact]
    public void CatalogItem_Include_Loads_Navigation()
    {
        using var context = CreateContext();

        var item = context.CatalogItems
            .Include(i => i.CatalogBrand)
            .Include(i => i.CatalogType)
            .Single(i => i.Id == 1);

        Assert.NotNull(item.CatalogBrand);
        Assert.NotNull(item.CatalogType);
        Assert.Equal(".NET", item.CatalogBrand!.Brand);
        Assert.Equal("T-Shirt", item.CatalogType!.Type);
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
