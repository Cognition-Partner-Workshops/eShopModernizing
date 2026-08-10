using eShop.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Catalog.Data.Configurations;

/// <summary>
/// WCF-only entity, ported from the data annotations on <c>eShopWCFService.Models.DiscountItem</c>.
/// The legacy class has no <c>[Table]</c> attribute, so EF6 pluralization named the table
/// <c>DiscountItems</c>; that name is kept. <c>Id</c> stays store-generated (the legacy model does
/// not opt out of identity) and <c>Start</c>/<c>End</c> stay SQL <c>date</c> columns.
/// </summary>
public class DiscountItemConfiguration : IEntityTypeConfiguration<DiscountItem>
{
    public void Configure(EntityTypeBuilder<DiscountItem> builder)
    {
        builder.ToTable("DiscountItems");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Size)
            .IsRequired();

        builder.Property(d => d.Start)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(d => d.End)
            .HasColumnType("date")
            .IsRequired();
    }
}
