using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.EntityConfigurations;

public class CatalogItemsStockConfiguration : IEntityTypeConfiguration<CatalogItemsStock>
{
    public void Configure(EntityTypeBuilder<CatalogItemsStock> builder)
    {
        builder.ToTable("CatalogItemsStock");

        builder.HasKey(s => s.StockId);

        builder.Property(s => s.StockId)
            .ValueGeneratedOnAdd();

        builder.Property(s => s.CatalogItemId)
            .IsRequired();

        builder.Property(s => s.Date)
            .IsRequired();

        builder.Property(s => s.AvailableStock)
            .IsRequired();
    }
}
