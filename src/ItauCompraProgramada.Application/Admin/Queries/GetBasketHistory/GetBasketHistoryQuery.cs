using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using MediatR;

namespace ItauCompraProgramada.Application.Admin.Queries.GetBasketHistory;

/// <summary>
/// Returns the full history of all baskets (active and deactivated), ordered newest first.
/// </summary>
public record GetBasketHistoryQuery() : IRequest<BasketHistoryDto>;

public record BasketHistoryDto(
    [property: JsonPropertyName("cestas")] List<BasketHistoryItemDto> Cestas);

public record BasketHistoryItemDto(
    [property: JsonPropertyName("cestaId")] long CestaId,
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("ativa")] bool Ativa,
    [property: JsonPropertyName("dataCriacao")] DateTime DataCriacao,
    [property: JsonPropertyName("dataDesativacao")] DateTime? DataDesativacao,
    [property: JsonPropertyName("itens")] List<BasketHistoryItemEntryDto> Itens);

public record BasketHistoryItemEntryDto(
    [property: JsonPropertyName("ticker")] string Ticker,
    [property: JsonPropertyName("percentual")] decimal Percentual);
