using eShop.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Data;

/// <summary>
/// EF Core 8 replacement for the legacy EF6 <c>CatalogDBContext</c> (MVC/Web Forms) and
/// <c>EntityModel</c> (WCF), consolidated onto a single catalog database.
/// </summary>
public class CatalogDbContext : DbContext
{
    /// <summary>
    /// Sequence backing the application-side HiLo generator for <see cref="CatalogItem.Id" />.
    /// </summary>
    public const string CatalogItemHiLoSequenceName = "catalog_hilo";

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
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // SQLite (used by the tests) has no sequences; the sequence only matters for the
        // SQL Server schema the legacy application shipped.
        if (Database.IsSqlServer())
        {
            modelBuilder.HasSequence<long>(CatalogItemHiLoSequenceName)
                .StartsAt(1)
                .IncrementsBy(10);
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
