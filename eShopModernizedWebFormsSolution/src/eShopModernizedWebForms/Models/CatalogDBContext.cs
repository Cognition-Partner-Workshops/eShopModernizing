using Microsoft.EntityFrameworkCore;

namespace eShopModernizedWebForms.Models
{
    /// <summary>
    /// EF Core mapping that reproduces the schema EF6 generated for the legacy Web Forms app
    /// (table/column names, column order, IDENTITY on brand/type, explicit ids on Catalog,
    /// cascade FKs and the dbo.* constraint names), so the port can open a legacy-created database.
    /// </summary>
    public class CatalogDBContext : DbContext
    {
        public CatalogDBContext(DbContextOptions<CatalogDBContext> options) : base(options)
        {
        }

        public DbSet<CatalogItem> CatalogItems { get; set; }

        public DbSet<CatalogBrand> CatalogBrands { get; set; }

        public DbSet<CatalogType> CatalogTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var type = modelBuilder.Entity<CatalogType>();
            type.ToTable("CatalogType");
            type.HasKey(ci => ci.Id).HasName("PK_dbo.CatalogType");
            type.Property(ci => ci.Id).UseIdentityColumn().HasColumnOrder(0);
            type.Property(cb => cb.Type).IsRequired().HasMaxLength(100).HasColumnOrder(1);

            var brand = modelBuilder.Entity<CatalogBrand>();
            brand.ToTable("CatalogBrand");
            brand.HasKey(ci => ci.Id).HasName("PK_dbo.CatalogBrand");
            brand.Property(ci => ci.Id).UseIdentityColumn().HasColumnOrder(0);
            brand.Property(cb => cb.Brand).IsRequired().HasMaxLength(100).HasColumnOrder(1);

            var item = modelBuilder.Entity<CatalogItem>();
            item.ToTable("Catalog");
            item.HasKey(ci => ci.Id).HasName("PK_dbo.Catalog");
            item.Property(ci => ci.Id).ValueGeneratedNever().HasColumnOrder(0);
            item.Property(ci => ci.Name).IsRequired().HasMaxLength(50).HasColumnOrder(1);
            item.Property(ci => ci.Description).HasColumnOrder(2);
            item.Property(ci => ci.Price).IsRequired().HasPrecision(18, 2).HasColumnOrder(3);
            item.Property(ci => ci.PictureFileName).IsRequired().HasColumnOrder(4);
            item.Property(ci => ci.CatalogTypeId).HasColumnOrder(5);
            item.Property(ci => ci.CatalogBrandId).HasColumnOrder(6);
            item.Property(ci => ci.AvailableStock).HasColumnOrder(7);
            item.Property(ci => ci.RestockThreshold).HasColumnOrder(8);
            item.Property(ci => ci.MaxStockThreshold).HasColumnOrder(9);
            item.Property(ci => ci.OnReorder).HasColumnOrder(10);
            item.Ignore(ci => ci.PictureUri);
            item.HasIndex(ci => ci.CatalogTypeId).HasDatabaseName("IX_CatalogTypeId");
            item.HasIndex(ci => ci.CatalogBrandId).HasDatabaseName("IX_CatalogBrandId");
            item.HasOne(ci => ci.CatalogBrand).WithMany().HasForeignKey(ci => ci.CatalogBrandId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_dbo.Catalog_dbo.CatalogBrand_CatalogBrandId");
            item.HasOne(ci => ci.CatalogType).WithMany().HasForeignKey(ci => ci.CatalogTypeId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_dbo.Catalog_dbo.CatalogType_CatalogTypeId");
        }
    }
}
