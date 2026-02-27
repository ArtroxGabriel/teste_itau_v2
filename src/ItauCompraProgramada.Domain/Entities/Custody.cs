using System;

namespace ItauCompraProgramada.Domain.Entities;

public class Custody
{
    public long Id { get; private set; }
    public long AccountId { get; private set; }
    public string Ticker { get; private set; } = null!;
    public int Quantity { get; private set; }
    public decimal AveragePrice { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    // Navigation
    public virtual GraphicAccount? Account { get; private set; }

    protected Custody() { } // EF Constructor

    public Custody(long accountId, string ticker, int quantity, decimal averagePrice)
    {
        AccountId = accountId;
        Ticker = ticker;
        Quantity = quantity;
        AveragePrice = averagePrice;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAveragePrice(int newQuantity, decimal unitPrice)
    {
        if (newQuantity <= 0) return;
        
        AveragePrice = (Quantity * AveragePrice + newQuantity * unitPrice) / (Quantity + newQuantity);
        Quantity += newQuantity;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void SubtractQuantity(int quantity)
    {
        if (quantity > Quantity)
            throw new InvalidOperationException("Insufficient quantity in custody.");
            
        Quantity -= quantity;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
