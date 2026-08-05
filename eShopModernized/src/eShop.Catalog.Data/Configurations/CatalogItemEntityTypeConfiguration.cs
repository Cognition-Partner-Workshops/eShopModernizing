using eShop.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Catalog.Data.Configurations;

public class CatalogItemEntityTypeConfiguration : IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Catalog");

        builder.HasKey(ci => ci.Id);

        // Legacy DatabaseGeneratedOption.None: ids come from the dbo.catalog_hilo sequence,
        // handed out by the application-side HiLo generator rather than by the database.
        builder.Property(ci => ci.Id)
            .ValueGeneratedNever();

        builder.Property(ci => ci.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ci => ci.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(ci => ci.PictureFileName)
            .IsRequired();

        builder.Ignore(ci => ci.PictureUri);

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
