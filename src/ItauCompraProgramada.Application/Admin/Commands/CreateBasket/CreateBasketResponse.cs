using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ItauCompraProgramada.Application.Admin.Commands.CreateBasket;

public record CreateBasketResponse(
    [property: JsonPropertyName("cestaId")] long CestaId,
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("ativa")] bool Ativa,
    [property: JsonPropertyName("dataCriacao")] DateTime DataCriacao,
    [property: JsonPropertyName("itens")] List<BasketItemResponse> Itens,
    [property: JsonPropertyName("rebalanceamentoDisparado")] bool RebalanceamentoDisparado,
    [property: JsonPropertyName("cestaAnteriorDesativada")] DeactivatedBasketInfo? CestaAnteriorDesativada,
    [property: JsonPropertyName("ativosRemovidos")] List<string>? AtivosRemovidos,
    [property: JsonPropertyName("ativosAdicionados")] List<string>? AtivosAdicionados,
    [property: JsonPropertyName("mensagem")] string Mensagem);

public record BasketItemResponse(
    [property: JsonPropertyName("ticker")] string Ticker,
    [property: JsonPropertyName("percentual")] decimal Percentual);

public record DeactivatedBasketInfo(
    [property: JsonPropertyName("cestaId")] long CestaId,
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("dataDesativacao")] DateTime DataDesativacao);
