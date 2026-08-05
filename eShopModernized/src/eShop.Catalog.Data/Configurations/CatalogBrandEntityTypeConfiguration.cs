using eShop.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Catalog.Data.Configurations;

public class CatalogBrandEntityTypeConfiguration : IEntityTypeConfiguration<CatalogBrand>
{
    public void Configure(EntityTypeBuilder<CatalogBrand> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CatalogBrand");

        builder.HasKey(cb => cb.Id);

        builder.Property(cb => cb.Id)
            .ValueGeneratedOnAdd();

        builder.Property(cb => cb.Brand)
            .IsRequired()
            .HasMaxLength(50);
    }
}
