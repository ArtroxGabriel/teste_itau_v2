using System;
using System.Text.Json.Serialization;

namespace ItauCompraProgramada.Api.Models;

public record CreateClientRequest(
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("cpf")] string Cpf,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("valorMensal")] decimal ValorMensal
);

public record ContaGraficaResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("numeroConta")] string NumeroConta,
    [property: JsonPropertyName("tipo")] string Tipo,
    [property: JsonPropertyName("dataCriacao")] DateTime DataCriacao
);

public record CreateClientHttpResponse(
    [property: JsonPropertyName("clienteId")] long ClienteId,
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("cpf")] string Cpf,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("valorMensal")] decimal ValorMensal,
    [property: JsonPropertyName("ativo")] bool Ativo,
    [property: JsonPropertyName("dataAdesao")] DateTime DataAdesao,
    [property: JsonPropertyName("contaGrafica")] ContaGraficaResponse ContaGrafica
);
