namespace ItauCompraProgramada.Domain.Entities;

public class BasketItem
{
    public long Id { get; private set; }
    public long BasketId { get; private set; }
    public string Ticker { get; private set; }
    public decimal Percentage { get; private set; }

    // Navigation
    public virtual RecommendationBasket? Basket { get; private set; }

    protected BasketItem() { } // EF Constructor

    public BasketItem(string ticker, decimal percentage)
    {
        Ticker = ticker;
        Percentage = percentage;
    }
}
