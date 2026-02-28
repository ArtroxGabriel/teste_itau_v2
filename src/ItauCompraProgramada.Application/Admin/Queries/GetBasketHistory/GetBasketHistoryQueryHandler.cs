using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Interfaces;

using MediatR;

using Microsoft.Extensions.Logging;

namespace ItauCompraProgramada.Application.Admin.Queries.GetBasketHistory;

public class GetBasketHistoryQueryHandler(
    IRecommendationBasketRepository basketRepository,
    ILogger<GetBasketHistoryQueryHandler> logger) : IRequestHandler<GetBasketHistoryQuery, BasketHistoryDto>
{
    public async Task<BasketHistoryDto> Handle(GetBasketHistoryQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("GetBasketHistoryQuery started.");

        var baskets = await basketRepository.GetAllAsync(cancellationToken);

        var cestas = baskets.Select(b => new BasketHistoryItemDto(
            CestaId: b.Id,
            Nome: b.Name,
            Ativa: b.IsActive,
            DataCriacao: b.CreatedAt,
            DataDesativacao: b.DeactivatedAt,
            Itens: b.Items.Select(i => new BasketHistoryItemEntryDto(i.Ticker, i.Percentage)).ToList()
        )).ToList();

        logger.LogInformation("GetBasketHistoryQuery completed. Total baskets: {Count}", cestas.Count);

        return new BasketHistoryDto(cestas);
    }
}
