using MediatR;

namespace ItauCompraProgramada.Application.Clients.Queries.GetClientWallet;

public record GetClientWalletQuery(long ClientId) : IRequest<ClientWalletDto>;

public record ClientWalletDto(
    long ClientId,
    string Name,
    string AccountNumber,
    DateTime CalculationDate,
    WalletSummaryDto Summary,
    List<WalletAssetDto> Assets);

public record WalletSummaryDto(
    decimal TotalInvestedValue,
    decimal TotalCurrentValue,
    decimal TotalProfitLoss,
    decimal ProfitLossPercentage);

public record WalletAssetDto(
    string Ticker,
    int Quantity,
    decimal AveragePrice,
    decimal CurrentPrice,
    decimal CurrentValue,
    decimal ProfitLoss,
    decimal ProfitLossPercentage,
    decimal PortfolioWeight);
