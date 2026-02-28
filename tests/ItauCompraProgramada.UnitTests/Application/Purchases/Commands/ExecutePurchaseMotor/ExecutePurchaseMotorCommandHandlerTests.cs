using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using ItauCompraProgramada.Application.Interfaces;
using ItauCompraProgramada.Application.Purchases.Commands.ExecutePurchaseMotor;
using ItauCompraProgramada.Application.Taxes.Services;
using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;
using ItauCompraProgramada.Domain.Repositories;

using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace ItauCompraProgramada.UnitTests.Application.Purchases.Commands.ExecutePurchaseMotor;

public class ExecutePurchaseMotorCommandHandlerTests
{
    private readonly Mock<IClientRepository> _clientRepositoryMock;
    private readonly Mock<IStockQuoteRepository> _stockQuoteRepositoryMock;
    private readonly Mock<IPurchaseOrderRepository> _purchaseOrderRepositoryMock;
    private readonly Mock<ICustodyRepository> _custodyRepositoryMock;
    private readonly Mock<IRecommendationBasketRepository> _recommendationBasketRepositoryMock;
    private readonly Mock<IIREventRepository> _irEventRepositoryMock;
    private readonly TaxService _taxService;
    private readonly Mock<IKafkaProducer> _kafkaProducerMock;
    private readonly Mock<ILogger<ExecutePurchaseMotorCommandHandler>> _loggerMock;
    private readonly ExecutePurchaseMotorCommandHandler _handler;

    public ExecutePurchaseMotorCommandHandlerTests()
    {
        _clientRepositoryMock = new Mock<IClientRepository>();
        _stockQuoteRepositoryMock = new Mock<IStockQuoteRepository>();
        _purchaseOrderRepositoryMock = new Mock<IPurchaseOrderRepository>();
        _custodyRepositoryMock = new Mock<ICustodyRepository>();
        _recommendationBasketRepositoryMock = new Mock<IRecommendationBasketRepository>();
        _irEventRepositoryMock = new Mock<IIREventRepository>();
        _taxService = new TaxService();
        _kafkaProducerMock = new Mock<IKafkaProducer>();
        _loggerMock = new Mock<ILogger<ExecutePurchaseMotorCommandHandler>>();

        _handler = new ExecutePurchaseMotorCommandHandler(
            _clientRepositoryMock.Object,
            _stockQuoteRepositoryMock.Object,
            _purchaseOrderRepositoryMock.Object,
            _custodyRepositoryMock.Object,
            _recommendationBasketRepositoryMock.Object,
            _irEventRepositoryMock.Object,
            _taxService,
            _kafkaProducerMock.Object,
            _loggerMock.Object);
    }

    private static RecommendationBasket BuildTestBasket()
    {
        return new RecommendationBasket("Top Five - Test", new List<BasketItem>
        {
            new BasketItem("PETR4", 20m),
            new BasketItem("VALE3", 20m),
            new BasketItem("ITUB4", 20m),
            new BasketItem("BBDC4", 20m),
            new BasketItem("ABEV3", 20m),
        });
    }

