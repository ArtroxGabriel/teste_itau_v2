using ItauCompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItauCompraProgramada.Infrastructure.Persistence.Mappings;

public class StockQuoteMapping : IEntityTypeConfiguration<StockQuote>
{
    public void Configure(EntityTypeBuilder<StockQuote> builder)
    {
        builder.ToTable("Cotacoes");

        builder.HasKey(sq => sq.Id);

        builder.Property(sq => sq.TradingDate)
            .IsRequired()
            .HasColumnName("DataPregao");

        builder.Property(sq => sq.Ticker)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(sq => sq.OpeningPrice)
            .IsRequired()
            .HasPrecision(18, 4)
            .HasColumnName("PrecoAbertura");

        builder.Property(sq => sq.ClosingPrice)
            .IsRequired()
            .HasPrecision(18, 4)
            .HasColumnName("PrecoFechamento");

        builder.Property(sq => sq.MaxPrice)
            .IsRequired()
            .HasPrecision(18, 4)
            .HasColumnName("PrecoMaximo");

        builder.Property(sq => sq.MinPrice)
            .IsRequired()
            .HasPrecision(18, 4)
            .HasColumnName("PrecoMinimo");
    }
}
