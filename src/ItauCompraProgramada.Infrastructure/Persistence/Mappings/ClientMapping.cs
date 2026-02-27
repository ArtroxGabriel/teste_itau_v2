using ItauCompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItauCompraProgramada.Infrastructure.Persistence.Mappings;

public class ClientMapping : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("Nome");

        builder.Property(c => c.Cpf)
            .IsRequired()
            .HasMaxLength(11)
            .HasColumnName("CPF");

        builder.HasIndex(c => c.Cpf).IsUnique();

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.MonthlyContribution)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasColumnName("ValorMensal");

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasColumnName("Ativo");

        builder.Property(c => c.AdhesionDate)
            .IsRequired()
            .HasColumnName("DataAdesao");

        builder.HasOne(c => c.GraphicAccount)
            .WithOne(ga => ga.Client)
            .HasForeignKey<GraphicAccount>(ga => ga.ClienteId);
    }
}
