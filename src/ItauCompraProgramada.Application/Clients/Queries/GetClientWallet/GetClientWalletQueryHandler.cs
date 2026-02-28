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
        var client = await clientRepository.GetByIdAsync(request.ClientId);
        if (client == null || client.GraphicAccount == null)
        {
            throw new KeyNotFoundException($"Client with ID {request.ClientId} not found or has no graphic account.");
        }

        var custodies = await custodyRepository.GetByAccountIdAsync(client.GraphicAccount.Id);
        var assets = new List<WalletAssetDto>();

        foreach (var custody in custodies.Where(c => c.Quantity > 0))
        {
            var quote = await stockQuoteRepository.GetLatestByTickerAsync(custody.Ticker);
            var currentPrice = quote?.ClosingPrice ?? 0m;
            var currentValue = currentPrice * custody.Quantity;
            var investedValue = custody.AveragePrice * custody.Quantity;
            var pl = currentValue - investedValue;
            var plPercentage = investedValue > 0 ? (pl / investedValue) * 100 : 0m;

            assets.Add(new WalletAssetDto(
                custody.Ticker,
                custody.Quantity,
                custody.AveragePrice,
                currentPrice,
                currentValue,
                pl,
                plPercentage,
                0m // Will calculate weight later
            ));
        }

        var totalCurrentValue = assets.Sum(a => a.CurrentValue);
        var totalInvestedValue = assets.Sum(a => a.Quantity * a.AveragePrice);
        var totalPL = totalCurrentValue - totalInvestedValue;
        var totalPLPercentage = totalInvestedValue > 0 ? (totalPL / totalInvestedValue) * 100 : 0m;

        // Update weights
        var finalAssets = assets.Select(a => a with 
        { 
            PortfolioWeight = totalCurrentValue > 0 ? (a.CurrentValue / totalCurrentValue) * 100 : 0 
        }).ToList();

        return new ClientWalletDto(
            client.Id,
            client.Name,
            client.GraphicAccount.AccountNumber,
            DateTime.UtcNow,
            new WalletSummaryDto(totalInvestedValue, totalCurrentValue, totalPL, totalPLPercentage),
            finalAssets
        );
    }
}
