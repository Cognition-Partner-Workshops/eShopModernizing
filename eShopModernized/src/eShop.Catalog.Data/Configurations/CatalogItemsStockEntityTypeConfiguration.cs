using eShop.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Catalog.Data.Configurations;

/// <summary>
/// WCF-only entity: daily available-stock rows read by GetAvailableStock/CreateAvailableStock.
/// </summary>
public class CatalogItemsStockEntityTypeConfiguration : IEntityTypeConfiguration<CatalogItemsStock>
{
    public void Configure(EntityTypeBuilder<CatalogItemsStock> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CatalogItemsStock");

        builder.HasKey(cis => cis.StockId);

        builder.Property(cis => cis.StockId)
            .ValueGeneratedNever();

        builder.Property(cis => cis.Date)
            .HasColumnType("date");

        builder.Property(cis => cis.CatalogItemId)
            .IsRequired();

        builder.Property(cis => cis.AvailableStock)
            .IsRequired();

        builder.HasIndex(cis => new { cis.CatalogItemId, cis.Date });
    }
}
