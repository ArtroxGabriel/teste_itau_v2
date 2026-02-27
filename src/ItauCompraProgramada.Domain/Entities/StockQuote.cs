using System;

namespace ItauCompraProgramada.Domain.Entities;

public class StockQuote
{
    public long Id { get; private set; }
    public DateTime TradingDate { get; private set; }
    public string Ticker { get; private set; } = null!;
    public decimal OpeningPrice { get; private set; }
    public decimal ClosingPrice { get; private set; }
    public decimal MaxPrice { get; private set; }
    public decimal MinPrice { get; private set; }

    protected StockQuote() { } // EF Constructor

    public StockQuote(DateTime tradingDate, string ticker, decimal openingPrice, decimal closingPrice, decimal maxPrice, decimal minPrice)
    {
        TradingDate = tradingDate;
        Ticker = ticker;
        OpeningPrice = openingPrice;
        ClosingPrice = closingPrice;
        MaxPrice = maxPrice;
        MinPrice = minPrice;
    }
}
