using ItauCompraProgramada.Application.Common.Interfaces;
using MediatR;
using System.Text.Json.Serialization;

namespace ItauCompraProgramada.Application.Clients.Commands.UpdateClientContribution;

public record UpdateClientContributionCommand(long ClientId, decimal NewMonthlyContribution, string CorrelationId) : IRequest<UpdateClientContributionResponse>, ICorrelatedRequest;

public record UpdateClientContributionResponse(
    [property: JsonPropertyName("clienteId")] long ClientId,
    [property: JsonPropertyName("valorMensalAnterior")] decimal OldMonthlyContribution,
    [property: JsonPropertyName("valorMensalNovo")] decimal NewMonthlyContribution,
    [property: JsonPropertyName("dataAlteracao")] DateTime AlterationDate,
    [property: JsonPropertyName("mensagem")] string Message
);