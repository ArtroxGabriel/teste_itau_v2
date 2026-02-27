using ItauCompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItauCompraProgramada.Infrastructure.Persistence.Mappings;

public class GraphicAccountMapping : IEntityTypeConfiguration<GraphicAccount>
{
    public void Configure(EntityTypeBuilder<GraphicAccount> builder)
    {
        builder.ToTable("ContasGraficas");

        builder.HasKey(ga => ga.Id);

        builder.Property(ga => ga.AccountNumber)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("NumeroConta");

        builder.HasIndex(ga => ga.AccountNumber).IsUnique();

        builder.Property(ga => ga.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnName("Tipo");

        builder.Property(ga => ga.CreatedAt)
            .IsRequired()
            .HasColumnName("DataCriacao");
    }
}
