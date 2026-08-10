using eShop.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Catalog.Data.Configurations;

/// <summary>
/// Port of <c>CatalogDBContext.ConfigureCatalogType</c> (MVC / Web Forms). The WCF copy diverges
/// exactly as <see cref="CatalogBrandConfiguration"/> describes (non-unicode <c>varchar(50)</c>,
/// non-generated key, pluralized table) and is resolved the same way.
/// </summary>
public class CatalogTypeConfiguration : IEntityTypeConfiguration<CatalogType>
{
    public void Configure(EntityTypeBuilder<CatalogType> builder)
    {
        builder.ToTable("CatalogType");

        builder.HasKey(ct => ct.Id);

        builder.Property(ct => ct.Id)
            .IsRequired();

        builder.Property(ct => ct.Type)
            .IsRequired()
            .HasMaxLength(100);
    }
}
