using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ItauCompraProgramada.Api.Models;

public record MasterCustodyItemDto(
    [property: JsonPropertyName("ticker")] string Ticker,
    [property: JsonPropertyName("quantidade")] int Quantidade,
    [property: JsonPropertyName("precoMedio")] decimal PrecoMedio
);

public record MasterCustodyResponse(
    [property: JsonPropertyName("custodias")] List<MasterCustodyItemDto> Custodias
);
