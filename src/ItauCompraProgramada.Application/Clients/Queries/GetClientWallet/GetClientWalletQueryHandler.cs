using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;
using MediatR;

namespace ItauCompraProgramada.Application.Clients.Queries.GetClientWallet;

public class GetClientWalletQueryHandler(
    IClientRepository clientRepository,
    ICustodyRepository custodyRepository,
    IStockQuoteRepository stockQuoteRepository) : IRequestHandler<GetClientWalletQuery, ClientWalletDto>
{
    public async Task<ClientWalletDto> Handle(GetClientWalletQuery request, CancellationToken cancellationToken)
    {
        var (client, account) = await GetClientWithGraphicAccountAsync(request.ClientId, cancellationToken);
        
        var rawAssets = await FetchAssetsWithMarketDataAsync(account.Id, cancellationToken);
        
        var summary = CreateWalletSummary(rawAssets);
        
        var finalAssets = EnrichAssetsWithWeights(rawAssets, summary.TotalCurrentValue);

        return new ClientWalletDto(
            client.Id,
            client.Name,
            account.AccountNumber,
            DateTime.UtcNow,
            summary,
            finalAssets
        );
    }

    private async Task<(Client Client, GraphicAccount GraphicAccount)> GetClientWithGraphicAccountAsync(long clientId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var client = await clientRepository.GetByIdAsync(clientId);
        if (client == null || client.GraphicAccount == null)
        {
            throw new KeyNotFoundException($"Client with ID {clientId} not found or has no graphic account.");
        }
        return (client, client.GraphicAccount);
    }

    private async Task<List<WalletAssetDto>> FetchAssetsWithMarketDataAsync(long accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var custodies = await custodyRepository.GetByAccountIdAsync(accountId);
        var assets = new List<WalletAssetDto>();

        foreach (var custody in custodies.Where(c => c.Quantity > 0))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var quote = await stockQuoteRepository.GetLatestByTickerAsync(custody.Ticker);
            assets.Add(CalculateAssetMetrics(custody, quote));
        }

        return assets;
    }

    private static WalletAssetDto CalculateAssetMetrics(Custody custody, StockQuote? quote)
    {
        var currentPrice = quote?.ClosingPrice ?? 0m;
        var currentValue = currentPrice * custody.Quantity;
        var investedValue = custody.AveragePrice * custody.Quantity;
        var pl = currentValue - investedValue;
        var plPercentage = investedValue > 0 ? (pl / investedValue) * 100 : 0m;

        return new WalletAssetDto(
            custody.Ticker,
            custody.Quantity,
            custody.AveragePrice,
            currentPrice,
            currentValue,
            pl,
            plPercentage,
            0m
        );
    }

    private static WalletSummaryDto CreateWalletSummary(List<WalletAssetDto> assets)
    {
        var totalCurrentValue = assets.Sum(a => a.CurrentValue);
        var totalInvestedValue = assets.Sum(a => a.Quantity * a.AveragePrice);
        var totalPL = totalCurrentValue - totalInvestedValue;
        var totalPLPercentage = totalInvestedValue > 0 ? (totalPL / totalInvestedValue) * 100 : 0m;

        return new WalletSummaryDto(totalInvestedValue, totalCurrentValue, totalPL, totalPLPercentage);
    }

    private static List<WalletAssetDto> EnrichAssetsWithWeights(List<WalletAssetDto> assets, decimal totalValue)
    {
        return assets.Select(a => a with
        {
            PortfolioWeight = totalValue > 0 ? (a.CurrentValue / totalValue) * 100 : 0
        }).ToList();
    }
}
