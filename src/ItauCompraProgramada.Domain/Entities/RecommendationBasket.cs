using System;
using System.Collections.Generic;

namespace ItauCompraProgramada.Domain.Entities;

public class RecommendationBasket
{
    public long Id { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }

    // Navigation
    public virtual ICollection<BasketItem> Items { get; private set; } = new List<BasketItem>();

    protected RecommendationBasket() { } // EF Constructor

    public RecommendationBasket(string name, List<BasketItem> items)
    {
        if (items.Count != 5)
            throw new ArgumentException("The basket must contain exactly 5 stocks.");

        if (items.Sum(i => i.Percentage) != 100m)
            throw new ArgumentException("The total percentage of the basket must be exactly 100%.");

        Name = name;
        Items = items;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        DeactivatedAt = DateTime.UtcNow;
    }
}
