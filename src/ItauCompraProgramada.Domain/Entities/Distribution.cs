using System;

namespace ItauCompraProgramada.Domain.Entities;

public class Distribution
{
    public long Id { get; private set; }
    public long PurchaseOrderId { get; private set; }
    public long FilhoteCustodyId { get; private set; }
    public string Ticker { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public DateTime DistributedAt { get; private set; }

    // Navigation
    public virtual PurchaseOrder? PurchaseOrder { get; private set; }
    public virtual Custody? Custody { get; private set; }

    protected Distribution() { } // EF Constructor

    public Distribution(long purchaseOrderId, long filhoteCustodyId, string ticker, int quantity, decimal unitPrice)
    {
        PurchaseOrderId = purchaseOrderId;
        FilhoteCustodyId = filhoteCustodyId;
        Ticker = ticker;
        Quantity = quantity;
        UnitPrice = unitPrice;
        DistributedAt = DateTime.UtcNow;
    }
}
