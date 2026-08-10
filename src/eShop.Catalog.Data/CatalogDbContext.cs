using eShop.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Data;

/// <summary>
/// EF Core 8 port of the three legacy EF6 contexts (MVC <c>CatalogDBContext</c>, Web Forms
/// <c>CatalogDBContext</c> and WCF <c>EntityModel</c>), reconciled into a single model.
/// See Configurations/ for the per-entity mapping and the divergences that were resolved.
/// </summary>
public class CatalogDbContext : DbContext
{
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);

        // SEAM (NET-65): the HiLo sequences (catalog_hilo, catalog_brand_hilo, catalog_type_hilo)
        // and the preconfigured-data seeding that the legacy CreateDatabaseIfNotExists initializers
        // performed are deliberately NOT part of this model. NET-65 adds them, either as
        // modelBuilder.HasSequence(...) plus UseHiLo(...) or as a separate seeding migration.
        base.OnModelCreating(modelBuilder);
    }
}
