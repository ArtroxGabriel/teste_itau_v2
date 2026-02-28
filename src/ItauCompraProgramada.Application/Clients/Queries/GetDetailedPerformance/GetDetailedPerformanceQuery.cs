using System.Text.Json.Serialization;
using MediatR;

namespace ItauCompraProgramada.Application.Clients.Queries.GetDetailedPerformance;

public record GetDetailedPerformanceQuery(long ClientId) : IRequest<DetailedPerformanceDto>;

public record DetailedPerformanceDto(
    [property: JsonPropertyName("clienteId")] long ClientId,
    [property: JsonPropertyName("nome")] string Name,
    [property: JsonPropertyName("dataConsulta")] DateTime CalculationDate,
    [property: JsonPropertyName("rentabilidade")] PerformanceSummaryDto Summary,
    [property: JsonPropertyName("historicoAportes")] List<AporteHistoryDto> AporteHistory,
    [property: JsonPropertyName("evolucaoCarteira")] List<WalletEvolutionDto> WalletEvolution);

public record PerformanceSummaryDto(
    [property: JsonPropertyName("valorTotalInvestido")] decimal TotalInvestedValue,
    [property: JsonPropertyName("valorAtualCarteira")] decimal TotalCurrentValue,
    [property: JsonPropertyName("plTotal")] decimal TotalProfitLoss,
    [property: JsonPropertyName("rentabilidadePercentual")] decimal ProfitLossPercentage);

public record AporteHistoryDto(
    [property: JsonPropertyName("data")] DateTime Date,
    [property: JsonPropertyName("valor")] decimal Value,
    [property: JsonPropertyName("parcela")] string Installment); // e.g., "1/3"

public record WalletEvolutionDto(
    [property: JsonPropertyName("data")] DateTime Date,
    [property: JsonPropertyName("valorCarteira")] decimal WalletValue,
    [property: JsonPropertyName("valorInvestido")] decimal InvestedValue,
    [property: JsonPropertyName("rentabilidade")] decimal Rentability);
