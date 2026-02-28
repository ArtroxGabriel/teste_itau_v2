using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using MediatR;

namespace ItauCompraProgramada.Application.Admin.Queries.GetCurrentBasket;

/// <summary>
/// Returns the currently active recommendation basket with live stock quotes.
/// </summary>
public record GetCurrentBasketQuery() : IRequest<CurrentBasketDto>;

public record CurrentBasketDto(
    [property: JsonPropertyName("cestaId")] long CestaId,
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("ativa")] bool Ativa,
    [property: JsonPropertyName("dataCriacao")] DateTime DataCriacao,
    [property: JsonPropertyName("itens")] List<CurrentBasketItemDto> Itens);

public record CurrentBasketItemDto(
    [property: JsonPropertyName("ticker")] string Ticker,
    [property: JsonPropertyName("percentual")] decimal Percentual,
    [property: JsonPropertyName("cotacaoAtual")] decimal? CotacaoAtual);
