using ItauCompraProgramada.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItauCompraProgramada.Infrastructure.Persistence.Mappings;

public class CustodyMapping : IEntityTypeConfiguration<Custody>
{
    public void Configure(EntityTypeBuilder<Custody> builder)
    {
        builder.ToTable("Custodias");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Ticker)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(c => c.Quantity)
            .IsRequired()
            .HasColumnName("Quantidade");

        builder.Property(c => c.AveragePrice)
            .IsRequired()
            .HasPrecision(18, 4)
            .HasColumnName("PrecoMedio");

        builder.Property(c => c.LastUpdatedAt)
            .IsRequired()
            .HasColumnName("DataUltimaAtualizacao");

        builder.HasOne(c => c.Account)
            .WithMany(ga => ga.Custodies)
            .HasForeignKey(c => c.AccountId);
    }
}