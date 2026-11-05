using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace eShopWcf.Catalog.Data;

public static class CatalogDatabaseInitializer
{
    public static async Task InitializeAsync(CatalogDbContext db, string dataset = "Legacy", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        var exists = await db.GetService<IRelationalDatabaseCreator>().ExistsAsync(cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            var tableCount = await db.Database.SqlQueryRaw<int>("""
                SELECT COUNT(*) AS [Value] FROM sys.tables
                WHERE schema_id = SCHEMA_ID(N'dbo')
                AND name IN (N'CatalogBrands', N'CatalogTypes', N'CatalogItems', N'CatalogItemsStock', N'DiscountItems')
                """).SingleAsync(cancellationToken).ConfigureAwait(false);
            if (tableCount != 5
                || await db.CatalogBrands.AnyAsync(cancellationToken).ConfigureAwait(false)
                || await db.CatalogTypes.AnyAsync(cancellationToken).ConfigureAwait(false)
                || await db.CatalogItems.AnyAsync(cancellationToken).ConfigureAwait(false)
                || await db.CatalogItemsStocks.AnyAsync(cancellationToken).ConfigureAwait(false)
                || await db.DiscountItems.AnyAsync(cancellationToken).ConfigureAwait(false))
            {
                return;
            }
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

        if (!exists)
        {
            await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        }
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
