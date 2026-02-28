using ItauCompraProgramada.Application.Clients.Commands.CreateClient;
using ItauCompraProgramada.Application.Clients.Queries.GetClientWallet;
using ItauCompraProgramada.Application.Clients.Queries.GetDetailedPerformance;


using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace ItauCompraProgramada.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController(IMediator mediator) : ControllerBase
{
    [HttpPost("adhesion")]
    public async Task<ActionResult<CreateClientResponse>> Adhesion(CreateClientCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(result);
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