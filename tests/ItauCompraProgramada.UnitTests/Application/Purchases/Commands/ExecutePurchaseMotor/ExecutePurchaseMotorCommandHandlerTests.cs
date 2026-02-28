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

    // ─── RN-049: stayed-ticker rebalancing ────────────────────────────────────

    /// <summary>
    /// RN-049: Client holds 8 PETR4 (basket 25%), 4 VALE3 (basket 20%), 6 ITUB4 (basket 20%).
    /// Portfolio: 8×35 + 4×62 + 6×30 = 280+248+180 = 708.
    /// PETR4 target = TRUNCATE(708 × 25% / 35) = TRUNCATE(5.06) = 5  → sell 3
    /// VALE3 target = TRUNCATE(708 × 20% / 62) = TRUNCATE(2.28) = 2  → sell 2
    /// ITUB4 target = TRUNCATE(708 × 20% / 30) = TRUNCATE(4.72) = 4  → sell 2
    /// </summary>
    [Fact]
    public async Task Handle_StayedTicker_WithExcessShares_ShouldSellExcess()
    {
        // Arrange
        var executionDate = new DateTime(2026, 3, 5);
        var command = new ExecutePurchaseMotorCommand(executionDate, "rn049-sell-excess");

        // Basket with only stayed tickers (no removed, no new — so all 5 slots used for stayed)
        var basket = new RecommendationBasket("Top Five", new List<BasketItem>
        {
            new BasketItem("PETR4", 25m),
            new BasketItem("VALE3", 20m),
            new BasketItem("ITUB4", 20m),
            new BasketItem("ABEV3", 20m),
            new BasketItem("RENT3", 15m),
        });
        _recommendationBasketRepositoryMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        var quotes = new List<StockQuote>
        {
            new StockQuote(executionDate.AddDays(-1), "PETR4", 35m, 35m, 36m, 34m),
            new StockQuote(executionDate.AddDays(-1), "VALE3", 62m, 62m, 63m, 61m),
            new StockQuote(executionDate.AddDays(-1), "ITUB4", 30m, 30m, 31m, 29m),
            new StockQuote(executionDate.AddDays(-1), "ABEV3", 14m, 14m, 15m, 13m),
            new StockQuote(executionDate.AddDays(-1), "RENT3", 48m, 48m, 49m, 47m),
        };
        _stockQuoteRepositoryMock
            .Setup(r => r.GetLatestQuotesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(quotes);
        _stockQuoteRepositoryMock
            .Setup(r => r.GetLatestByTickerAsync(It.IsAny<string>()))
            .Returns<string>(ticker => Task.FromResult(quotes.FirstOrDefault(q => q.Ticker == ticker)));

        var client = new Client("Client RN049", "111", "rn049@test.com", 3000m, 5);

        // Client already holds PETR4, VALE3, ITUB4 — portfolio = 708
        var clientCustodies = new List<Custody>
        {
            new Custody(client.GraphicAccount!.Id, "PETR4", 8, 35m),
            new Custody(client.GraphicAccount.Id, "VALE3", 4, 62m),
            new Custody(client.GraphicAccount.Id, "ITUB4", 6, 30m),
        };

        _clientRepositoryMock
            .Setup(r => r.GetClientsForExecutionAsync(executionDate.Day))
            .ReturnsAsync(new List<Client> { client });

        _custodyRepositoryMock.Setup(r => r.GetByAccountIdAsync(1)).ReturnsAsync(new List<Custody>());
        _custodyRepositoryMock
            .Setup(r => r.GetByAccountIdAsync(client.GraphicAccount.Id))
            .ReturnsAsync(clientCustodies);

        _irEventRepositoryMock.Setup(r => r.GetPendingPublicationAsync()).ReturnsAsync(new List<IREvent>());
        _purchaseOrderRepositoryMock
            .Setup(r => r.GetTotalSalesValueInMonthAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(0m);
        _irEventRepositoryMock
            .Setup(r => r.GetTotalBaseValueInMonthAsync(It.IsAny<long>(), It.IsAny<ItauCompraProgramada.Domain.Enums.IREventType>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(0m);
        _irEventRepositoryMock
            .Setup(r => r.GetTotalIRValueInMonthAsync(It.IsAny<long>(), It.IsAny<ItauCompraProgramada.Domain.Enums.IREventType>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(0m);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert: sell orders (negative quantity) issued for PETR4, VALE3, ITUB4 (all over target)
        _purchaseOrderRepositoryMock.Verify(
            r => r.AddAsync(It.Is<PurchaseOrder>(o => o.Ticker == "PETR4" && o.Quantity < 0)),
            Times.Once, "Expected sell order for PETR4 (RN-049 excess)");

        _purchaseOrderRepositoryMock.Verify(
            r => r.AddAsync(It.Is<PurchaseOrder>(o => o.Ticker == "VALE3" && o.Quantity < 0)),
            Times.Once, "Expected sell order for VALE3 (RN-049 excess)");

        _purchaseOrderRepositoryMock.Verify(
            r => r.AddAsync(It.Is<PurchaseOrder>(o => o.Ticker == "ITUB4" && o.Quantity < 0)),
            Times.Once, "Expected sell order for ITUB4 (RN-049 excess)");
    }

    /// <summary>
    /// RN-049: Client holds 2 PETR4 (basket 25%) and 2 VALE3 (basket 20%).
    /// Portfolio: 2×35 + 2×62 = 70+124 = 194.
    /// PETR4 target = TRUNCATE(194 × 25% / 35) = TRUNCATE(1.39) = 1  → sell 1
    /// VALE3 target = TRUNCATE(194 × 20% / 62) = TRUNCATE(0.63) = 0  → sell 2
    ///
    /// Actually, let's pick values that reliably produce a BUY deficit:
    /// PETR4=1 share, VALE3=0 shares, ITUB4=0 shares.
    /// Portfolio: 1×35 = 35 (only tickers with custody contribute).
    /// PETR4 target = TRUNCATE(35 × 25% / 35) = 0  → would sell 1, not buy.
    ///
    /// Better scenario: Client holds only ITUB4=1 at R$30 in a basket where ITUB4 is 50%.
    /// But the basket must have 5 tickers. Use a basket where ITUB4=40%.
    /// Portfolio = 1×30 = 30. Target = TRUNCATE(30 × 40% / 30) = TRUNCATE(0.4) = 0 → would sell.
    ///
    /// Clean approach: 1 PETR4 at R$10, portfolio=10, basket PETR4=100% (not valid).
    /// → Use: client has 1 PETR4 at 35, basket PETR4=25%. Total portfolio=35.
    ///   PETR4 target=TRUNCATE(35×25%/35)=0 → sell.
    ///
    /// The reliable way to get a BUY deficit: client has NO custody yet for a ticker in the basket,
    /// but DOES have custody for other basket tickers so portfolio value is non-zero.
    /// Client has: VALE3=5 at 62 = 310. Basket: PETR4=25%, VALE3=20%, ITUB4=20%, ABEV3=20%, RENT3=15%.
    /// Portfolio includes only VALE3 (only held ticker in basket) = 310.
    /// PETR4 target = TRUNCATE(310 × 25% / 35) = TRUNCATE(2.21) = 2 → buy 2 (client has 0)
    /// VALE3 target = TRUNCATE(310 × 20% / 62) = TRUNCATE(1.00) = 1 → sell 4 (client has 5)
    /// ...
    /// Let's verify PETR4 produces a buy: 0 held vs target 2 → delta +2 → BUY. ✓
    /// </summary>
    [Fact]
    public async Task Handle_StayedTicker_WithDeficitShares_ShouldBuyDeficit()
    {
        // Arrange
        var executionDate = new DateTime(2026, 3, 5);
        var command = new ExecutePurchaseMotorCommand(executionDate, "rn049-buy-deficit");

        var basket = new RecommendationBasket("Top Five", new List<BasketItem>
        {
            new BasketItem("PETR4", 25m),
            new BasketItem("VALE3", 20m),
            new BasketItem("ITUB4", 20m),
            new BasketItem("ABEV3", 20m),
            new BasketItem("RENT3", 15m),
        });
        _recommendationBasketRepositoryMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        var quotes = new List<StockQuote>
        {
            new StockQuote(executionDate.AddDays(-1), "PETR4", 35m, 35m, 36m, 34m),
            new StockQuote(executionDate.AddDays(-1), "VALE3", 62m, 62m, 63m, 61m),
            new StockQuote(executionDate.AddDays(-1), "ITUB4", 30m, 30m, 31m, 29m),
            new StockQuote(executionDate.AddDays(-1), "ABEV3", 14m, 14m, 15m, 13m),
            new StockQuote(executionDate.AddDays(-1), "RENT3", 48m, 48m, 49m, 47m),
        };
        _stockQuoteRepositoryMock
            .Setup(r => r.GetLatestQuotesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(quotes);
        _stockQuoteRepositoryMock
            .Setup(r => r.GetLatestByTickerAsync(It.IsAny<string>()))
            .Returns<string>(ticker => Task.FromResult(quotes.FirstOrDefault(q => q.Ticker == ticker)));

        // Client has a large VALE3 position but NO PETR4.
        // Portfolio (basket tickers only): VALE3=5×62 = 310.
        // PETR4 target = TRUNCATE(310 × 25% / 35) = 2  → BUY 2 (has 0)
        var client = new Client("Client RN049 Deficit", "222", "rn049b@test.com", 3000m, 5);
        client.GraphicAccount!.AddBalance(10_000m); // ensure enough balance for buys

        var clientCustodies = new List<Custody>
        {
            new Custody(client.GraphicAccount.Id, "VALE3", 5, 62m),
        };

        _clientRepositoryMock
            .Setup(r => r.GetClientsForExecutionAsync(executionDate.Day))
            .ReturnsAsync(new List<Client> { client });

        _custodyRepositoryMock.Setup(r => r.GetByAccountIdAsync(1)).ReturnsAsync(new List<Custody>());
        _custodyRepositoryMock
            .Setup(r => r.GetByAccountIdAsync(client.GraphicAccount.Id))
            .ReturnsAsync(clientCustodies);

        _irEventRepositoryMock.Setup(r => r.GetPendingPublicationAsync()).ReturnsAsync(new List<IREvent>());
        _purchaseOrderRepositoryMock
            .Setup(r => r.GetTotalSalesValueInMonthAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(0m);
        _irEventRepositoryMock
            .Setup(r => r.GetTotalBaseValueInMonthAsync(It.IsAny<long>(), It.IsAny<ItauCompraProgramada.Domain.Enums.IREventType>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(0m);
        _irEventRepositoryMock
            .Setup(r => r.GetTotalIRValueInMonthAsync(It.IsAny<long>(), It.IsAny<ItauCompraProgramada.Domain.Enums.IREventType>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(0m);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert: buy order (positive quantity) created for PETR4 (deficit)
        _purchaseOrderRepositoryMock.Verify(
            r => r.AddAsync(It.Is<PurchaseOrder>(o =>
                o.Ticker == "PETR4" && o.Quantity > 0 && o.MasterAccountId == client.GraphicAccount.Id)),
            Times.AtLeastOnce, "Expected buy order for PETR4 (RN-049 deficit)");
    }

    /// <summary>
    /// RN-049: When target quantity == current quantity, no order should be created for that ticker.
    /// Client holds exactly TRUNCATE(portfolio × pct / price) shares of each basket ticker.
    /// </summary>
    [Fact]
    public async Task Handle_StayedTicker_WithExactShares_ShouldNotCreateOrder()
    {
        // Arrange
        var executionDate = new DateTime(2026, 3, 5);
        var command = new ExecutePurchaseMotorCommand(executionDate, "rn049-no-change");

        // Single ticker basket (not valid in production, but valid for this unit test scenario)
        // Use 5-ticker basket where client's holdings already match targets.
        // Portfolio: PETR4=6×35=210, VALE3=0, ITUB4=0, ABEV3=0, RENT3=0  (total=210)
        // PETR4 target = TRUNCATE(210×25%/35) = TRUNCATE(1.5) = 1  → not equal to 6
        //
        // To get exact match we need: qty = TRUNCATE(totalPortfolio × pct / price)
        // Let's use: PETR4=1 share at R$35, basket PETR4=25%.
        // Portfolio = 35. PETR4 target = TRUNCATE(35×25%/35)=TRUNCATE(0.25)=0 → mismatch.
        //
        // Guaranteed exact match: pick price and pct so that qty = TRUNCATE(qty×price × pct/price)
        // i.e. qty = TRUNCATE(qty × pct%) → true when pct=100% (single ticker) or when remainder is 0.
        // With 5 tickers each 20%: qty = TRUNCATE(totalPortfolio × 20% / price)
        // If only one ticker with custody: qty=k, portfolio=k×price, target=TRUNCATE(k×price×20%/price)=TRUNCATE(k×0.2)
        // For k=5: target = TRUNCATE(5×0.2)=TRUNCATE(1)=1 ≠ 5.
        //
        // Simplest approach: client has NO custodies at all for basket tickers.
        // Portfolio = 0. All targets = 0. Delta = 0 for all. No orders.
        var basket = new RecommendationBasket("Top Five", new List<BasketItem>
        {
            new BasketItem("PETR4", 25m),
            new BasketItem("VALE3", 20m),
            new BasketItem("ITUB4", 20m),
            new BasketItem("ABEV3", 20m),
            new BasketItem("RENT3", 15m),
        });
        _recommendationBasketRepositoryMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        var quotes = new List<StockQuote>
        {
            new StockQuote(executionDate.AddDays(-1), "PETR4", 35m, 35m, 36m, 34m),
            new StockQuote(executionDate.AddDays(-1), "VALE3", 62m, 62m, 63m, 61m),
            new StockQuote(executionDate.AddDays(-1), "ITUB4", 30m, 30m, 31m, 29m),
            new StockQuote(executionDate.AddDays(-1), "ABEV3", 14m, 14m, 15m, 13m),
            new StockQuote(executionDate.AddDays(-1), "RENT3", 48m, 48m, 49m, 47m),
        };
        _stockQuoteRepositoryMock
            .Setup(r => r.GetLatestQuotesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(quotes);

        // Client has no basket-ticker custodies → portfolio = 0, all targets = 0, all deltas = 0
        var client = new Client("Client RN049 NoChange", "333", "rn049c@test.com", 3000m, 5);

        _clientRepositoryMock
            .Setup(r => r.GetClientsForExecutionAsync(executionDate.Day))
            .ReturnsAsync(new List<Client> { client });

        _custodyRepositoryMock.Setup(r => r.GetByAccountIdAsync(1)).ReturnsAsync(new List<Custody>());
        _custodyRepositoryMock
            .Setup(r => r.GetByAccountIdAsync(client.GraphicAccount!.Id))
            .ReturnsAsync(new List<Custody>()); // no custodies at all

        _irEventRepositoryMock.Setup(r => r.GetPendingPublicationAsync()).ReturnsAsync(new List<IREvent>());
        _purchaseOrderRepositoryMock
            .Setup(r => r.GetTotalSalesValueInMonthAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(0m);
        _irEventRepositoryMock
            .Setup(r => r.GetTotalBaseValueInMonthAsync(It.IsAny<long>(), It.IsAny<ItauCompraProgramada.Domain.Enums.IREventType>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(0m);
        _irEventRepositoryMock
            .Setup(r => r.GetTotalIRValueInMonthAsync(It.IsAny<long>(), It.IsAny<ItauCompraProgramada.Domain.Enums.IREventType>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(0m);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert: NO rebalancing orders (positive OR negative) created on the client's account
        _purchaseOrderRepositoryMock.Verify(
            r => r.AddAsync(It.Is<PurchaseOrder>(o =>
                o.MasterAccountId == client.GraphicAccount!.Id && (o.Ticker == "PETR4" || o.Ticker == "VALE3" || o.Ticker == "ITUB4"))),
            Times.Never, "Expected no rebalancing order for already-exact holdings (RN-049)");
    }
}
