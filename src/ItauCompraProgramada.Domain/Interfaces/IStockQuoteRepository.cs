using ItauCompraProgramada.Domain.Entities;

namespace ItauCompraProgramada.Domain.Interfaces;

public interface IStockQuoteRepository
{
    Task<StockQuote?> GetLatestByTickerAsync(string ticker);
    Task<List<StockQuote>> GetLatestQuotesAsync(DateTime date);
    Task AddRangeAsync(IEnumerable<StockQuote> quotes);
    Task SaveChangesAsync();
}