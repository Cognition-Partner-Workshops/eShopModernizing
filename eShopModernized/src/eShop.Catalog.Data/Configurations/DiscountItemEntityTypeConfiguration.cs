using eShop.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Catalog.Data.Configurations;

/// <summary>
/// WCF-only entity: date-ranged discounts read by GetDiscount.
/// </summary>
public class DiscountItemEntityTypeConfiguration : IEntityTypeConfiguration<DiscountItem>
{
    public void Configure(EntityTypeBuilder<DiscountItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("DiscountItems");

        builder.HasKey(di => di.Id);

        builder.Property(di => di.Id)
            .ValueGeneratedOnAdd();

        builder.Property(di => di.Start)
            .HasColumnType("date");

        builder.Property(di => di.End)
            .HasColumnType("date");

        builder.Property(di => di.Size)
            .IsRequired();
    }
}
