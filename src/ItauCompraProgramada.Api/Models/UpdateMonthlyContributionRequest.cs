using System.Text.Json.Serialization;

namespace ItauCompraProgramada.Api.Models;

public record UpdateMonthlyContributionRequest(
    [property: JsonPropertyName("novoValorMensal")] decimal NovoValorMensal
);