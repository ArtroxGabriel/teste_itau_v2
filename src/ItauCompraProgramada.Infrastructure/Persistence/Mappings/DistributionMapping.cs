using ItauCompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItauCompraProgramada.Infrastructure.Persistence.Mappings;

public class DistributionMapping : IEntityTypeConfiguration<Distribution>
{
    public void Configure(EntityTypeBuilder<Distribution> builder)
    {
        builder.ToTable("Distribuicoes");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Ticker)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(d => d.Quantity)
            .IsRequired()
            .HasColumnName("Quantidade");

        builder.Property(d => d.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 4)
            .HasColumnName("PrecoUnitario");

        builder.Property(d => d.DistributedAt)
            .IsRequired()
            .HasColumnName("DataDistribuicao");

        builder.HasOne(d => d.PurchaseOrder)
            .WithMany()
            .HasForeignKey(d => d.PurchaseOrderId);

        builder.HasOne(d => d.Custody)
            .WithMany()
            .HasForeignKey(d => d.FilhoteCustodyId);
    }
}
