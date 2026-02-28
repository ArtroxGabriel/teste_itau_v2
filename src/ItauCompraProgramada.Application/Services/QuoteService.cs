using ItauCompraProgramada.Application.Interfaces;
using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;

namespace ItauCompraProgramada.Application.Services;

public class QuoteService(ICotahistParser parser, IStockQuoteRepository repository) : IQuoteService
{
    private readonly ICotahistParser _parser = parser;
    private readonly IStockQuoteRepository _repository = repository;

    public async Task<StockQuote?> GetLatestQuoteAsync(string ticker)
    {
        return await _repository.GetLatestByTickerAsync(ticker);
    }

    public async Task SyncQuotesFromFileAsync(string filePath)
    {
        var quotes = _parser.ParseFile(filePath);
        if (quotes.Any())
        {
            await _repository.AddRangeAsync(quotes);
            await _repository.SaveChangesAsync();
        }
    }
}