using System.Collections.Generic;

namespace ItauCompraProgramada.Application.Purchases.Commands.ExecutePurchaseMotor;

public record ExecutePurchaseMotorResult(
    List<PurchaseOrderDto> OrdensCompra,
    List<DistributionDto> Distribuicoes,
    List<MasterResidueDto> ResiduosCustMaster,
    List<IREventDto> EventosIRPublicados);
