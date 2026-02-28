using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Interfaces;

using MediatR;

using Microsoft.Extensions.Logging;

namespace ItauCompraProgramada.Application.Admin.Queries.GetCurrentBasket;

public class GetCurrentBasketQueryHandler(
    IRecommendationBasketRepository basketRepository,
    IStockQuoteRepository stockQuoteRepository,
    ILogger<GetCurrentBasketQueryHandler> logger) : IRequestHandler<GetCurrentBasketQuery, CurrentBasketDto>
{
    public async Task<CurrentBasketDto> Handle(GetCurrentBasketQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("GetCurrentBasketQuery started.");

        var basket = await basketRepository.GetActiveAsync(cancellationToken);
        if (basket is null)
            throw new KeyNotFoundException("Nenhuma cesta ativa encontrada.");

        var items = await EnrichItemsWithQuotesAsync(basket.Items.ToList(), cancellationToken);

        logger.LogInformation("GetCurrentBasketQuery completed. BasketId: {Id}", basket.Id);

        return new CurrentBasketDto(
            CestaId: basket.Id,
            Nome: basket.Name,
            Ativa: basket.IsActive,
            DataCriacao: basket.CreatedAt,
            Itens: items);
    }

    private async Task<List<CurrentBasketItemDto>> EnrichItemsWithQuotesAsync(
        List<Domain.Entities.BasketItem> items,
        CancellationToken cancellationToken)
    {
        var result = new List<CurrentBasketItemDto>();

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var quote = await stockQuoteRepository.GetLatestByTickerAsync(item.Ticker);
            result.Add(new CurrentBasketItemDto(item.Ticker, item.Percentage, quote?.ClosingPrice));
        }

        return result;
    }
}
