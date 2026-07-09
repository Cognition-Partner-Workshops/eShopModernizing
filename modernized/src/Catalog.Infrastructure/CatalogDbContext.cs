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

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();

    public DbSet<CatalogBrand> CatalogBrands => Set<CatalogBrand>();

    public DbSet<CatalogType> CatalogTypes => Set<CatalogType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CatalogTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogBrandConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogItemConfiguration());

        // HiLo relies on server-side sequences, which are only supported by the
        // SQL Server provider. Providers that lack sequence support (e.g. the
        // Sqlite provider used by the test suite) fall back to the default
        // store-generated identity strategy configured on each key property.
        if (Database.IsSqlServer())
        {
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
