using ItauCompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItauCompraProgramada.Infrastructure.Persistence.Mappings;

public class RecommendationBasketMapping : IEntityTypeConfiguration<RecommendationBasket>
{
    public void Configure(EntityTypeBuilder<RecommendationBasket> builder)
    {
        builder.ToTable("CestasRecomendacao");

        builder.HasKey(rb => rb.Id);

        builder.Property(rb => rb.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("Nome");

        builder.Property(rb => rb.IsActive)
            .IsRequired()
            .HasColumnName("Ativa");

        builder.Property(rb => rb.CreatedAt)
            .IsRequired()
            .HasColumnName("DataCriacao");

        builder.Property(rb => rb.DeactivatedAt)
            .HasColumnName("DataDesativacao");
    }
}
