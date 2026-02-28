using System.Collections.Generic;
using System.Text.Json.Serialization;

using MediatR;

namespace ItauCompraProgramada.Application.Admin.Commands.CreateBasket;

/// <summary>
/// Command to create (or replace) the active recommendation basket.
/// RN-014: exactly 5 items. RN-015: sum of percentages = 100%.
/// </summary>
public record CreateBasketCommand(
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("itens")] List<BasketItemInput> Itens,
    string CorrelationId) : IRequest<CreateBasketResponse>;

public record BasketItemInput(
    [property: JsonPropertyName("ticker")] string Ticker,
    [property: JsonPropertyName("percentual")] decimal Percentual);
