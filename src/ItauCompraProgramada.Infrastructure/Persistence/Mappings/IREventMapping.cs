using ItauCompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItauCompraProgramada.Infrastructure.Persistence.Mappings;

public class IREventMapping : IEntityTypeConfiguration<IREvent>
{
    public void Configure(EntityTypeBuilder<IREvent> builder)
    {
        builder.ToTable("EventosIR");

        builder.HasKey(ir => ir.Id);

        builder.Property(ir => ir.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnName("Tipo");

        builder.Property(ir => ir.BaseValue)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasColumnName("ValorBase");

        builder.Property(ir => ir.IRValue)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasColumnName("ValorIR");

        builder.Property(ir => ir.PublishedToKafka)
            .IsRequired()
            .HasColumnName("PublicadoKafka");

        builder.Property(ir => ir.EventDate)
            .IsRequired()
            .HasColumnName("DataEvento");

        builder.HasOne(ir => ir.Client)
            .WithMany()
            .HasForeignKey(ir => ir.ClienteId);
    }
}
