using Microsoft.EntityFrameworkCore;

namespace eShopWcf.Catalog.Data;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<CatalogBrand> CatalogBrands => Set<CatalogBrand>();
    public DbSet<CatalogType> CatalogTypes => Set<CatalogType>();
    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<CatalogItemsStock> CatalogItemsStocks => Set<CatalogItemsStock>();
    public DbSet<DiscountItem> DiscountItems => Set<DiscountItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dbo");
        var brands = modelBuilder.Entity<CatalogBrand>();
        brands.ToTable("CatalogBrands");
        brands.HasKey(x => x.Id);
        brands.Property(x => x.Id).ValueGeneratedNever();
        brands.Property(x => x.Brand).HasColumnType("varchar(50)").IsRequired(false);

        var types = modelBuilder.Entity<CatalogType>();
        types.ToTable("CatalogTypes");
        types.HasKey(x => x.Id);
        types.Property(x => x.Id).ValueGeneratedNever();
        types.Property(x => x.Type).HasColumnType("varchar(50)").IsRequired(false);

        var items = modelBuilder.Entity<CatalogItem>();
        items.ToTable("CatalogItems");
        items.HasKey(x => x.Id);
        items.Property(x => x.Id).ValueGeneratedNever();
        items.Property(x => x.Description).HasColumnType("nvarchar(max)").IsRequired(false);
        items.Property(x => x.Name).HasColumnType("nvarchar(max)").IsRequired(false);
        items.Property(x => x.Picturefilename).HasColumnType("nvarchar(max)").IsRequired(false);
        items.Property(x => x.Price).HasColumnType("money");
        items.HasOne(x => x.CatalogBrand).WithMany().HasForeignKey(x => x.CatalogBrandId).OnDelete(DeleteBehavior.Cascade);
        items.HasOne(x => x.CatalogType).WithMany().HasForeignKey(x => x.CatalogTypeId).OnDelete(DeleteBehavior.Cascade);
        items.HasIndex(x => x.CatalogBrandId).HasDatabaseName("IX_CatalogBrandId");
        items.HasIndex(x => x.CatalogTypeId).HasDatabaseName("IX_CatalogTypeId");

        var stock = modelBuilder.Entity<CatalogItemsStock>();
        stock.ToTable("CatalogItemsStock");
        stock.HasKey(x => x.StockId);
        stock.Property(x => x.StockId).ValueGeneratedNever();
        stock.Property(x => x.Date).HasColumnType("date");

        var discounts = modelBuilder.Entity<DiscountItem>();
        discounts.ToTable("DiscountItems");
        discounts.HasKey(x => x.Id);
        discounts.Property(x => x.Id).UseIdentityColumn();
        discounts.Property(x => x.Size).HasColumnType("float");
        discounts.Property(x => x.Start).HasColumnType("date");
        discounts.Property(x => x.End).HasColumnType("date");
    }
}
