using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.EntityConfigurations;

public class DiscountItemConfiguration : IEntityTypeConfiguration<DiscountItem>
{
    public void Configure(EntityTypeBuilder<DiscountItem> builder)
    {
        builder.ToTable("DiscountItem");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedOnAdd();

        builder.Property(d => d.Size)
            .IsRequired();

        builder.Property(d => d.Start)
            .IsRequired();

        builder.Property(d => d.End)
            .IsRequired();
    }
}
