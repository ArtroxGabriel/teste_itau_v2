using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using ItauCompraProgramada.Application.Admin.Queries.GetBasketHistory;
using ItauCompraProgramada.Application.Admin.Queries.GetCurrentBasket;
using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;

using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace ItauCompraProgramada.UnitTests.Application.Admin.Queries;

public class GetCurrentBasketQueryHandlerTests
{
    private readonly Mock<IRecommendationBasketRepository> _basketRepositoryMock;
    private readonly Mock<IStockQuoteRepository> _stockQuoteRepositoryMock;
    private readonly Mock<ILogger<GetCurrentBasketQueryHandler>> _loggerMock;
    private readonly GetCurrentBasketQueryHandler _handler;

    public GetCurrentBasketQueryHandlerTests()
    {
        _basketRepositoryMock = new Mock<IRecommendationBasketRepository>();
        _stockQuoteRepositoryMock = new Mock<IStockQuoteRepository>();
        _loggerMock = new Mock<ILogger<GetCurrentBasketQueryHandler>>();

        _handler = new GetCurrentBasketQueryHandler(
            _basketRepositoryMock.Object,
            _stockQuoteRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ActiveBasketExists_ShouldReturnBasketWithQuotes()
    {
        // Arrange
        var items = new List<BasketItem>
        {
            new BasketItem("PETR4", 30m),
            new BasketItem("VALE3", 25m),
            new BasketItem("ITUB4", 20m),
            new BasketItem("BBDC4", 15m),
            new BasketItem("WEGE3", 10m),
        };
        var basket = new RecommendationBasket("Top Five - Test", items);

        _basketRepositoryMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        _stockQuoteRepositoryMock
            .Setup(r => r.GetLatestByTickerAsync("PETR4"))
            .ReturnsAsync(new StockQuote(System.DateTime.UtcNow, "PETR4", 35m, 37m, 38m, 34m));
        _stockQuoteRepositoryMock
            .Setup(r => r.GetLatestByTickerAsync(It.IsNotIn("PETR4")))
            .ReturnsAsync((Domain.Entities.StockQuote?)null);

        // Act
        var result = await _handler.Handle(new GetCurrentBasketQuery(), CancellationToken.None);

        // Assert
        result.Ativa.Should().BeTrue();
        result.Nome.Should().Be("Top Five - Test");
        result.Itens.Should().HaveCount(5);

        var petr4 = result.Itens.First(i => i.Ticker == "PETR4");
        petr4.CotacaoAtual.Should().Be(37m);
        petr4.Percentual.Should().Be(30m);

        var vale3 = result.Itens.First(i => i.Ticker == "VALE3");
        vale3.CotacaoAtual.Should().BeNull(); // No quote mocked
    }

    [Fact]
    public async Task Handle_NoActiveBasket_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _basketRepositoryMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecommendationBasket?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new GetCurrentBasketQuery(), CancellationToken.None));
    }
}

public class GetBasketHistoryQueryHandlerTests
{
    private readonly Mock<IRecommendationBasketRepository> _basketRepositoryMock;
    private readonly Mock<ILogger<GetBasketHistoryQueryHandler>> _loggerMock;
    private readonly GetBasketHistoryQueryHandler _handler;

    public GetBasketHistoryQueryHandlerTests()
    {
        _basketRepositoryMock = new Mock<IRecommendationBasketRepository>();
        _loggerMock = new Mock<ILogger<GetBasketHistoryQueryHandler>>();

        _handler = new GetBasketHistoryQueryHandler(
            _basketRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_TwoBasketsExist_ShouldReturnBothOrderedNewestFirst()
    {
        // Arrange
        var items1 = new List<BasketItem>
        {
            new BasketItem("PETR4", 20m), new BasketItem("VALE3", 20m),
            new BasketItem("ITUB4", 20m), new BasketItem("BBDC4", 20m),
            new BasketItem("ABEV3", 20m),
        };
        var items2 = new List<BasketItem>
        {
            new BasketItem("PETR4", 25m), new BasketItem("VALE3", 25m),
            new BasketItem("ITUB4", 25m), new BasketItem("WEGE3", 15m),
            new BasketItem("RENT3", 10m),
        };
        var basket1 = new RecommendationBasket("Top Five - Old", items1);
        basket1.Deactivate();

        var basket2 = new RecommendationBasket("Top Five - Current", items2);

        // Returned newest-first (repository does OrderByDescending in implementation)
        _basketRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecommendationBasket> { basket2, basket1 });

        // Act
        var result = await _handler.Handle(new GetBasketHistoryQuery(), CancellationToken.None);

        // Assert
        result.Cestas.Should().HaveCount(2);
        result.Cestas[0].Nome.Should().Be("Top Five - Current");
        result.Cestas[0].Ativa.Should().BeTrue();
        result.Cestas[0].DataDesativacao.Should().BeNull();

        result.Cestas[1].Nome.Should().Be("Top Five - Old");
        result.Cestas[1].Ativa.Should().BeFalse();
        result.Cestas[1].DataDesativacao.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NoBasketsExist_ShouldReturnEmptyList()
    {
        // Arrange
        _basketRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecommendationBasket>());

        // Act
        var result = await _handler.Handle(new GetBasketHistoryQuery(), CancellationToken.None);

        // Assert
        result.Cestas.Should().BeEmpty();
    }
}
