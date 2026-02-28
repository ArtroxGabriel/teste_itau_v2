using ItauCompraProgramada.Application.Common.Interfaces;
using MediatR;
using System.Text.Json.Serialization;

namespace ItauCompraProgramada.Application.Clients.Commands.DeactivateClient;

public record DeactivateClientCommand(long ClientId, string CorrelationId) : IRequest<DeactivateClientResponse>, ICorrelatedRequest;

public record DeactivateClientResponse(
    [property: JsonPropertyName("clienteId")] long ClientId,
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("ativo")] bool Ativo,
    [property: JsonPropertyName("dataSaida")] DateTime DataSaida,
    [property: JsonPropertyName("mensagem")] string Mensagem
);