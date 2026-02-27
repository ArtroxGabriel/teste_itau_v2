using System;
using ItauCompraProgramada.Domain.Enums;

namespace ItauCompraProgramada.Domain.Entities;

public class PurchaseOrder
{
    public long Id { get; private set; }
    public long MasterAccountId { get; private set; }
    public string Ticker { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public MarketType MarketType { get; private set; }
    public DateTime ExecutionDate { get; private set; }

    // Navigation
    public virtual GraphicAccount? MasterAccount { get; private set; }

    protected PurchaseOrder() { } // EF Constructor

    public PurchaseOrder(long masterAccountId, string ticker, int quantity, decimal unitPrice, MarketType marketType)
    {
        MasterAccountId = masterAccountId;
        Ticker = ticker;
        Quantity = quantity;
        UnitPrice = unitPrice;
        MarketType = marketType;
        ExecutionDate = DateTime.UtcNow;
    }
}
