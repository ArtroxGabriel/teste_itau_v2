using FluentAssertions;
using ItauCompraProgramada.Application.Clients.Queries.GetClientWallet;
using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;
using Moq;
using Xunit;

namespace ItauCompraProgramada.UnitTests.Application.Clients.Queries.GetClientWallet;

public class GetClientWalletQueryHandlerTests
{
    private readonly Mock<IClientRepository> _clientRepositoryMock;
    private readonly Mock<ICustodyRepository> _custodyRepositoryMock;
    private readonly Mock<IStockQuoteRepository> _stockQuoteRepositoryMock;
    private readonly GetClientWalletQueryHandler _handler;

    public GetClientWalletQueryHandlerTests()
    {
        _clientRepositoryMock = new Mock<IClientRepository>();
        _custodyRepositoryMock = new Mock<ICustodyRepository>();
        _stockQuoteRepositoryMock = new Mock<IStockQuoteRepository>();
        _handler = new GetClientWalletQueryHandler(
            _clientRepositoryMock.Object,
            _custodyRepositoryMock.Object,
            _stockQuoteRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnWalletDto()
    {
        // Arrange
        var clientId = 1L;
        var client = new Client("Joao da Silva", "12345678901", "joao@email.com", 3000m, 5);
        typeof(Client).GetProperty("Id")?.SetValue(client, clientId);
        
        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(client);

        var custodies = new List<Custody>
        {
            new Custody(client.GraphicAccount!.Id, "PETR4", 100, 30m),
            new Custody(client.GraphicAccount.Id, "VALE3", 50, 80m)
        };
        _custodyRepositoryMock.Setup(r => r.GetByAccountIdAsync(client.GraphicAccount.Id)).ReturnsAsync(custodies);

        _stockQuoteRepositoryMock.Setup(r => r.GetLatestByTickerAsync("PETR4"))
            .ReturnsAsync(new StockQuote(DateTime.UtcNow, "PETR4", 30m, 35m, 36m, 29m));
        _stockQuoteRepositoryMock.Setup(r => r.GetLatestByTickerAsync("VALE3"))
            .ReturnsAsync(new StockQuote(DateTime.UtcNow, "VALE3", 80m, 90m, 91m, 79m));

        // Act
        var result = await _handler.Handle(new GetClientWalletQuery(clientId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ClientId.Should().Be(clientId);
        result.Summary.TotalInvestedValue.Should().Be(100 * 30m + 50 * 80m); // 3000 + 4000 = 7000
        result.Summary.TotalCurrentValue.Should().Be(100 * 35m + 50 * 90m); // 3500 + 4500 = 8000
        result.Summary.TotalProfitLoss.Should().Be(1000m);
        result.Summary.ProfitLossPercentage.Should().BeApproximately(14.28m, 0.01m);
        result.Assets.Should().HaveCount(2);
        
        var petr4 = result.Assets.First(a => a.Ticker == "PETR4");
        petr4.PortfolioWeight.Should().Be(3500m / 8000m * 100m);
    }
}
