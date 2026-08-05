using eShop.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Data.Tests;

public class CatalogDbContextCrudTests : IDisposable
{
    private readonly SqliteCatalogDatabase _database = new();

    [Fact]
    public async Task CatalogBrand_RoundTrips()
    {
        await using (var context = _database.CreateContext())
        {
            context.CatalogBrands.Add(new CatalogBrand { Brand = "Azure" });
            await context.SaveChangesAsync();
        }

        int brandId;
        await using (var context = _database.CreateContext())
        {
            var brand = await context.CatalogBrands.SingleAsync(b => b.Brand == "Azure");
            brandId = brand.Id;
            Assert.NotEqual(0, brandId);

            brand.Brand = "Azure DevOps";
            await context.SaveChangesAsync();
        }

        await using (var context = _database.CreateContext())
        {
            Assert.Equal("Azure DevOps", (await context.CatalogBrands.FindAsync(brandId))!.Brand);

            context.CatalogBrands.Remove((await context.CatalogBrands.FindAsync(brandId))!);
            await context.SaveChangesAsync();
        }

        await using (var context = _database.CreateContext())
        {
            Assert.Null(await context.CatalogBrands.FindAsync(brandId));
        }
    }

    [Fact]
    public async Task CatalogType_RoundTrips()
    {
        await using (var context = _database.CreateContext())
        {
            context.CatalogTypes.Add(new CatalogType { Type = "Mug" });
            await context.SaveChangesAsync();
        }

        await using (var context = _database.CreateContext())
        {
            var type = await context.CatalogTypes.SingleAsync(t => t.Type == "Mug");
            type.Type = "Cup";
            await context.SaveChangesAsync();
        }

        await using (var context = _database.CreateContext())
        {
            var type = await context.CatalogTypes.SingleAsync(t => t.Type == "Cup");

            context.CatalogTypes.Remove(type);
            await context.SaveChangesAsync();

            Assert.False(await context.CatalogTypes.AnyAsync(t => t.Id == type.Id));
        }
    }

    [Fact]
    public async Task CatalogItem_RoundTripsWithExplicitKeyAndNavigations()
    {
        await using (var context = _database.CreateContext())
        {
            var brand = new CatalogBrand { Brand = ".NET" };
            var type = new CatalogType { Type = "T-Shirt" };
            context.CatalogBrands.Add(brand);
            context.CatalogTypes.Add(type);
            await context.SaveChangesAsync();

            context.CatalogItems.Add(new CatalogItem
            {
                Id = 4242,
                Name = ".NET Bot Black Hoodie",
                Description = ".NET Bot Black Hoodie",
                Price = 19.5M,
                PictureFileName = "1.png",
                CatalogBrandId = brand.Id,
                CatalogTypeId = type.Id,
                AvailableStock = 100,
            });
            await context.SaveChangesAsync();
        }

        await using (var context = _database.CreateContext())
        {
            var item = await context.CatalogItems
                .Include(ci => ci.CatalogBrand)
                .Include(ci => ci.CatalogType)
                .SingleAsync(ci => ci.Id == 4242);

            Assert.Equal(".NET", item.CatalogBrand!.Brand);
            Assert.Equal("T-Shirt", item.CatalogType!.Type);
            Assert.Equal(19.5M, item.Price);

            item.Price = 21.0M;
            await context.SaveChangesAsync();
        }

        await using (var context = _database.CreateContext())
        {
            var item = await context.CatalogItems.SingleAsync(ci => ci.Id == 4242);
            Assert.Equal(21.0M, item.Price);

            context.CatalogItems.Remove(item);
            await context.SaveChangesAsync();

            Assert.False(await context.CatalogItems.AnyAsync());
        }
    }

    [Fact]
    public async Task CatalogItem_WithoutExplicitKey_IsNotAssignedADatabaseGeneratedId()
    {
        await using var context = _database.CreateContext();

        var brand = new CatalogBrand { Brand = "SQL Server" };
        var type = new CatalogType { Type = "Sheet" };
        context.CatalogBrands.Add(brand);
        context.CatalogTypes.Add(type);
        await context.SaveChangesAsync();

        var item = new CatalogItem
        {
            Name = "Roslyn Red Sheet",
            Price = 8.5M,
            PictureFileName = "5.png",
            CatalogBrandId = brand.Id,
            CatalogTypeId = type.Id,
        };
        context.CatalogItems.Add(item);
        await context.SaveChangesAsync();

        // ValueGeneratedNever: the key stays whatever the caller supplied (0 here); the legacy
        // application feeds it from the dbo.catalog_hilo sequence instead.
        Assert.Equal(0, item.Id);
    }

    [Fact]
    public async Task CatalogItem_RequiresAnExistingBrandAndType()
    {
        await using var context = _database.CreateContext();

        context.CatalogItems.Add(new CatalogItem
        {
            Id = 1,
            Name = "Orphan",
            Price = 1M,
            PictureFileName = "1.png",
            CatalogBrandId = 999,
            CatalogTypeId = 999,
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task CatalogItemsStock_RoundTrips()
    {
        var date = new DateTime(2017, 8, 16, 0, 0, 0, DateTimeKind.Unspecified);

        await using (var context = _database.CreateContext())
        {
            context.CatalogItemsStocks.Add(new CatalogItemsStock
            {
                StockId = 1,
                CatalogItemId = 7,
                Date = date,
                AvailableStock = 100,
            });
            await context.SaveChangesAsync();
        }

        await using (var context = _database.CreateContext())
        {
            var stock = await context.CatalogItemsStocks.SingleAsync(s => s.CatalogItemId == 7 && s.Date == date);
            Assert.Equal(100, stock.AvailableStock);

            stock.AvailableStock = 42;
            await context.SaveChangesAsync();
        }

        await using (var context = _database.CreateContext())
        {
            var stock = await context.CatalogItemsStocks.SingleAsync();
            Assert.Equal(42, stock.AvailableStock);

            context.CatalogItemsStocks.Remove(stock);
            await context.SaveChangesAsync();

            Assert.False(await context.CatalogItemsStocks.AnyAsync());
        }
    }

    [Fact]
    public async Task DiscountItem_RoundTrips()
    {
        await using (var context = _database.CreateContext())
        {
            context.DiscountItems.Add(new DiscountItem
            {
                Size = 0.15,
                Start = new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                End = new DateTime(2017, 12, 31, 0, 0, 0, DateTimeKind.Unspecified),
            });
            await context.SaveChangesAsync();
        }

        await using (var context = _database.CreateContext())
        {
            var discount = await context.DiscountItems.SingleAsync();
            Assert.NotEqual(0, discount.Id);
            Assert.Equal(0.15, discount.Size);

            discount.Size = 0.2;
            await context.SaveChangesAsync();
        }

        await using (var context = _database.CreateContext())
        {
            var discount = await context.DiscountItems.SingleAsync();
            Assert.Equal(0.2, discount.Size);

            context.DiscountItems.Remove(discount);
            await context.SaveChangesAsync();

            Assert.False(await context.DiscountItems.AnyAsync());
        }
    }

    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}
