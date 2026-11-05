using eShop.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Catalog.Data.Configurations;

/// <summary>
/// Port of <c>CatalogDBContext.ConfigureCatalogItem</c> (MVC / Web Forms).
///
/// Reconciled divergences against the WCF <c>EntityModel</c> copy of the model:
/// <list type="bullet">
///   <item>Price: WCF maps it as <c>money</c> with precision (19,4); the MVC/Web Forms context —
///   the one that actually creates the schema — leaves it on the EF6 default for
///   <c>decimal</c>, i.e. <c>decimal(18,2)</c>. The MVC shape wins and is made explicit here.</item>
///   <item>The WCF model has no <c>AvailableStock</c>, <c>RestockThreshold</c>,
///   <c>MaxStockThreshold</c> or <c>OnReorder</c> and spells the picture column
///   <c>Picturefilename</c>; the MVC columns are canonical and case-insensitively identical.</item>
/// </list>
/// </summary>
public class CatalogItemConfiguration : IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> builder)
    {
        builder.ToTable("Catalog");

        builder.HasKey(ci => ci.Id);

        // EF6: HasDatabaseGeneratedOption(DatabaseGeneratedOption.None) — ids come from the
        // application (HiLo generator), never from the database.
        builder.Property(ci => ci.Id)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(ci => ci.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ci => ci.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

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
