using ItauCompraProgramada.Domain.Entities;

namespace ItauCompraProgramada.Application.Interfaces;

public interface ICotahistParser
{
    IEnumerable<StockQuote> ParseFile(string filePath);
    StockQuote? GetLatestClosingPrice(string cotacoesPath, string ticker);
}
