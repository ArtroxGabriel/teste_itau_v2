using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ItauCompraProgramada.Application.Interfaces;
using ItauCompraProgramada.Domain.Entities;

namespace ItauCompraProgramada.Infrastructure.ExternalServices;

public class CotahistParser : ICotahistParser
{
    static CotahistParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public IEnumerable<StockQuote> ParseFile(string filePath)
    {
        var encoding = Encoding.GetEncoding("ISO-8859-1");
        var quotes = new List<StockQuote>();

        foreach (var line in File.ReadLines(filePath, encoding))
        {
            if (line.Length < 245 || !line.StartsWith("01"))
                continue;

            var ticker = line.Substring(12, 12).Trim();
            var tradingDate = DateTime.ParseExact(line.Substring(2, 8), "yyyyMMdd", CultureInfo.InvariantCulture);
            
            var openingPrice = ParsePrice(line.Substring(56, 13));
            var maxPrice = ParsePrice(line.Substring(69, 13));
            var minPrice = ParsePrice(line.Substring(82, 13));
            var closingPrice = ParsePrice(line.Substring(108, 13));

            quotes.Add(new StockQuote(tradingDate, ticker, openingPrice, closingPrice, maxPrice, minPrice));
        }

        return quotes;
    }

    public StockQuote? GetLatestClosingPrice(string cotacoesPath, string ticker)
    {
        var files = Directory.GetFiles(cotacoesPath, "COTAHIST_D*.TXT")
            .OrderByDescending(f => f);

        foreach (var file in files)
        {
            var quote = ParseFile(file)
                .FirstOrDefault(q => q.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase));

            if (quote != null)
                return quote;
        }

        return null;
    }

    private static decimal ParsePrice(string value)
    {
        if (long.TryParse(value.Trim(), out var result))
            return result / 100m;
        return 0m;
    }
}
