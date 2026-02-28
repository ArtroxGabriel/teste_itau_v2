using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ItauCompraProgramada.Application.Interfaces;
using ItauCompraProgramada.Application.Taxes.Services;
using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Enums;
using ItauCompraProgramada.Domain.Interfaces;
using ItauCompraProgramada.Domain.Repositories;

using MediatR;

using Microsoft.Extensions.Logging;

namespace ItauCompraProgramada.Application.Purchases.Commands.ExecutePurchaseMotor;

public class ExecutePurchaseMotorCommandHandler : IRequestHandler<ExecutePurchaseMotorCommand, ExecutePurchaseMotorResult>
{
    private readonly IClientRepository _clientRepository;
    private readonly IStockQuoteRepository _stockQuoteRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly ICustodyRepository _custodyRepository;
    private readonly IDistributionRepository _distributionRepository;
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
        IDistributionRepository distributionRepository,
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
        _distributionRepository = distributionRepository;
        _recommendationBasketRepository = recommendationBasketRepository;
        _irEventRepository = irEventRepository;
        _taxService = taxService;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task<ExecutePurchaseMotorResult> Handle(ExecutePurchaseMotorCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ExecutePurchaseMotorCommand started. Date: {Date}", request.ExecutionDate);

        var result = new ExecutePurchaseMotorResult([], [], [], []);

        var clients = await GetScheduledClientsAsync(request.ExecutionDate.Day);
        if (!clients.Any()) return result;

        var basket = await LoadActiveBasketAsync(cancellationToken);
        var basketQuotes = await LoadBasketQuotesAsync(basket, request.ExecutionDate);

        decimal totalContribution = clients.Sum(c => c.MonthlyContribution / 3);
        await ExecuteConsolidatedPurchaseAndDistributionAsync(clients, basket, basketQuotes, totalContribution, result);

        await ProcessRebalancingSalesAsync(clients, basket, basketQuotes, request.ExecutionDate, result);

        await SaveChangesAsync();
        await PublishTaxEventsAsync(result);

        _logger.LogInformation("ExecutePurchaseMotorCommand completed successfully.");
        return result;
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

    /// <summary>
    /// Loads the admin-managed active basket (US02).
    /// Throws if no basket has been configured yet.
    /// </summary>
    private async Task<RecommendationBasket> LoadActiveBasketAsync(CancellationToken cancellationToken)
    {
        var basket = await _recommendationBasketRepository.GetActiveAsync(cancellationToken);
        if (basket is null)
        {
            _logger.LogError("No active recommendation basket found. Purchase motor cannot run.");
            throw new InvalidOperationException(
                "Nenhuma cesta de recomendação ativa encontrada. Configure a cesta via POST /api/admin/cesta antes de executar o motor.");
        }

        _logger.LogInformation(
            "Active basket loaded. Id: {Id}, Name: {Name}, Items: {Tickers}",
            basket.Id, basket.Name, string.Join(", ", basket.Items.Select(i => i.Ticker)));

        return basket;
    }

    /// <summary>
    /// Fetches the latest stock quotes for all tickers in the basket.
    /// </summary>
    private async Task<Dictionary<string, StockQuote>> LoadBasketQuotesAsync(RecommendationBasket basket, DateTime date)
    {
        var quotes = await _stockQuoteRepository.GetLatestQuotesAsync(date);
        var quoteByTicker = quotes.ToDictionary(q => q.Ticker, q => q, StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, StockQuote>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in basket.Items)
        {
            if (quoteByTicker.TryGetValue(item.Ticker, out var quote))
            {
                result[item.Ticker] = quote;
            }
            else
            {
                _logger.LogWarning("No quote found for basket ticker {Ticker} on {Date}. Skipping.", item.Ticker, date);
            }
        }

        return result;
    }

    private async Task ExecuteConsolidatedPurchaseAndDistributionAsync(
        List<Client> clients,
        RecommendationBasket basket,
        Dictionary<string, StockQuote> basketQuotes,
        decimal totalContribution,
        ExecutePurchaseMotorResult result)
    {
        var masterAccountCustodies = await _custodyRepository.GetByAccountIdAsync(1);

        foreach (var item in basket.Items)
        {
            if (!basketQuotes.TryGetValue(item.Ticker, out var quote)) continue;

            // Use basket percentage for allocation (RN-015 guarantees sum = 100%)
            decimal allocationForTicker = totalContribution * (item.Percentage / 100m);
            int totalQuantityNeeded = (int)(allocationForTicker / quote.ClosingPrice);
            if (totalQuantityNeeded <= 0) continue;

            var masterCustody = masterAccountCustodies.FirstOrDefault(c => c.Ticker == quote.Ticker);
            int masterBalance = masterCustody?.Quantity ?? 0;
            int quantityToBuy = Math.Max(0, totalQuantityNeeded - masterBalance);

            if (quantityToBuy > 0)
            {
                await CreateMasterPurchaseOrdersAsync(quote, quantityToBuy, result);
            }

            int totalAvailable = masterBalance + quantityToBuy;
            int distributedCount = await DistributeStockToClientsAsync(clients, quote, item.Percentage, totalAvailable, totalContribution, result);

            await UpdateMasterResidueAsync(masterCustody, quote, totalAvailable - distributedCount, masterBalance, result);
        }
    }

    private async Task CreateMasterPurchaseOrdersAsync(StockQuote quote, int quantityToBuy, ExecutePurchaseMotorResult result)
    {
        int standardQuantity = (quantityToBuy / 100) * 100;
        int fractionalQuantity = quantityToBuy % 100;

        if (standardQuantity > 0)
        {
            var stdOrder = new PurchaseOrder(1, quote.Ticker, standardQuantity, quote.ClosingPrice, MarketType.Standard);
            await _purchaseOrderRepository.AddAsync(stdOrder);
            result.OrdensCompra.Add(new PurchaseOrderDto(stdOrder.Ticker, stdOrder.Quantity, stdOrder.UnitPrice, stdOrder.MarketType.ToString().ToUpperInvariant()));
        }

        if (fractionalQuantity > 0)
        {
            var fracOrder = new PurchaseOrder(1, quote.Ticker + "F", fractionalQuantity, quote.ClosingPrice, MarketType.Fractional);
            await _purchaseOrderRepository.AddAsync(fracOrder);
            result.OrdensCompra.Add(new PurchaseOrderDto(fracOrder.Ticker, fracOrder.Quantity, fracOrder.UnitPrice, fracOrder.MarketType.ToString().ToUpperInvariant()));
        }

        _logger.LogInformation("Buy orders created for {Ticker}: {Total} ({Standard} Std, {Fractional} Frac).",
            quote.Ticker, quantityToBuy, standardQuantity, fractionalQuantity);
    }

    private async Task<int> DistributeStockToClientsAsync(
        List<Client> clients,
        StockQuote quote,
        decimal basketPercentage,
        int totalAvailable,
        decimal totalContribution,
        ExecutePurchaseMotorResult result)
    {
        int distributedCount = 0;
        foreach (var client in clients)
        {
            if (client.GraphicAccount == null) continue;

            // Each client gets their proportional share of the basket-weighted allocation
            decimal clientContribution = client.MonthlyContribution / 3;
            decimal clientAllocation = clientContribution * (basketPercentage / 100m);
            int clientQuantity = quote.ClosingPrice > 0 ? (int)(clientAllocation / quote.ClosingPrice) : 0;
            if (clientQuantity <= 0) continue;

            // Ensure we do not distribute more than available
            clientQuantity = Math.Min(clientQuantity, totalAvailable - distributedCount);
            if (clientQuantity <= 0) continue;

            distributedCount += clientQuantity;
            await UpdateClientCustodyAndBalanceAsync(client, quote, clientQuantity, result);
        }
        return distributedCount;
    }

    private async Task UpdateClientCustodyAndBalanceAsync(Client client, StockQuote quote, int quantity, ExecutePurchaseMotorResult result)
    {
        var clientCustodies = await _custodyRepository.GetByAccountIdAsync(client.GraphicAccount!.Id);
        var clientCustody = clientCustodies.FirstOrDefault(c => c.Ticker == quote.Ticker);

        if (clientCustody != null)
        {
            clientCustody.UpdateAveragePrice(quantity, quote.ClosingPrice);
        }
        else
        {
            clientCustody = new Custody(client.GraphicAccount.Id, quote.Ticker, quantity, quote.ClosingPrice);
            await _custodyRepository.AddAsync(clientCustody);
        }

        // RN-034: Persist distribution details
        var distribution = new Distribution(0, clientCustody.Id, quote.Ticker, quantity, quote.ClosingPrice);
        await _distributionRepository.AddAsync(distribution);

        result.Distribuicoes.Add(new DistributionDto(client.Id, quote.Ticker, quantity, quote.ClosingPrice));

        var operationValue = quantity * quote.ClosingPrice;
        client.GraphicAccount.SubtractBalance(operationValue);

        var irDedoDuro = _taxService.CalculateIrDedoDuro(operationValue);
        if (irDedoDuro > 0)
        {
            await _irEventRepository.AddAsync(new IREvent(client.Id, IREventType.DedoDuro, operationValue, irDedoDuro));
            result.EventosIRPublicados.Add(new IREventDto("IR_DEDO_DURO", client.Id, irDedoDuro));
        }
    }

    private async Task UpdateMasterResidueAsync(Custody? masterCustody, StockQuote quote, int residue, int oldBalance, ExecutePurchaseMotorResult result)
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

        result.ResiduosCustMaster.Add(new MasterResidueDto(quote.Ticker, residue));
    }

