using ItauCompraProgramada.Domain.Interfaces;
using MediatR;

namespace ItauCompraProgramada.Application.Clients.Queries.GetDetailedPerformance;

public class GetDetailedPerformanceQueryHandler(
    IClientRepository clientRepository,
    IDistributionRepository distributionRepository,
    IStockQuoteRepository stockQuoteRepository) : IRequestHandler<GetDetailedPerformanceQuery, DetailedPerformanceDto>
{
    public async Task<DetailedPerformanceDto> Handle(GetDetailedPerformanceQuery request, CancellationToken cancellationToken)
    {
        var client = await clientRepository.GetByIdAsync(request.ClientId);
        if (client == null)
        {
            throw new KeyNotFoundException($"Client with ID {request.ClientId} not found.");
        }

        var distributions = await distributionRepository.GetByClientIdAsync(request.ClientId);
        
        // 1. Aporte History
        var aporteHistory = distributions
            .GroupBy(d => d.DistributedAt.Date)
            .Select(g => new AporteHistoryDto(
                g.Key,
                g.Sum(d => d.Quantity * d.UnitPrice),
                "1/3" // Simplified as the motor currently does 1/3
            ))
            .OrderBy(h => h.Date)
            .ToList();

        // 2. Wallet Evolution
        var walletEvolution = new List<WalletEvolutionDto>();
        var runningCustody = new Dictionary<string, int>();
        decimal runningInvestedValue = 0m;

        foreach (var dateGroup in distributions.GroupBy(d => d.DistributedAt.Date).OrderBy(g => g.Key))
        {
            var date = dateGroup.Key;
            
            foreach (var dist in dateGroup)
            {
                if (!runningCustody.ContainsKey(dist.Ticker))
                    runningCustody[dist.Ticker] = 0;
                
                runningCustody[dist.Ticker] += dist.Quantity;
                runningInvestedValue += dist.Quantity * dist.UnitPrice;
            }

            // Calculate market value at this date
            var quotesAtDate = await stockQuoteRepository.GetLatestQuotesAsync(date);
            decimal marketValueAtDate = 0m;

            foreach (var item in runningCustody)
            {
                var quote = quotesAtDate.FirstOrDefault(q => q.Ticker == item.Key);
                marketValueAtDate += item.Value * (quote?.ClosingPrice ?? 0m);
            }

            decimal rentability = runningInvestedValue > 0 
                ? ((marketValueAtDate / runningInvestedValue) - 1) * 100 
                : 0m;

            walletEvolution.Add(new WalletEvolutionDto(
                date,
                marketValueAtDate,
                runningInvestedValue,
                rentability
            ));
        }

        // Summary (Current state)
        var latestEvolution = walletEvolution.LastOrDefault();
        var summary = new PerformanceSummaryDto(
            latestEvolution?.InvestedValue ?? 0m,
            latestEvolution?.WalletValue ?? 0m,
            (latestEvolution?.WalletValue ?? 0m) - (latestEvolution?.InvestedValue ?? 0m),
            latestEvolution?.Rentability ?? 0m
        );

        return new DetailedPerformanceDto(
            client.Id,
            client.Name,
            DateTime.UtcNow,
            summary,
            aporteHistory,
            walletEvolution
        );
    }
}
