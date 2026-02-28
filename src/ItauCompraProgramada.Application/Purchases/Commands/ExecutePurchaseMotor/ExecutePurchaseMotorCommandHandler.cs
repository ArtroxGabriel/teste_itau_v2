using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Enums;
using ItauCompraProgramada.Domain.Interfaces;
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
    private readonly ILogger<ExecutePurchaseMotorCommandHandler> _logger;

    public ExecutePurchaseMotorCommandHandler(
        IClientRepository clientRepository,
        IStockQuoteRepository stockQuoteRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        ICustodyRepository custodyRepository,
        IRecommendationBasketRepository recommendationBasketRepository,
        ILogger<ExecutePurchaseMotorCommandHandler> logger)
    {
        _clientRepository = clientRepository;
        _stockQuoteRepository = stockQuoteRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _custodyRepository = custodyRepository;
        _recommendationBasketRepository = recommendationBasketRepository;
        _logger = logger;
    }

    public async Task Handle(ExecutePurchaseMotorCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ExecutePurchaseMotorCommand started. Date: {Date}", request.ExecutionDate);

        // 1. Identify Clients Scheduled for Today
        var clients = await _clientRepository.GetClientsForExecutionAsync(request.ExecutionDate.Day);
        if (!clients.Any())
        {
            _logger.LogInformation("No clients scheduled for execution today ({Day}).", request.ExecutionDate.Day);
            return;
        }

        _logger.LogInformation("Processing {Count} clients for day {Day}.", clients.Count, request.ExecutionDate.Day);

        // 2. Calculate Top 5
        var quotes = await _stockQuoteRepository.GetLatestQuotesAsync(request.ExecutionDate);
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

        var top5Tickers = string.Join(", ", top5Quotes.Select(q => q.Ticker));
        _logger.LogInformation("Top 5 stocks identified: {Tickers}", top5Tickers);

        // 3. Update Recommendation Basket
        var basketItems = top5Quotes.Select(q => new BasketItem(q.Ticker, 20m)).ToList();
        var newBasket = new RecommendationBasket($"Top 5 - {request.ExecutionDate:yyyy-MM-dd}", basketItems);
        var previousBasket = await _recommendationBasketRepository.GetActiveAsync();
        previousBasket?.Deactivate();
        await _recommendationBasketRepository.AddAsync(newBasket);
        _logger.LogInformation("Recommendation basket updated to active.");

        // 4. Consolidate Contributions (1/3 of monthly)
        decimal totalConsolidatedContribution = clients.Sum(c => c.MonthlyContribution / 3);
        decimal contributionPerStock = totalConsolidatedContribution / 5;
        _logger.LogInformation("Total consolidated contribution: {Total}. Contribution per stock: {PerStock}.", totalConsolidatedContribution, contributionPerStock);

        // 5. Identify Master Account (Assuming it's AccountId 1 for now)
        var masterAccountCustodies = await _custodyRepository.GetByAccountIdAsync(1);

        foreach (var quote in top5Quotes)
        {
            _logger.LogDebug("Processing stock {Ticker}. Consolidating buy and distribution.", quote.Ticker);

            // Total quantity needed for the day
            int totalQuantityNeeded = (int)(contributionPerStock / quote.ClosingPrice);
            if (totalQuantityNeeded <= 0) continue;

            // Check Master Custody
            var masterCustody = masterAccountCustodies.FirstOrDefault(c => c.Ticker == quote.Ticker);
            int masterBalance = masterCustody?.Quantity ?? 0;

            // Quantity to actually buy
            int quantityToBuy = Math.Max(0, totalQuantityNeeded - masterBalance);

            if (quantityToBuy > 0)
            {
                // Separate Standard (100x) and Fractional
                int standardQuantity = (quantityToBuy / 100) * 100;
                int fractionalQuantity = quantityToBuy % 100;

                if (standardQuantity > 0)
                {
                    await _purchaseOrderRepository.AddAsync(new PurchaseOrder(1, quote.Ticker, standardQuantity, quote.ClosingPrice, MarketType.Standard));
                }

                if (fractionalQuantity > 0)
                {
                    await _purchaseOrderRepository.AddAsync(new PurchaseOrder(1, quote.Ticker + "F", fractionalQuantity, quote.ClosingPrice, MarketType.Fractional));
                }
                
                _logger.LogInformation("Buy orders created for {Ticker}: {Total} ({Standard} Standard, {Fractional} Fractional).", quote.Ticker, quantityToBuy, standardQuantity, fractionalQuantity);
            }

            // 6. Distribute to Clients
            int totalAvailableForDistribution = masterBalance + quantityToBuy;
            int distributedCount = 0;

            foreach (var client in clients)
            {
                if (client.GraphicAccount == null) continue;

                // Client proportion = Client aporte / Total aporte
                decimal proportion = (client.MonthlyContribution / 3) / totalConsolidatedContribution;
                int clientQuantity = (int)(totalAvailableForDistribution * proportion);
                if (clientQuantity <= 0) continue;

                distributedCount += clientQuantity;

                // Update Client Custody
                var clientCustodies = await _custodyRepository.GetByAccountIdAsync(client.GraphicAccount.Id);
                var clientCustody = clientCustodies.FirstOrDefault(c => c.Ticker == quote.Ticker);

                if (clientCustody != null)
                {
                    clientCustody.UpdateAveragePrice(clientQuantity, quote.ClosingPrice);
                }
                else
                {
                    await _custodyRepository.AddAsync(new Custody(client.GraphicAccount.Id, quote.Ticker, clientQuantity, quote.ClosingPrice));
                }

                // Deduct from client's graphic account balance (which was initial aporte)
                client.GraphicAccount.SubtractBalance(clientQuantity * quote.ClosingPrice);
            }

            // 7. Update Master Custody with Residue
            int residue = totalAvailableForDistribution - distributedCount;
            if (masterCustody != null)
            {
                masterCustody.SubtractQuantity(masterBalance); // Remove old
                masterCustody.UpdateAveragePrice(residue, quote.ClosingPrice); // Add residue
            }
            else if (residue > 0)
            {
                await _custodyRepository.AddAsync(new Custody(1, quote.Ticker, residue, quote.ClosingPrice));
            }
            
            _logger.LogDebug("Distributed {Count} of {Ticker}. Residue on Master: {Residue}.", distributedCount, quote.Ticker, residue);
        }

        // 8. Rebalancing (Sell old tickers)
        _logger.LogInformation("Starting portfolio rebalancing for clients.");
        var currentTop5Tickers = top5Quotes.Select(q => q.Ticker).ToList();
        foreach (var client in clients)
        {
            if (client.GraphicAccount == null) continue;
            var clientCustodies = await _custodyRepository.GetByAccountIdAsync(client.GraphicAccount.Id);
            var toSell = clientCustodies.Where(c => !currentTop5Tickers.Contains(c.Ticker) && c.Quantity > 0).ToList();

            foreach (var custody in toSell)
            {
                var currentQuote = await _stockQuoteRepository.GetLatestByTickerAsync(custody.Ticker);
                if (currentQuote == null) continue;

                var proceeds = custody.Quantity * currentQuote.ClosingPrice;
                client.GraphicAccount.AddBalance(proceeds);

                await _purchaseOrderRepository.AddAsync(new PurchaseOrder(client.GraphicAccount.Id, custody.Ticker, -custody.Quantity, currentQuote.ClosingPrice, MarketType.Standard));
                custody.SubtractQuantity(custody.Quantity);
                
                _logger.LogInformation("Sold {Quantity} of {Ticker} for client {ClientId}. Proceeds: {Proceeds}.", custody.Quantity, custody.Ticker, client.Id, proceeds);
            }
        }

        // Final Save
        _logger.LogInformation("Finalizing changes. Saving to database.");
        await _purchaseOrderRepository.SaveChangesAsync();
        await _clientRepository.SaveChangesAsync();
        await _recommendationBasketRepository.SaveChangesAsync();
        await _custodyRepository.SaveChangesAsync();
        
        _logger.LogInformation("ExecutePurchaseMotorCommand completed successfully.");
    }
}

