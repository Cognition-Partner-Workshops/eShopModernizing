using Catalog.Domain;
using Catalog.Infrastructure.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

public class CatalogDbContext : DbContext
{
    // Legacy HiLo sequence names (EF6 MVC/WebForms baseline). Preserved so the
    // modernized EF Core layer generates ids the same way as the legacy app.
    internal const string CatalogItemHiLoSequence = "catalog_hilo";
    internal const string CatalogBrandHiLoSequence = "catalog_brand_hilo";
    internal const string CatalogTypeHiLoSequence = "catalog_type_hilo";

    // HiLo block size (must match the increment baked into each sequence) and
    // the count of explicitly-seeded rows per entity. The sequences start at
    // (seeded count + one block) so generated ids never collide with seed ids.
    private const int HiLoBlockSize = 10;
    private const int SeededCatalogItemCount = 12;
    private const int SeededCatalogBrandCount = 5;
    private const int SeededCatalogTypeCount = 4;

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();

    public DbSet<CatalogBrand> CatalogBrands => Set<CatalogBrand>();

    public DbSet<CatalogType> CatalogTypes => Set<CatalogType>();

    public DbSet<CatalogItemsStock> CatalogItemsStocks => Set<CatalogItemsStock>();

    public DbSet<DiscountItem> DiscountItems => Set<DiscountItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CatalogTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogBrandConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogItemConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogItemsStockConfiguration());
        modelBuilder.ApplyConfiguration(new DiscountItemConfiguration());

        // HiLo relies on server-side sequences, which are only supported by the
        // SQL Server provider. Providers that lack sequence support (e.g. the
        // Sqlite provider used by the test suite) fall back to the default
        // store-generated identity strategy configured on each key property.
        if (Database.IsSqlServer())
        {
            // Start each HiLo sequence past the highest explicitly-seeded id
            // (items 1-12, brands 1-5, types 1-4). The seed rows use fixed ids
            // via HasData, so a sequence starting at the default 1 would hand
            // out ids that collide with seeded rows on the first insert
            // (PRIMARY KEY violation). IncrementsBy(10) matches the HiLo block
            // size configured by UseHiLo.
            modelBuilder.HasSequence<int>(CatalogItemHiLoSequence)
                .StartsAt(SeededCatalogItemCount + HiLoBlockSize)
                .IncrementsBy(HiLoBlockSize);
            modelBuilder.HasSequence<int>(CatalogBrandHiLoSequence)
                .StartsAt(SeededCatalogBrandCount + HiLoBlockSize)
                .IncrementsBy(HiLoBlockSize);
            modelBuilder.HasSequence<int>(CatalogTypeHiLoSequence)
                .StartsAt(SeededCatalogTypeCount + HiLoBlockSize)
                .IncrementsBy(HiLoBlockSize);

            modelBuilder.Entity<CatalogItem>()
                .Property(ci => ci.Id)
                .UseHiLo(CatalogItemHiLoSequence);

            modelBuilder.Entity<CatalogBrand>()
                .Property(cb => cb.Id)
                .UseHiLo(CatalogBrandHiLoSequence);

            modelBuilder.Entity<CatalogType>()
                .Property(ct => ct.Id)
                .UseHiLo(CatalogTypeHiLoSequence);
        }

        modelBuilder.SeedCatalog();

        base.OnModelCreating(modelBuilder);
    }
}
