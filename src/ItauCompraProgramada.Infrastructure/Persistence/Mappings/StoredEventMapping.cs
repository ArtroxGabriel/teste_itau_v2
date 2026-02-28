using ItauCompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItauCompraProgramada.Infrastructure.Persistence.Mappings;

public class StoredEventMapping : IEntityTypeConfiguration<StoredEvent>
{
    public void Configure(EntityTypeBuilder<StoredEvent> builder)
    {
        builder.ToTable("stored_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Payload)
            .IsRequired()
            .HasColumnType("longtext");

        builder.Property(e => e.ResponsePayload)
            .HasColumnType("longtext");

        builder.Property(e => e.CorrelationId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(e => e.CorrelationId);

        builder.Property(e => e.Status)
            .HasMaxLength(50);
            
        builder.Property(e => e.Timestamp)
            .IsRequired();
    }
}
