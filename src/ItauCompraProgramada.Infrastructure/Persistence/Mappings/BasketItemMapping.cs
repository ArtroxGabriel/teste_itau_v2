using ItauCompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItauCompraProgramada.Infrastructure.Persistence.Mappings;

public class BasketItemMapping : IEntityTypeConfiguration<BasketItem>
{
    public void Configure(EntityTypeBuilder<BasketItem> builder)
    {
        builder.ToTable("ItensCesta");

        builder.HasKey(bi => bi.Id);

        builder.Property(bi => bi.Ticker)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(bi => bi.Percentage)
            .IsRequired()
            .HasPrecision(5, 2)
            .HasColumnName("Percentual");

        builder.HasOne(bi => bi.Basket)
            .WithMany(rb => rb.Items)
            .HasForeignKey(bi => bi.BasketId);
    }
}
