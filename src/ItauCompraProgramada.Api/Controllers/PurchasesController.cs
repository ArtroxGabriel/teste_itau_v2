using System;
using System.Threading.Tasks;
using ItauCompraProgramada.Application.Purchases.Commands.ExecutePurchaseMotor;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ItauCompraProgramada.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchasesController(IMediator mediator) : ControllerBase
{
    [HttpPost("execute-motor")]
    public async Task<IActionResult> ExecuteMotor([FromQuery] DateTime? date, [FromQuery] string? correlationId)
    {
        var executionDate = date ?? DateTime.UtcNow;
        var cid = correlationId ?? $"Manual-PurchaseMotor-{executionDate:yyyy-MM-dd}";
        
        var command = new ExecutePurchaseMotorCommand(executionDate, cid);
        await mediator.Send(command);
        
        return Ok(new { Message = "Purchase Motor execution triggered.", ExecutionDate = executionDate, CorrelationId = cid });
    }
}
