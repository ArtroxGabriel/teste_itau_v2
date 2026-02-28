namespace ItauCompraProgramada.Application.Clients.Queries.GetClientPortfolio;

public record PortfolioDto(
    decimal TotalInvested,
    decimal TotalCurrentValue,
    decimal TotalProfitLoss,
    decimal RentabilityPercentage,
    List<PortfolioItemDto> Items);

public record PortfolioItemDto(
    string Ticker,
    int Quantity,
    decimal AveragePrice,
    decimal CurrentPrice,
    decimal CurrentValue,
    decimal ProfitLoss,
    decimal WeightPercentage);