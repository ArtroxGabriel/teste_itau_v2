using ItauCompraProgramada.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItauCompraProgramada.Infrastructure.Persistence.Mappings;

public class PurchaseOrderMapping : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("OrdensCompra");

        builder.HasKey(po => po.Id);

        builder.Property(po => po.Ticker)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(po => po.Quantity)
            .IsRequired()
            .HasColumnName("Quantidade");

        builder.Property(po => po.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 4)
            .HasColumnName("PrecoUnitario");

        builder.Property(po => po.MarketType)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnName("TipoMercado");

        builder.Property(po => po.ExecutionDate)
            .IsRequired()
            .HasColumnName("DataExecucao");

        builder.HasOne(po => po.MasterAccount)
            .WithMany()
            .HasForeignKey(po => po.MasterAccountId);
    }
}