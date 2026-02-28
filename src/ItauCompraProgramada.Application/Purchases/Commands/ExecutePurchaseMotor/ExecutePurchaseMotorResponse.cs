using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ItauCompraProgramada.Application.Purchases.Commands.ExecutePurchaseMotor;

public record PurchaseOrderDto(
    [property: JsonPropertyName("ticker")] string Ticker,
    [property: JsonPropertyName("quantidade")] int Quantidade,
    [property: JsonPropertyName("preco")] decimal Preco,
    [property: JsonPropertyName("tipoMercado")] string TipoMercado
);

public record DistributionDto(
    [property: JsonPropertyName("clienteId")] long ClienteId,
    [property: JsonPropertyName("ticker")] string Ticker,
    [property: JsonPropertyName("quantidade")] int Quantidade,
    [property: JsonPropertyName("preco")] decimal Preco
);

public record MasterResidueDto(
    [property: JsonPropertyName("ticker")] string Ticker,
    [property: JsonPropertyName("quantidade")] int Quantidade
);

public record IREventDto(
    [property: JsonPropertyName("tipo")] string Tipo,
    [property: JsonPropertyName("clienteId")] long ClienteId,
    [property: JsonPropertyName("valor")] decimal Valor
);

public record ExecutePurchaseMotorResponse(
    [property: JsonPropertyName("mensagem")] string Mensagem,
    [property: JsonPropertyName("dataExecucao")] DateTime DataExecucao,
    [property: JsonPropertyName("ordensCompra")] List<PurchaseOrderDto> OrdensCompra,
    [property: JsonPropertyName("distribuicoes")] List<DistributionDto> Distribuicoes,
    [property: JsonPropertyName("residuosCustMaster")] List<MasterResidueDto> ResiduosCustMaster,
    [property: JsonPropertyName("eventosIRPublicados")] List<IREventDto> EventosIRPublicados
);
