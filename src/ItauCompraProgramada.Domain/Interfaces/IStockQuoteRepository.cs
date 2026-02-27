using ItauCompraProgramada.Domain.Entities;

namespace ItauCompraProgramada.Domain.Interfaces;

public interface IStockQuoteRepository
{
    Task<StockQuote?> GetLatestByTickerAsync(string ticker);
    Task AddRangeAsync(IEnumerable<StockQuote> quotes);
    Task SaveChangesAsync();
}
