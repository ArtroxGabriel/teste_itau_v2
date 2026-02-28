using System.Collections.Generic;
using System.Text.Json.Serialization;

using ItauCompraProgramada.Application.Admin.Commands.CreateBasket;

namespace ItauCompraProgramada.Api.Models;

public record CreateBasketRequest(
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("itens")] List<BasketItemInput> Itens);
