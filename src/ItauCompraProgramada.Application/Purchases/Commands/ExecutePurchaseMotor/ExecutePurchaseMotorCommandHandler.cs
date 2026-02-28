using ItauCompraProgramada.Application.Interfaces;
using ItauCompraProgramada.Application.Taxes.Services;
using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Enums;
using ItauCompraProgramada.Domain.Interfaces;
using ItauCompraProgramada.Domain.Repositories;

using MediatR;

using Microsoft.Extensions.Logging;

namespace ItauCompraProgramada.Application.Purchases.Commands.ExecutePurchaseMotor;

public class ExecutePurchaseMotorCommandHandler : IRequestHandler<ExecutePurchaseMotorCommand>
{
    private readonly IClientRepository _clientRepository;
    private readonly IStockQuoteRepository _stockQuoteRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly ICustodyRepository _custodyRepository;
    private readonly IRecommendationBasketRepository _recommendationBasketRepository;
    private readonly IIREventRepository _irEventRepository;
    private readonly TaxService _taxService;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ILogger<ExecutePurchaseMotorCommandHandler> _logger;

    public ExecutePurchaseMotorCommandHandler(
        IClientRepository clientRepository,
        IStockQuoteRepository stockQuoteRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        ICustodyRepository custodyRepository,
        IRecommendationBasketRepository recommendationBasketRepository,
        IIREventRepository irEventRepository,
        TaxService taxService,
        IKafkaProducer kafkaProducer,
        ILogger<ExecutePurchaseMotorCommandHandler> logger)
    {
        _clientRepository = clientRepository;
        _stockQuoteRepository = stockQuoteRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _custodyRepository = custodyRepository;
        _recommendationBasketRepository = recommendationBasketRepository;
        _irEventRepository = irEventRepository;
        _taxService = taxService;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task Handle(ExecutePurchaseMotorCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ExecutePurchaseMotorCommand started. Date: {Date}", request.ExecutionDate);

        var clients = await GetScheduledClientsAsync(request.ExecutionDate.Day);
        if (!clients.Any()) return;

        var top5Quotes = await DetermineAndRefreshTop5BasketAsync(request.ExecutionDate);

        decimal totalContribution = clients.Sum(c => c.MonthlyContribution / 3);
        await ExecuteConsolidatedPurchaseAndDistributionAsync(clients, top5Quotes, totalContribution);

        await ProcessRebalancingSalesAsync(clients, top5Quotes, request.ExecutionDate);

        await SaveChangesAsync();
        await PublishTaxEventsAsync();

        _logger.LogInformation("ExecutePurchaseMotorCommand completed successfully.");
    }

    private async Task<List<Client>> GetScheduledClientsAsync(int day)
    {
        var clients = await _clientRepository.GetClientsForExecutionAsync(day);
        if (!clients.Any())
        {
            _logger.LogInformation("No clients scheduled for execution today ({Day}).", day);
        }
        else
        {
            _logger.LogInformation("Processing {Count} clients for day {Day}.", clients.Count, day);
        }
        return clients;
    }

    private async Task<List<StockQuote>> DetermineAndRefreshTop5BasketAsync(DateTime date)
    {
        var quotes = await _stockQuoteRepository.GetLatestQuotesAsync(date);
        var top5Quotes = quotes
            .Select(q => new
            {
                Quote = q,
                Performance = q.OpeningPrice > 0 ? (q.ClosingPrice - q.OpeningPrice) / q.OpeningPrice : 0
            })
            .OrderByDescending(x => x.Performance)
            .Take(5)
            .Select(x => x.Quote)
            .ToList();

        if (top5Quotes.Count < 5)
        {
            _logger.LogError("Only {Count} quotes found. Need 5 to determine Top 5.", top5Quotes.Count);
            throw new InvalidOperationException("Not enough quotes to determine Top 5.");
        }

        _logger.LogInformation("Top 5 stocks identified: {Tickers}", string.Join(", ", top5Quotes.Select(q => q.Ticker)));

        var basketItems = top5Quotes.Select(q => new BasketItem(q.Ticker, 20m)).ToList();
        var newBasket = new RecommendationBasket($"Top 5 - {date:yyyy-MM-dd}", basketItems);
        var previousBasket = await _recommendationBasketRepository.GetActiveAsync();
        previousBasket?.Deactivate();
        await _recommendationBasketRepository.AddAsync(newBasket);

        return top5Quotes;
    }

    private async Task ExecuteConsolidatedPurchaseAndDistributionAsync(List<Client> clients, List<StockQuote> top5Quotes, decimal totalContribution)
    {
        decimal contributionPerStock = totalContribution / 5;
        var masterAccountCustodies = await _custodyRepository.GetByAccountIdAsync(1);

        foreach (var quote in top5Quotes)
        {
            int totalQuantityNeeded = (int)(contributionPerStock / quote.ClosingPrice);
            if (totalQuantityNeeded <= 0) continue;

            var masterCustody = masterAccountCustodies.FirstOrDefault(c => c.Ticker == quote.Ticker);
            int masterBalance = masterCustody?.Quantity ?? 0;
            int quantityToBuy = Math.Max(0, totalQuantityNeeded - masterBalance);

            if (quantityToBuy > 0)
            {
                await CreateMasterPurchaseOrdersAsync(quote, quantityToBuy);
            }

            int totalAvailable = masterBalance + quantityToBuy;
            int distributedCount = await DistributeStockToClientsAsync(clients, quote, totalAvailable, totalContribution);

            await UpdateMasterResidueAsync(masterCustody, quote, totalAvailable - distributedCount, masterBalance);
        }
    }

    private async Task CreateMasterPurchaseOrdersAsync(StockQuote quote, int quantityToBuy)
    {
        int standardQuantity = (quantityToBuy / 100) * 100;
        int fractionalQuantity = quantityToBuy % 100;

        if (standardQuantity > 0)
            await _purchaseOrderRepository.AddAsync(new PurchaseOrder(1, quote.Ticker, standardQuantity, quote.ClosingPrice, MarketType.Standard));

        if (fractionalQuantity > 0)
            await _purchaseOrderRepository.AddAsync(new PurchaseOrder(1, quote.Ticker + "F", fractionalQuantity, quote.ClosingPrice, MarketType.Fractional));

        _logger.LogInformation("Buy orders created for {Ticker}: {Total} ({Standard} Std, {Fractional} Frac).",
            quote.Ticker, quantityToBuy, standardQuantity, fractionalQuantity);
    }

    private async Task<int> DistributeStockToClientsAsync(List<Client> clients, StockQuote quote, int totalAvailable, decimal totalContribution)
    {
        int distributedCount = 0;
        foreach (var client in clients)
        {
            if (client.GraphicAccount == null) continue;

            decimal proportion = (client.MonthlyContribution / 3) / totalContribution;
            int clientQuantity = (int)(totalAvailable * proportion);
            if (clientQuantity <= 0) continue;

            distributedCount += clientQuantity;
            await UpdateClientCustodyAndBalanceAsync(client, quote, clientQuantity);
        }
        return distributedCount;
    }

    private async Task UpdateClientCustodyAndBalanceAsync(Client client, StockQuote quote, int quantity)
    {
        var clientCustodies = await _custodyRepository.GetByAccountIdAsync(client.GraphicAccount!.Id);
        var clientCustody = clientCustodies.FirstOrDefault(c => c.Ticker == quote.Ticker);

        if (clientCustody != null)
            clientCustody.UpdateAveragePrice(quantity, quote.ClosingPrice);
        else
            await _custodyRepository.AddAsync(new Custody(client.GraphicAccount.Id, quote.Ticker, quantity, quote.ClosingPrice));

        var operationValue = quantity * quote.ClosingPrice;
        client.GraphicAccount.SubtractBalance(operationValue);

        var irDedoDuro = _taxService.CalculateIrDedoDuro(operationValue);
        if (irDedoDuro > 0)
        {
            await _irEventRepository.AddAsync(new IREvent(client.Id, IREventType.DedoDuro, operationValue, irDedoDuro));
        }
    }

    private async Task UpdateMasterResidueAsync(Custody? masterCustody, StockQuote quote, int residue, int oldBalance)
    {
        if (masterCustody != null)
        {
            masterCustody.SubtractQuantity(oldBalance);
            masterCustody.UpdateAveragePrice(residue, quote.ClosingPrice);
        }
        else if (residue > 0)
        {
            await _custodyRepository.AddAsync(new Custody(1, quote.Ticker, residue, quote.ClosingPrice));
        }
    }

    private async Task ProcessRebalancingSalesAsync(List<Client> clients, List<StockQuote> top5Quotes, DateTime executionDate)
    {
        _logger.LogInformation("Starting portfolio rebalancing for clients.");
        var top5Tickers = top5Quotes.Select(q => q.Ticker).ToList();

        foreach (var client in clients)
        {
            if (client.GraphicAccount == null) continue;

            var clientCustodies = await _custodyRepository.GetByAccountIdAsync(client.GraphicAccount.Id);
            var toSell = clientCustodies.Where(c => !top5Tickers.Contains(c.Ticker) && c.Quantity > 0).ToList();

            decimal executionSales = 0m;
            decimal executionProfit = 0m;

            foreach (var custody in toSell)
            {
                var quote = await _stockQuoteRepository.GetLatestByTickerAsync(custody.Ticker);
                if (quote == null) continue;

                var proceeds = (decimal)custody.Quantity * quote.ClosingPrice;
                var profit = (quote.ClosingPrice - custody.AveragePrice) * custody.Quantity;

                executionSales += proceeds;
                executionProfit += profit;

                client.GraphicAccount.AddBalance(proceeds);
                await _purchaseOrderRepository.AddAsync(new PurchaseOrder(client.GraphicAccount.Id, custody.Ticker, -custody.Quantity, quote.ClosingPrice, MarketType.Standard));

                var irDedoDuro = _taxService.CalculateIrDedoDuro(proceeds);
                if (irDedoDuro > 0)
                    await _irEventRepository.AddAsync(new IREvent(client.Id, IREventType.DedoDuro, proceeds, irDedoDuro));

                await _irEventRepository.AddAsync(new IREvent(client.Id, IREventType.SalesProfit, profit, 0m));
                custody.SubtractQuantity(custody.Quantity);
            }

            await CalculateMonthlyProfitTaxAsync(client, executionSales, executionProfit, executionDate);
        }
    }

    private async Task CalculateMonthlyProfitTaxAsync(Client client, decimal currentSales, decimal currentProfit, DateTime date)
    {
        var totalSales = await _purchaseOrderRepository.GetTotalSalesValueInMonthAsync(client.GraphicAccount!.Id, date.Year, date.Month) + currentSales;

        if (totalSales > 20000m)
        {
            var totalProfit = await _irEventRepository.GetTotalBaseValueInMonthAsync(client.Id, IREventType.SalesProfit, date.Year, date.Month) + currentProfit;
            var requiredTax = _taxService.CalculateProfitTax(totalSales, totalProfit);

            if (requiredTax > 0)
            {
                var paidTax = await _irEventRepository.GetTotalIRValueInMonthAsync(client.Id, IREventType.SalesProfit, date.Year, date.Month);
                var taxToPayNow = requiredTax - paidTax;

                if (taxToPayNow > 0)
                    await _irEventRepository.AddAsync(new IREvent(client.Id, IREventType.SalesProfit, 0m, taxToPayNow));
            }
        }
    }

    private async Task SaveChangesAsync()
    {
        await _purchaseOrderRepository.SaveChangesAsync();
        await _clientRepository.SaveChangesAsync();
        await _recommendationBasketRepository.SaveChangesAsync();
        await _custodyRepository.SaveChangesAsync();
        await _irEventRepository.SaveChangesAsync();
    }

    private async Task PublishTaxEventsAsync()
    {
        var pendingEvents = await _irEventRepository.GetPendingPublicationAsync();
        foreach (var irEvent in pendingEvents)
        {
            var topic = irEvent.Type == IREventType.DedoDuro ? "ir-dedo-duro" : "ir-profit-tax";
            var message = new { irEvent.Id, irEvent.ClienteId, irEvent.Type, irEvent.BaseValue, irEvent.IRValue, irEvent.EventDate, Topic = topic };

            await _kafkaProducer.PublishAsync(topic, message);
            irEvent.MarkAsPublished();
        }

        if (pendingEvents.Any())
            await _irEventRepository.SaveChangesAsync();
    }
}