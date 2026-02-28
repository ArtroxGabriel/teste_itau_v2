using System;
using System.Text.Json.Serialization;

namespace ItauCompraProgramada.Api.Models;

public record ExecutePurchaseMotorRequest(
    [property: JsonPropertyName("dataReferencia")] DateTime? DataReferencia
);
