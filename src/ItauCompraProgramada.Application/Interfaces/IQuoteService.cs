using ItauCompraProgramada.Domain.Entities;

namespace ItauCompraProgramada.Application.Interfaces;

public interface IQuoteService
{
    Task<StockQuote?> GetLatestQuoteAsync(string ticker);
    Task SyncQuotesFromFileAsync(string filePath);
}