using MediatR;

namespace ItauCompraProgramada.Application.Clients.Queries.GetDetailedPerformance;

public record GetDetailedPerformanceQuery(long ClientId) : IRequest<DetailedPerformanceDto>;

public record DetailedPerformanceDto(
    long ClientId,
    string Name,
    DateTime CalculationDate,
    PerformanceSummaryDto Summary,
    List<AporteHistoryDto> AporteHistory,
    List<WalletEvolutionDto> WalletEvolution);

public record PerformanceSummaryDto(
    decimal TotalInvestedValue,
    decimal TotalCurrentValue,
    decimal TotalProfitLoss,
    decimal ProfitLossPercentage);

public record AporteHistoryDto(
    DateTime Date,
    decimal Value,
    string Installment); // e.g., "1/3"

public record WalletEvolutionDto(
    DateTime Date,
    decimal WalletValue,
    decimal InvestedValue,
    decimal Rentability);
