using ItauCompraProgramada.Api.Models;
using ItauCompraProgramada.Application.Admin.Commands.CreateBasket;
using ItauCompraProgramada.Application.Admin.Queries.GetBasketHistory;
using ItauCompraProgramada.Application.Admin.Queries.GetCurrentBasket;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace ItauCompraProgramada.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates or replaces the active recommendation basket (Top Five).
    /// RN-014: must have exactly 5 assets. RN-015: percentages must sum to 100%.
    /// </summary>
    [HttpPost("cesta")]
    public async Task<ActionResult<CreateBasketResponse>> CreateBasket(
        [FromBody] CreateBasketRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
        CancellationToken cancellationToken)
    {
        var cid = correlationId ?? Guid.NewGuid().ToString();
        var command = new CreateBasketCommand(request.Nome, request.Itens, cid);
        var result = await mediator.Send(command, cancellationToken);
        return StatusCode(201, result);
    }

    /// <summary>
    /// Returns the currently active basket enriched with live stock quotes.
    /// </summary>
    [HttpGet("cesta/atual")]
    public async Task<ActionResult<CurrentBasketDto>> GetCurrentBasket(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCurrentBasketQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns the full basket history, ordered newest first.
    /// </summary>
    [HttpGet("cesta/historico")]
    public async Task<ActionResult<BasketHistoryDto>> GetBasketHistory(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBasketHistoryQuery(), cancellationToken);
        return Ok(result);
    }
}
