using ItauCompraProgramada.Domain.Entities;
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
        var client = await GetClientOrThrowAsync(request.ClientId, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        var distributions = await distributionRepository.GetByClientIdAsync(request.ClientId);

        var aporteHistory = MapToAporteHistory(distributions);

        var walletEvolution = await ReconstructWalletEvolutionAsync(distributions, cancellationToken);

        var summary = CreatePerformanceSummary(walletEvolution);

        return new DetailedPerformanceDto(
            client.Id,
            client.Name,
            DateTime.UtcNow,
            summary,
            aporteHistory,
            walletEvolution
        );
    }

    private async Task<Client> GetClientOrThrowAsync(long clientId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var client = await clientRepository.GetByIdAsync(clientId);
        if (client == null)
        {
            throw new KeyNotFoundException($"Client with ID {clientId} not found.");
        }
        return client;
    }

    private static List<AporteHistoryDto> MapToAporteHistory(List<Distribution> distributions)
    {
        return distributions
            .GroupBy(d => d.DistributedAt.Date)
            .Select(g => new AporteHistoryDto(
                g.Key,
                g.Sum(d => d.Quantity * d.UnitPrice),
                "1/3"
            ))
            .OrderBy(h => h.Date)
            .ToList();
    }

    private async Task<List<WalletEvolutionDto>> ReconstructWalletEvolutionAsync(List<Distribution> distributions, CancellationToken cancellationToken)
    {
        var walletEvolution = new List<WalletEvolutionDto>();
        var runningCustody = new Dictionary<string, int>();
        decimal runningInvestedValue = 0m;

        foreach (var dateGroup in distributions.GroupBy(d => d.DistributedAt.Date).OrderBy(g => g.Key))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var date = dateGroup.Key;

            foreach (var dist in dateGroup)
            {
                UpdateRunningCustody(runningCustody, dist);
                runningInvestedValue += dist.Quantity * dist.UnitPrice;
            }

            var dailyValue = await CalculateMarketValueAtDateAsync(date, runningCustody, cancellationToken);

            decimal rentability = runningInvestedValue > 0
                ? ((dailyValue / runningInvestedValue) - 1) * 100
                : 0m;

            walletEvolution.Add(new WalletEvolutionDto(
                date,
                dailyValue,
                runningInvestedValue,
                rentability
            ));
        }

        return walletEvolution;
    }

    private static void UpdateRunningCustody(Dictionary<string, int> custody, Distribution dist)
    {
        if (!custody.ContainsKey(dist.Ticker))
            custody[dist.Ticker] = 0;

        custody[dist.Ticker] += dist.Quantity;
    }

    private async Task<decimal> CalculateMarketValueAtDateAsync(DateTime date, Dictionary<string, int> runningCustody, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var quotesAtDate = await stockQuoteRepository.GetLatestQuotesAsync(date);
        decimal marketValue = 0m;

        foreach (var item in runningCustody)
        {
            var quote = quotesAtDate.FirstOrDefault(q => q.Ticker == item.Key);
            marketValue += item.Value * (quote?.ClosingPrice ?? 0m);
        }

        return marketValue;
    }

    private static PerformanceSummaryDto CreatePerformanceSummary(List<WalletEvolutionDto> evolution)
    {
        var latest = evolution.LastOrDefault();
        return new PerformanceSummaryDto(
            latest?.InvestedValue ?? 0m,
            latest?.WalletValue ?? 0m,
            (latest?.WalletValue ?? 0m) - (latest?.InvestedValue ?? 0m),
            latest?.Rentability ?? 0m
        );
    }
}
