using System;
using System.IO;
using System.Linq;
using System.Text;
using ItauCompraProgramada.Infrastructure.ExternalServices;
using Xunit;

namespace ItauCompraProgramada.UnitTests.Infrastructure;

public class CotahistParserTests
{
    private const string SampleLine = "012026022602PETR4       010PETROBRAS   PN      N2   R$  000000000393000000000039800000000003897000000000394100000000039610000000003960000000000396144146000000000029919100000000117917752500000000000000009999123100000010000000000000BRPETRACNPR6223";

    [Fact]
    public void ParseFile_ShouldReturnCorrectStockQuote()
    {
        // Arrange
        var filePath = Path.GetTempFileName();
        var content = new StringBuilder();
        content.AppendLine("00COTAHIST.2026BOVESPA 20260226");
        content.AppendLine(SampleLine);
        content.AppendLine("99COTAHIST.2026BOVESPA 000000000000002");
        
        File.WriteAllText(filePath, content.ToString(), Encoding.GetEncoding("ISO-8859-1"));
        var parser = new CotahistParser();

        // Act
        var result = parser.ParseFile(filePath).ToList();

        // Assert
        Assert.Single(result);
        var quote = result.First();
        Assert.Equal("PETR4", quote.Ticker);
        Assert.Equal(new DateTime(2026, 02, 26), quote.TradingDate);
        Assert.Equal(39.30m, quote.OpeningPrice);
        Assert.Equal(39.61m, quote.ClosingPrice);
        Assert.Equal(39.80m, quote.MaxPrice);
        Assert.Equal(38.97m, quote.MinPrice);

        // Cleanup
        if (File.Exists(filePath)) File.Delete(filePath);
    }
}
