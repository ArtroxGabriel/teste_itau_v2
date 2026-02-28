using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace ItauCompraProgramada.Infrastructure.Persistence.Repositories;

public class StockQuoteRepository(ItauDbContext context) : IStockQuoteRepository
{
    private readonly ItauDbContext _context = context;

    public async Task<StockQuote?> GetLatestByTickerAsync(string ticker)
    {
        return await _context.StockQuotes
            .Where(q => q.Ticker == ticker)
            .OrderByDescending(q => q.TradingDate)
            .FirstOrDefaultAsync();
    }

    public async Task<List<StockQuote>> GetLatestQuotesAsync(DateTime date)
    {
        // Get the most recent date available in the database on or before the requested date
        var latestDate = await _context.StockQuotes
            .Where(q => q.TradingDate.Date <= date.Date)
            .MaxAsync(q => (DateTime?)q.TradingDate);

        if (latestDate == null) return new List<StockQuote>();

        return await _context.StockQuotes
            .Where(q => q.TradingDate.Date == latestDate.Value.Date)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<StockQuote> quotes)
    {
        await _context.StockQuotes.AddRangeAsync(quotes);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}