using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using ItauCompraProgramada.Api.Models;
using ItauCompraProgramada.Application.Purchases.Commands.ExecutePurchaseMotor;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace ItauCompraProgramada.Api.Controllers;

[ApiController]
[Route("api/motor")]
public class PurchasesController(IMediator mediator) : ControllerBase
{
    [HttpPost("executar-compra")]
    public async Task<IActionResult> ExecuteMotor([FromBody] ExecutePurchaseMotorRequest request, [FromHeader(Name = "X-Correlation-Id")] string? correlationId)
    {
        var executionDate = request.DataReferencia ?? DateTime.UtcNow;
        var cid = correlationId ?? $"Manual-PurchaseMotor-{executionDate:yyyy-MM-dd}";

        var command = new ExecutePurchaseMotorCommand(executionDate, cid);
        await mediator.Send(command);

        // Retornar um modelo basico por enquanto. O contrato real e extenso e exige retornar distribuicoes.
        return Ok(new { 
            mensagem = "Compra programada executada com sucesso.", 
            dataExecucao = executionDate 
        });
    }
}