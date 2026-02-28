using ItauCompraProgramada.Application.Clients.Commands.CreateClient;

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
}