using eShop.Catalog.Data.Sequences;
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

        // The three legacy sequences (Models/Infrastructure/dbo.catalog_*.Sequence.sql). Ids stay
        // application-allocated (Catalog.Id remains ValueGeneratedNever); the sequences are part of
        // the model so the migration creates them with the legacy start and increment. Providers
        // without sequence objects — the SQLite test databases — emulate them instead, see
        // SqliteCatalogSequenceProvider.
        if (Database.IsSqlServer())
        {
            foreach (var sequence in CatalogSequences.All)
            {
                modelBuilder.HasSequence<long>(sequence, CatalogSequences.Schema)
                    .StartsAt(CatalogSequences.StartValue)
                    .IncrementsBy(CatalogSequences.Increment);
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}
