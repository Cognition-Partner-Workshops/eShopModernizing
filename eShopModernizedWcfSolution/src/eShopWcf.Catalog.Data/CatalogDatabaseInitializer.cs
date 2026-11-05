using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace eShopWcf.Catalog.Data;

public static class CatalogDatabaseInitializer
{
    public static async Task InitializeAsync(CatalogDbContext db, string dataset = "Legacy", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        if (await db.GetService<IRelationalDatabaseCreator>().ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var seed = dataset switch
        {
            "Legacy" => new CatalogSeed(
                PreconfiguredData.GetPreconfiguredCatalogBrands().ToList(),
                PreconfiguredData.GetPreconfiguredCatalogTypes().ToList(),
                PreconfiguredData.GetPreconfiguredCatalogItems(),
                PreconfiguredData.GetPreconfiguredCatalogItemsStock().ToList()),
            "Csv" => CatalogCsvSeed.Load(),
            _ => throw new ArgumentException("Seed:Dataset must be Legacy or Csv.", nameof(dataset)),
        };

        await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        db.AddRange(seed.Brands);
        db.AddRange(seed.Types);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        db.AddRange(seed.Items);
        db.AddRange(seed.Stock);
        db.AddRange(PreconfiguredData.GetPreconfiguredDiscountItems());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal sealed record CatalogSeed(
    List<CatalogBrand> Brands,
    List<CatalogType> Types,
    List<CatalogItem> Items,
    List<CatalogItemsStock> Stock);
