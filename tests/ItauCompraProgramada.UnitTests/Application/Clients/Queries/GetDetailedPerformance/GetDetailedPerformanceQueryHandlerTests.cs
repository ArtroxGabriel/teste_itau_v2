using FluentAssertions;
using ItauCompraProgramada.Application.Clients.Queries.GetDetailedPerformance;
using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;
using Moq;
using Xunit;

namespace ItauCompraProgramada.UnitTests.Application.Clients.Queries.GetDetailedPerformance;

public class GetDetailedPerformanceQueryHandlerTests
{
    private readonly Mock<IClientRepository> _clientRepositoryMock;
    private readonly Mock<IDistributionRepository> _distributionRepositoryMock;
    private readonly Mock<IStockQuoteRepository> _stockQuoteRepositoryMock;
    private readonly GetDetailedPerformanceQueryHandler _handler;

    public GetDetailedPerformanceQueryHandlerTests()
    {
        _clientRepositoryMock = new Mock<IClientRepository>();
        _distributionRepositoryMock = new Mock<IDistributionRepository>();
        _stockQuoteRepositoryMock = new Mock<IStockQuoteRepository>();
        _handler = new GetDetailedPerformanceQueryHandler(
            _clientRepositoryMock.Object,
            _distributionRepositoryMock.Object,
            _stockQuoteRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnPerformanceDto()
    {
        // Arrange
        var clientId = 1L;
        var client = new Client("Joao da Silva", "12345678901", "joao@email.com", 3000m, 5);
        
        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(client);

        var date1 = new DateTime(2026, 1, 5);
        var date2 = new DateTime(2026, 1, 15);

        // Distributions (Date 1)
        var dist1 = new Distribution(1, 1, "PETR4", 10, 30m);
        typeof(Distribution).GetProperty("DistributedAt")?.SetValue(dist1, date1);
        
        // Distributions (Date 2)
        var dist2 = new Distribution(2, 1, "PETR4", 10, 35m);
        typeof(Distribution).GetProperty("DistributedAt")?.SetValue(dist2, date2);

        _distributionRepositoryMock.Setup(r => r.GetByClientIdAsync(clientId))
            .ReturnsAsync(new List<Distribution> { dist1, dist2 });

        // Quotes for Date 1
        _stockQuoteRepositoryMock.Setup(r => r.GetLatestQuotesAsync(date1))
            .ReturnsAsync(new List<StockQuote> { new StockQuote(date1, "PETR4", 30m, 29m, 31m, 28m) });

        // Quotes for Date 2
        _stockQuoteRepositoryMock.Setup(r => r.GetLatestQuotesAsync(date2))
            .ReturnsAsync(new List<StockQuote> { new StockQuote(date2, "PETR4", 35m, 36m, 37m, 34m) });

        // Act
        var result = await _handler.Handle(new GetDetailedPerformanceQuery(clientId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AporteHistory.Should().HaveCount(2);
        result.WalletEvolution.Should().HaveCount(2);

        // Date 1 Evolution: Invested 300, Value 290
        result.WalletEvolution[0].Date.Should().Be(date1);
        result.WalletEvolution[0].InvestedValue.Should().Be(300m);
        result.WalletEvolution[0].WalletValue.Should().Be(290m);
        result.WalletEvolution[0].Rentability.Should().BeApproximately(-3.33m, 0.01m);

        // Date 2 Evolution: Invested 300+350=650, Value 20*36=720
        result.WalletEvolution[1].Date.Should().Be(date2);
        result.WalletEvolution[1].InvestedValue.Should().Be(650m);
        result.WalletEvolution[1].WalletValue.Should().Be(720m);
        result.WalletEvolution[1].Rentability.Should().BeApproximately(10.76m, 0.01m);
    }
}
