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
            type.HasKey(x => x.Id);
            type.Property(x => x.Id).UseHiLo("catalog_type_hilo");
            type.Property(x => x.Type).IsRequired().HasMaxLength(100);
            var brand = builder.Entity<CatalogBrand>();
            brand.ToTable("CatalogBrand");
            brand.HasKey(x => x.Id);
            brand.Property(x => x.Id).UseHiLo("catalog_brand_hilo");
            brand.Property(x => x.Brand).IsRequired().HasMaxLength(100);
            var item = builder.Entity<CatalogItem>();
            item.ToTable("Catalog");
            item.HasKey(x => x.Id);
            item.Property(x => x.Id).UseHiLo("catalog_hilo");
            item.Property(x => x.Name).IsRequired().HasMaxLength(50);
            item.Property(x => x.Price).IsRequired().HasPrecision(18, 2);
            item.Property(x => x.PictureFileName).IsRequired();
            item.Ignore(x => x.PictureUri);
            item.HasOne(x => x.CatalogBrand).WithMany().HasForeignKey(x => x.CatalogBrandId).OnDelete(DeleteBehavior.Restrict);
            item.HasOne(x => x.CatalogType).WithMany().HasForeignKey(x => x.CatalogTypeId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
