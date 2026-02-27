using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ItauCompraProgramada.Application.Interfaces;
using ItauCompraProgramada.Application.Services;
using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;
using Moq;
using Xunit;

namespace ItauCompraProgramada.UnitTests.Application;

public class QuoteServiceTests
{
    private readonly Mock<ICotahistParser> _parserMock;
    private readonly Mock<IStockQuoteRepository> _repositoryMock;
    private readonly QuoteService _service;

    public QuoteServiceTests()
    {
        _parserMock = new Mock<ICotahistParser>();
        _repositoryMock = new Mock<IStockQuoteRepository>();
        _service = new QuoteService(_parserMock.Object, _repositoryMock.Object);
    }

    [Fact]
    public async Task SyncQuotesFromFileAsync_ShouldCallParserAndRepository()
    {
        // Arrange
        var filePath = "test.txt";
        var quotes = new List<StockQuote>
        {
            new StockQuote(DateTime.UtcNow, "PETR4", 30m, 31m, 32m, 29m)
        };

        _parserMock.Setup(p => p.ParseFile(filePath)).Returns(quotes);

        // Act
        await _service.SyncQuotesFromFileAsync(filePath);

        // Assert
        _parserMock.Verify(p => p.ParseFile(filePath), Times.Once);
        _repositoryMock.Verify(r => r.AddRangeAsync(quotes), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetLatestQuoteAsync_ShouldCallRepository()
    {
        // Arrange
        var ticker = "PETR4";
        var quote = new StockQuote(DateTime.UtcNow, ticker, 30m, 31m, 32m, 29m);
        _repositoryMock.Setup(r => r.GetLatestByTickerAsync(ticker)).ReturnsAsync(quote);

        // Act
        var result = await _service.GetLatestQuoteAsync(ticker);

        // Assert
        Assert.Equal(quote, result);
        _repositoryMock.Verify(r => r.GetLatestByTickerAsync(ticker), Times.Once);
    }
}
