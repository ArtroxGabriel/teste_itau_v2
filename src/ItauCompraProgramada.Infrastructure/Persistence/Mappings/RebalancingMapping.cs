using ItauCompraProgramada.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItauCompraProgramada.Infrastructure.Persistence.Mappings;

public class RebalancingMapping : IEntityTypeConfiguration<Rebalancing>
{
    public void Configure(EntityTypeBuilder<Rebalancing> builder)
    {
        builder.ToTable("Rebalanceamentos");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnName("Tipo");

        builder.Property(r => r.TickerSold)
            .HasMaxLength(10);

        builder.Property(r => r.TickerBought)
            .HasMaxLength(10);

        builder.Property(r => r.SaleValue)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasColumnName("ValorVenda");

        builder.Property(r => r.RebalanceDate)
            .IsRequired()
            .HasColumnName("DataRebalanceamento");

        builder.HasOne(r => r.Client)
            .WithMany()
            .HasForeignKey(r => r.ClienteId);
    }
}