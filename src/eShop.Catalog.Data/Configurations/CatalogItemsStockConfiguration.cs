using eShop.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Catalog.Data.Configurations;

/// <summary>
/// WCF-only entity, ported from the data annotations on <c>eShopWCFService.CatalogItemsStock</c>:
/// table <c>CatalogItemsStock</c>, application-assigned <c>StockId</c> key and a SQL <c>date</c>
/// column. The legacy model carries no foreign key to <c>Catalog</c> (<c>CatalogItemId</c> is a
/// plain int) and that is preserved so the migration matches the legacy schema.
/// </summary>
public class CatalogItemsStockConfiguration : IEntityTypeConfiguration<CatalogItemsStock>
{
    public void Configure(EntityTypeBuilder<CatalogItemsStock> builder)
    {
        builder.ToTable("CatalogItemsStock");

        builder.HasKey(s => s.StockId);

        builder.Property(s => s.StockId)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(s => s.Date)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(s => s.CatalogItemId)
            .IsRequired();

        builder.Property(s => s.AvailableStock)
            .IsRequired();
    }
}