    private async Task ProcessRebalancingSalesAsync(
        List<Client> clients,
        RecommendationBasket basket,
        Dictionary<string, StockQuote> basketQuotes,
        DateTime executionDate,
        ExecutePurchaseMotorResult result)
    {
        _logger.LogInformation("Starting portfolio rebalancing for clients.");
        var basketTickers = basket.Items.Select(i => i.Ticker).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var client in clients)
        {
            if (client.GraphicAccount == null) continue;

            var clientCustodies = await _custodyRepository.GetByAccountIdAsync(client.GraphicAccount.Id);
            var toSell = clientCustodies.Where(c => !basketTickers.Contains(c.Ticker) && c.Quantity > 0).ToList();

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
                var sellOrder = new PurchaseOrder(client.GraphicAccount.Id, custody.Ticker, -custody.Quantity, quote.ClosingPrice, MarketType.Standard);
                await _purchaseOrderRepository.AddAsync(sellOrder);
                result.OrdensCompra.Add(new PurchaseOrderDto(sellOrder.Ticker, sellOrder.Quantity, sellOrder.UnitPrice, sellOrder.MarketType.ToString().ToUpperInvariant()));

                var irDedoDuro = _taxService.CalculateIrDedoDuro(proceeds);
                if (irDedoDuro > 0)
                {
                    await _irEventRepository.AddAsync(new IREvent(client.Id, IREventType.DedoDuro, proceeds, irDedoDuro));
                    result.EventosIRPublicados.Add(new IREventDto("IR_DEDO_DURO", client.Id, irDedoDuro));
                }

                await _irEventRepository.AddAsync(new IREvent(client.Id, IREventType.SalesProfit, profit, 0m));
                custody.SubtractQuantity(custody.Quantity);
            }