    [Fact]
    public async Task Handle_ValidExecution_ShouldExecutePurchasesForAllEligibleClients()
    {
        // Arrange
        var executionDate = new DateTime(2026, 3, 5); // Day 5
        var command = new ExecutePurchaseMotorCommand(executionDate, "correlation-456");

        // Mock stored basket (US02: admin-managed basket)
        var basket = BuildTestBasket();
        _recommendationBasketRepositoryMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        // Mock stock quotes
        var quotes = new List<StockQuote>
        {
            new StockQuote(executionDate.AddDays(-1), "PETR4", 30m, 35m, 36m, 29m),
            new StockQuote(executionDate.AddDays(-1), "VALE3", 80m, 90m, 91m, 79m),
            new StockQuote(executionDate.AddDays(-1), "ITUB4", 25m, 28m, 29m, 24m),
            new StockQuote(executionDate.AddDays(-1), "BBDC4", 15m, 16.5m, 17m, 14m),
            new StockQuote(executionDate.AddDays(-1), "ABEV3", 12m, 13m, 13.5m, 11m)
        };
        _stockQuoteRepositoryMock.Setup(r => r.GetLatestQuotesAsync(It.IsAny<DateTime>())).ReturnsAsync(quotes);

        // Mock Client (Aporte 3000 -> 1000 per day -> 200 per stock with equal 20% allocation)
        var client = new Client("Client 1", "123", "client1@itau.com.br", 3000m, 5);
        _clientRepositoryMock.Setup(r => r.GetClientsForExecutionAsync(executionDate.Day)).ReturnsAsync(new List<Client> { client });
        _custodyRepositoryMock.Setup(r => r.GetByAccountIdAsync(1)).ReturnsAsync(new List<Custody>()); // Master Account
        _custodyRepositoryMock.Setup(r => r.GetByAccountIdAsync(client.GraphicAccount!.Id)).ReturnsAsync(new List<Custody>()); // Client Account

        _irEventRepositoryMock.Setup(r => r.GetPendingPublicationAsync()).ReturnsAsync(new List<IREvent>());
        _purchaseOrderRepositoryMock.Setup(r => r.GetTotalSalesValueInMonthAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(0m);
        _irEventRepositoryMock.Setup(r => r.GetTotalBaseValueInMonthAsync(It.IsAny<long>(), It.IsAny<ItauCompraProgramada.Domain.Enums.IREventType>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(0m);
        _irEventRepositoryMock.Setup(r => r.GetTotalIRValueInMonthAsync(It.IsAny<long>(), It.IsAny<ItauCompraProgramada.Domain.Enums.IREventType>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(0m);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert: at least one buy order for each of the 5 basket stocks (master account)
        _purchaseOrderRepositoryMock.Verify(r => r.AddAsync(It.Is<PurchaseOrder>(o => o.MasterAccountId == 1)), Times.Exactly(5));
        _purchaseOrderRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_WithExistingTickerOutOfBasket_ShouldSellOldTicker()
    {
        // Arrange
        var executionDate = new DateTime(2026, 3, 5);
        var command = new ExecutePurchaseMotorCommand(executionDate, "correlation-789");

        // Basket does NOT contain WEGE3
        var basket = BuildTestBasket();
        _recommendationBasketRepositoryMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        var quotes = new List<StockQuote>
        {
            new StockQuote(executionDate.AddDays(-1), "PETR4", 30m, 35m, 36m, 29m),
            new StockQuote(executionDate.AddDays(-1), "VALE3", 80m, 90m, 91m, 79m),
            new StockQuote(executionDate.AddDays(-1), "ITUB4", 25m, 28m, 29m, 24m),
            new StockQuote(executionDate.AddDays(-1), "BBDC4", 15m, 16.5m, 17m, 14m),
            new StockQuote(executionDate.AddDays(-1), "ABEV3", 12m, 13m, 13.5m, 11m)
        };
        _stockQuoteRepositoryMock.Setup(r => r.GetLatestQuotesAsync(It.IsAny<DateTime>())).ReturnsAsync(quotes);

        // Client with existing custody of "WEGE3" (not in basket)
        var client = new Client("Client 1", "123", "client1@itau.com.br", 3000m, 5);
        _clientRepositoryMock.Setup(r => r.GetClientsForExecutionAsync(executionDate.Day)).ReturnsAsync(new List<Client> { client });

        var existingCustody = new Custody(client.GraphicAccount!.Id, "WEGE3", 10, 50m);
        _custodyRepositoryMock.Setup(r => r.GetByAccountIdAsync(1)).ReturnsAsync(new List<Custody>()); // Master Account
        _custodyRepositoryMock.Setup(r => r.GetByAccountIdAsync(client.GraphicAccount.Id)).ReturnsAsync(new List<Custody> { existingCustody });

        // Need quote for WEGE3 to sell it
        var wegeQuote = new StockQuote(executionDate.AddDays(-1), "WEGE3", 50m, 55m, 56m, 49m);
        _stockQuoteRepositoryMock.Setup(r => r.GetLatestByTickerAsync("WEGE3")).ReturnsAsync(wegeQuote);

        _irEventRepositoryMock.Setup(r => r.GetPendingPublicationAsync()).ReturnsAsync(new List<IREvent>());
        _purchaseOrderRepositoryMock.Setup(r => r.GetTotalSalesValueInMonthAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(0m);
        _irEventRepositoryMock.Setup(r => r.GetTotalBaseValueInMonthAsync(It.IsAny<long>(), It.IsAny<ItauCompraProgramada.Domain.Enums.IREventType>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(0m);
        _irEventRepositoryMock.Setup(r => r.GetTotalIRValueInMonthAsync(It.IsAny<long>(), It.IsAny<ItauCompraProgramada.Domain.Enums.IREventType>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(0m);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert: WEGE3 sold + 5 buy orders on master account
        _purchaseOrderRepositoryMock.Verify(r => r.AddAsync(It.Is<PurchaseOrder>(o => o.Ticker == "WEGE3" && o.Quantity < 0)), Times.Once);
        _purchaseOrderRepositoryMock.Verify(r => r.AddAsync(It.Is<PurchaseOrder>(o => o.MasterAccountId == 1)), Times.Exactly(5));
    }

    [Fact]
    public async Task Handle_NoActiveBasket_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var executionDate = new DateTime(2026, 3, 5);
        var command = new ExecutePurchaseMotorCommand(executionDate, "correlation-no-basket");

        _clientRepositoryMock.Setup(r => r.GetClientsForExecutionAsync(executionDate.Day))
            .ReturnsAsync(new List<Client> { new Client("C", "000", "c@c.com", 100m, 5) });

        _recommendationBasketRepositoryMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecommendationBasket?)null);

        _stockQuoteRepositoryMock.Setup(r => r.GetLatestQuotesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<StockQuote>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }
}
