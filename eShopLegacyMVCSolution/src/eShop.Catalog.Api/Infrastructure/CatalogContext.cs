using eShop.Catalog.Api.Model;
using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Api.Infrastructure;

public class CatalogContext : DbContext
{
    public const string Schema = "catalog";
    public const string ItemSequence = "catalog_hilo";
    public const string BrandSequence = "catalog_brand_hilo";
    public const string TypeSequence = "catalog_type_hilo";

    public CatalogContext(DbContextOptions<CatalogContext> options) : base(options)
    {
    }

    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<CatalogBrand> CatalogBrands => Set<CatalogBrand>();
    public DbSet<CatalogType> CatalogTypes => Set<CatalogType>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        var relational = Database.IsSqlServer();
        if (relational)
        {
            builder.HasDefaultSchema(Schema);
            builder.HasSequence<int>(ItemSequence).StartsAt(1).IncrementsBy(10);
            builder.HasSequence<int>(BrandSequence).StartsAt(1).IncrementsBy(10);
            builder.HasSequence<int>(TypeSequence).StartsAt(1).IncrementsBy(10);
        }

        builder.Entity<CatalogBrand>(e =>
        {
            e.ToTable("CatalogBrand");
            e.HasKey(b => b.Id);
            if (relational) e.Property(b => b.Id).UseHiLo(BrandSequence);
            e.Property(b => b.Brand).IsRequired().HasMaxLength(100);
        });

        builder.Entity<CatalogType>(e =>
        {
            e.ToTable("CatalogType");
            e.HasKey(t => t.Id);
            if (relational) e.Property(t => t.Id).UseHiLo(TypeSequence);
            e.Property(t => t.Type).IsRequired().HasMaxLength(100);
        });

        builder.Entity<CatalogItem>(e =>
        {
            e.ToTable("Catalog");
            e.HasKey(i => i.Id);
            if (relational) e.Property(i => i.Id).UseHiLo(ItemSequence);
            e.Property(i => i.Name).IsRequired().HasMaxLength(50);
            e.Property(i => i.Price).IsRequired().HasColumnType("decimal(18,2)");
            e.Property(i => i.PictureFileName).IsRequired(false);
            e.HasOne(i => i.CatalogBrand).WithMany().HasForeignKey(i => i.CatalogBrandId);
            e.HasOne(i => i.CatalogType).WithMany().HasForeignKey(i => i.CatalogTypeId);
        });
    }
}
