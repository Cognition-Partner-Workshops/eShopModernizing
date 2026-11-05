using eShop.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Catalog.Data.Configurations;

/// <summary>
/// Port of <c>CatalogDBContext.ConfigureCatalogBrand</c> (MVC / Web Forms).
///
/// Reconciled divergences against the WCF <c>EntityModel</c>: the WCF copy declares
/// <c>Brand</c> as non-unicode <c>varchar(50)</c> and the key as
/// <c>DatabaseGeneratedOption.None</c>, and relies on EF6 pluralization for the table name
/// (<c>CatalogBrands</c>). The MVC/Web Forms context creates the database, so its shape wins:
/// table <c>CatalogBrand</c>, identity key, <c>nvarchar(100)</c>.
/// </summary>
public class CatalogBrandConfiguration : IEntityTypeConfiguration<CatalogBrand>
{
    public void Configure(EntityTypeBuilder<CatalogBrand> builder)
    {
        builder.ToTable("CatalogBrand");

        builder.HasKey(cb => cb.Id);

        builder.Property(cb => cb.Id)
            .IsRequired();

        builder.Property(cb => cb.Brand)
            .IsRequired()
            .HasMaxLength(100);
    }
}
