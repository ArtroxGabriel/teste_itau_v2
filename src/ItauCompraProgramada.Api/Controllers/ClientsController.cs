using ItauCompraProgramada.Api.Models;
using ItauCompraProgramada.Application.Clients.Commands.CreateClient;
using ItauCompraProgramada.Application.Clients.Queries.GetClientWallet;
using ItauCompraProgramada.Application.Clients.Queries.GetDetailedPerformance;


using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace ItauCompraProgramada.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClientsController(IMediator mediator) : ControllerBase
{
    [HttpPost("adesao")]
    public async Task<ActionResult<CreateClientHttpResponse>> Adhesion([FromBody] CreateClientRequest request, [FromHeader(Name = "X-Correlation-Id")] string? correlationId)
    {
        var cid = correlationId ?? Guid.NewGuid().ToString();
        var command = new CreateClientCommand(request.Nome, request.Cpf, request.Email, request.ValorMensal, cid);
        
        var result = await mediator.Send(command);
        
        var response = new CreateClientHttpResponse(
            result.Id,
            result.Name,
            result.Cpf,
            result.Email,
            result.MonthlyContribution,
            result.IsActive,
            result.JoinedAt,
            new ContaGraficaResponse(
                result.GraphicAccountId,
                result.GraphicAccountNumber,
                result.GraphicAccountType.ToUpper(),
                result.GraphicAccountCreatedAt
            )
        );

        return CreatedAtAction(nameof(GetWallet), new { clienteId = result.Id }, response);
    }

    [HttpGet("{clienteId}/carteira")]
    public async Task<ActionResult<ClientWalletDto>> GetWallet(long clienteId)
    {
        var result = await mediator.Send(new GetClientWalletQuery(clienteId));
        return Ok(result);
    }

    [HttpGet("{clienteId}/rentabilidade")]
    public async Task<ActionResult<DetailedPerformanceDto>> GetPerformance(long clienteId)
    {
        var result = await mediator.Send(new GetDetailedPerformanceQuery(clienteId));
        return Ok(result);
    }
}