            // RN-049: rebalance stayed tickers (sell excess / buy deficit based on current basket %)
            var (rebalanceSales, rebalanceProfit) = await ProcessStayedTickerRebalancingAsync(
                client, basket, basketQuotes, clientCustodies, result);
            executionSales += rebalanceSales;
            executionProfit += rebalanceProfit;

            await CalculateMonthlyProfitTaxAsync(client, executionSales, executionProfit, executionDate, result);
        }
    }

    /// <summary>
    /// RN-049 — For tickers that remain in the basket but may have changed percentage,
    /// compare the client's current custody quantity against the target derived from
    /// total portfolio value × basket percentage / price. Sell excess or buy deficit.
    /// Returns (totalSalesValue, totalProfit) from all stayed-ticker operations.
    /// </summary>
    private async Task<(decimal sales, decimal profit)> ProcessStayedTickerRebalancingAsync(
        Client client,
        RecommendationBasket basket,
        Dictionary<string, StockQuote> basketQuotes,
        List<Custody> clientCustodies,
        ExecutePurchaseMotorResult result)
    {
        // Step 1: compute total portfolio value using only basket tickers with available quotes
        decimal totalPortfolioValue = 0m;
        foreach (var item in basket.Items)
        {
            if (!basketQuotes.TryGetValue(item.Ticker, out var quote)) continue;
            var custody = clientCustodies.FirstOrDefault(c =>
                string.Equals(c.Ticker, item.Ticker, StringComparison.OrdinalIgnoreCase));
            if (custody != null && custody.Quantity > 0)
                totalPortfolioValue += custody.Quantity * quote.ClosingPrice;
        }

        if (totalPortfolioValue <= 0m)
        {
            _logger.LogDebug(
                "Client {ClientId}: portfolio value is zero — skipping stayed-ticker rebalancing.",
                client.Id);
            return (0m, 0m);
        }

        decimal rebalanceSales = 0m;
        decimal rebalanceProfit = 0m;

        // Step 2: for each basket ticker compute target and delta
        foreach (var item in basket.Items)
        {
            if (!basketQuotes.TryGetValue(item.Ticker, out var quote)) continue;

            var custody = clientCustodies.FirstOrDefault(c =>
                string.Equals(c.Ticker, item.Ticker, StringComparison.OrdinalIgnoreCase));

            int currentQuantity = custody?.Quantity ?? 0;
            int targetQuantity = (int)(totalPortfolioValue * (item.Percentage / 100m) / quote.ClosingPrice);
            int delta = targetQuantity - currentQuantity;

            if (delta == 0) continue;

            if (delta < 0)
            {
                // Sell excess (delta is negative → abs value = shares to sell)
                int toSellQty = -delta;
                var (proceeds, profit) = await SellStayedTickerExcessAsync(client, custody!, quote, toSellQty, result);
                rebalanceSales += proceeds;
                rebalanceProfit += profit;
            }
            else
            {
                // Buy deficit
                await BuyStayedTickerDeficitAsync(client, custody, quote, item.Ticker, delta, clientCustodies, result);
            }
        }

        return (rebalanceSales, rebalanceProfit);
    }

    private async Task<(decimal proceeds, decimal profit)> SellStayedTickerExcessAsync(
        Client client,
        Custody custody,
        StockQuote quote,
        int quantity,
        ExecutePurchaseMotorResult result)
    {
        var proceeds = quantity * quote.ClosingPrice;
        var profit = (quote.ClosingPrice - custody.AveragePrice) * quantity;

        client.GraphicAccount!.AddBalance(proceeds);
        custody.SubtractQuantity(quantity);

        var sellOrder = new PurchaseOrder(client.GraphicAccount.Id, quote.Ticker, -quantity, quote.ClosingPrice, MarketType.Standard);
        await _purchaseOrderRepository.AddAsync(sellOrder);
        result.OrdensCompra.Add(new PurchaseOrderDto(sellOrder.Ticker, sellOrder.Quantity, sellOrder.UnitPrice, sellOrder.MarketType.ToString().ToUpperInvariant()));

        var irDedoDuro = _taxService.CalculateIrDedoDuro(proceeds);
        if (irDedoDuro > 0)
        {
            await _irEventRepository.AddAsync(new IREvent(client.Id, IREventType.DedoDuro, proceeds, irDedoDuro));
            result.EventosIRPublicados.Add(new IREventDto("IR_DEDO_DURO", client.Id, irDedoDuro));
        }

        await _irEventRepository.AddAsync(new IREvent(client.Id, IREventType.SalesProfit, profit, 0m));

        _logger.LogInformation(
            "Client {ClientId}: RN-049 sell {Quantity} {Ticker} @ {Price} (proceeds {Proceeds:C}).",
            client.Id, quantity, quote.Ticker, quote.ClosingPrice, proceeds);

        return (proceeds, profit);
    }

    private async Task BuyStayedTickerDeficitAsync(
        Client client,
        Custody? custody,
        StockQuote quote,
        string ticker,
        int quantity,
        List<Custody> clientCustodies,
        ExecutePurchaseMotorResult result)
    {
        var cost = quantity * quote.ClosingPrice;

        // Only buy if client has sufficient balance
        if (client.GraphicAccount!.Balance < cost)
        {
            _logger.LogWarning(
                "Client {ClientId}: insufficient balance ({Balance:C}) to buy {Quantity} {Ticker} @ {Price:C} (cost {Cost:C}). Skipping RN-049 deficit buy.",
                client.Id, client.GraphicAccount.Balance, quantity, ticker, quote.ClosingPrice, cost);
            return;
        }

        client.GraphicAccount.SubtractBalance(cost);

        if (custody != null)
        {
            custody.UpdateAveragePrice(quantity, quote.ClosingPrice);
        }
        else
        {
            var newCustody = new Custody(client.GraphicAccount.Id, ticker, quantity, quote.ClosingPrice);
            await _custodyRepository.AddAsync(newCustody);
            clientCustodies.Add(newCustody);
        }

        var buyOrder = new PurchaseOrder(client.GraphicAccount.Id, ticker, quantity, quote.ClosingPrice, MarketType.Standard);
        await _purchaseOrderRepository.AddAsync(buyOrder);
        result.OrdensCompra.Add(new PurchaseOrderDto(buyOrder.Ticker, buyOrder.Quantity, buyOrder.UnitPrice, buyOrder.MarketType.ToString().ToUpperInvariant()));

        var irDedoDuro = _taxService.CalculateIrDedoDuro(cost);
        if (irDedoDuro > 0)
        {
            await _irEventRepository.AddAsync(new IREvent(client.Id, IREventType.DedoDuro, cost, irDedoDuro));
            result.EventosIRPublicados.Add(new IREventDto("IR_DEDO_DURO", client.Id, irDedoDuro));
        }

        _logger.LogInformation(
            "Client {ClientId}: RN-049 buy {Quantity} {Ticker} @ {Price} (cost {Cost:C}).",
            client.Id, quantity, ticker, quote.ClosingPrice, cost);
    }

    private async Task CalculateMonthlyProfitTaxAsync(Client client, decimal currentSales, decimal currentProfit, DateTime date, ExecutePurchaseMotorResult result)
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
                {
                    await _irEventRepository.AddAsync(new IREvent(client.Id, IREventType.SalesProfit, 0m, taxToPayNow));
                    result.EventosIRPublicados.Add(new IREventDto("IR_VENDA", client.Id, taxToPayNow));
                }
            }
        }
    }

    private async Task SaveChangesAsync()
    {
        await _purchaseOrderRepository.SaveChangesAsync();
        await _clientRepository.SaveChangesAsync();
        await _recommendationBasketRepository.SaveChangesAsync();
        await _custodyRepository.SaveChangesAsync();
        await _distributionRepository.SaveChangesAsync();
        await _irEventRepository.SaveChangesAsync();
    }

    private async Task PublishTaxEventsAsync(ExecutePurchaseMotorResult result)
    {
        var pendingEvents = await _irEventRepository.GetPendingPublicationAsync();
        foreach (var irEvent in pendingEvents)
        {
            var topic = irEvent.Type == IREventType.DedoDuro ? "ir-dedo-duro" : "ir-profit-tax";
            var message = new { irEvent.Id, irEvent.ClienteId, irEvent.Type, irEvent.BaseValue, irEvent.IRValue, irEvent.EventDate, Topic = topic };

            await _kafkaProducer.PublishAsync(topic, message);
            irEvent.MarkAsPublished();
            
            // If they weren't added dynamically earlier (e.g. from previous runs), add to output
            if (!result.EventosIRPublicados.Any(e => e.ClienteId == irEvent.ClienteId && e.Valor == irEvent.IRValue))
            {
                result.EventosIRPublicados.Add(new IREventDto(irEvent.Type.ToString().ToUpperInvariant(), irEvent.ClienteId, irEvent.IRValue));
            }
        }

        if (pendingEvents.Any())
            await _irEventRepository.SaveChangesAsync();
    }
}
