using System.Text.Json.Serialization;
using MediatR;

namespace ItauCompraProgramada.Application.Clients.Queries.GetClientWallet;

public record GetClientWalletQuery(long ClientId) : IRequest<ClientWalletDto>;

public record ClientWalletDto(
    [property: JsonPropertyName("clienteId")] long ClientId,
    [property: JsonPropertyName("nome")] string Name,
    [property: JsonPropertyName("contaGrafica")] string AccountNumber,
    [property: JsonPropertyName("dataConsulta")] DateTime CalculationDate,
    [property: JsonPropertyName("resumo")] WalletSummaryDto Summary,
    [property: JsonPropertyName("ativos")] List<WalletAssetDto> Assets);

public record WalletSummaryDto(
    [property: JsonPropertyName("valorTotalInvestido")] decimal TotalInvestedValue,
    [property: JsonPropertyName("valorAtualCarteira")] decimal TotalCurrentValue,
    [property: JsonPropertyName("plTotal")] decimal TotalProfitLoss,
    [property: JsonPropertyName("rentabilidadePercentual")] decimal ProfitLossPercentage);

public record WalletAssetDto(
    [property: JsonPropertyName("ticker")] string Ticker,
    [property: JsonPropertyName("quantidade")] int Quantity,
    [property: JsonPropertyName("precoMedio")] decimal AveragePrice,
    [property: JsonPropertyName("cotacaoAtual")] decimal CurrentPrice,
    [property: JsonPropertyName("valorAtual")] decimal CurrentValue,
    [property: JsonPropertyName("pl")] decimal ProfitLoss,
    [property: JsonPropertyName("plPercentual")] decimal ProfitLossPercentage,
    [property: JsonPropertyName("composicaoCarteira")] decimal PortfolioWeight);
