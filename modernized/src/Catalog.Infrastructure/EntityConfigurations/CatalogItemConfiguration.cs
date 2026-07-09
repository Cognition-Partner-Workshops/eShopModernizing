using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.EntityConfigurations;

public class CatalogItemConfiguration : IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> builder)
    {
        builder.ToTable("Catalog");

        builder.HasKey(ci => ci.Id);

        // TODO(I-04/NET-42): switch to UseHiLo() to match the legacy raw-SQL
        // HiLo sequence for CatalogItem.Id. For this ticket the Id uses the
        // default store-generated identity strategy.
        builder.Property(ci => ci.Id)
            .ValueGeneratedOnAdd();

        builder.Property(ci => ci.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ci => ci.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(ci => ci.PictureFileName)
            .IsRequired();

        builder.HasOne(ci => ci.CatalogBrand)
            .WithMany()
            .HasForeignKey(ci => ci.CatalogBrandId)
            .IsRequired();

        builder.HasOne(ci => ci.CatalogType)
            .WithMany()
            .HasForeignKey(ci => ci.CatalogTypeId)
            .IsRequired();
    }
}
