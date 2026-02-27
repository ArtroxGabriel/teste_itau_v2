using System;
using ItauCompraProgramada.Domain.Enums;

namespace ItauCompraProgramada.Domain.Entities;

public class Rebalancing
{
    public long Id { get; private set; }
    public long ClienteId { get; private set; }
    public RebalancingType Type { get; private set; }
    public string? TickerSold { get; private set; }
    public string? TickerBought { get; private set; }
    public decimal SaleValue { get; private set; }
    public DateTime RebalanceDate { get; private set; }

    // Navigation
    public virtual Client? Client { get; private set; }

    protected Rebalancing() { } // EF Constructor

    public Rebalancing(long clienteId, RebalancingType type, string? tickerSold, string? tickerBought, decimal saleValue)
    {
        ClienteId = clienteId;
        Type = type;
        TickerSold = tickerSold;
        TickerBought = tickerBought;
        SaleValue = saleValue;
        RebalanceDate = DateTime.UtcNow;
    }
}
