using Microsoft.EntityFrameworkCore;

namespace eShopModernizedMVC.Models
{
    public class CatalogDBContext : DbContext
    {
        public CatalogDBContext(DbContextOptions<CatalogDBContext> options) : base(options) { }

        public DbSet<CatalogItem> CatalogItems { get; set; }
        public DbSet<CatalogBrand> CatalogBrands { get; set; }
        public DbSet<CatalogType> CatalogTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.HasSequence<long>("catalog_hilo").StartsAt(1).IncrementsBy(10);
            builder.HasSequence<long>("catalog_brand_hilo").StartsAt(1).IncrementsBy(10);
            builder.HasSequence<long>("catalog_type_hilo").StartsAt(1).IncrementsBy(10);
            var type = builder.Entity<CatalogType>();
            type.ToTable("CatalogType");
            type.HasKey(x => x.Id).HasName("PK_dbo.CatalogType");
            type.Property(x => x.Id).UseIdentityColumn().HasColumnOrder(0);
            type.Property(x => x.Type).IsRequired().HasMaxLength(100).HasColumnOrder(1);
            var brand = builder.Entity<CatalogBrand>();
            brand.ToTable("CatalogBrand");
            brand.HasKey(x => x.Id).HasName("PK_dbo.CatalogBrand");
            brand.Property(x => x.Id).UseIdentityColumn().HasColumnOrder(0);
            brand.Property(x => x.Brand).IsRequired().HasMaxLength(100).HasColumnOrder(1);
            var item = builder.Entity<CatalogItem>();
            item.ToTable("Catalog");
            item.HasKey(x => x.Id).HasName("PK_dbo.Catalog");
            item.Property(x => x.Id).UseHiLo("catalog_hilo").HasColumnOrder(0);
            item.Property(x => x.Name).IsRequired().HasMaxLength(50).HasColumnOrder(1);
            item.Property(x => x.Description).HasColumnOrder(2);
            item.Property(x => x.Price).IsRequired().HasPrecision(18, 2).HasColumnOrder(3);
            item.Property(x => x.PictureFileName).IsRequired().HasColumnOrder(4);
            item.Property(x => x.CatalogTypeId).HasColumnOrder(5);
            item.Property(x => x.CatalogBrandId).HasColumnOrder(6);
            item.Property(x => x.AvailableStock).HasColumnOrder(7);
            item.Property(x => x.RestockThreshold).HasColumnOrder(8);
            item.Property(x => x.MaxStockThreshold).HasColumnOrder(9);
            item.Property(x => x.OnReorder).HasColumnOrder(10);
            item.Ignore(x => x.PictureUri);
            item.HasIndex(x => x.CatalogBrandId).HasDatabaseName("IX_CatalogBrandId");
            item.HasIndex(x => x.CatalogTypeId).HasDatabaseName("IX_CatalogTypeId");
            item.HasOne(x => x.CatalogBrand).WithMany().HasForeignKey(x => x.CatalogBrandId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_dbo.Catalog_dbo.CatalogBrand_CatalogBrandId");
            item.HasOne(x => x.CatalogType).WithMany().HasForeignKey(x => x.CatalogTypeId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_dbo.Catalog_dbo.CatalogType_CatalogTypeId");
        }
    }
}
