using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;

using MediatR;

using Microsoft.Extensions.Logging;

namespace ItauCompraProgramada.Application.Admin.Commands.CreateBasket;

/// <summary>
/// Creates (or replaces) the active recommendation basket.
/// When a previous basket exists it is deactivated (RN-017/RN-018) and rebalancing
/// information is included in the response so downstream consumers can act (RN-019).
/// </summary>
public class CreateBasketCommandHandler(
    IRecommendationBasketRepository basketRepository,
    IClientRepository clientRepository,
    ILogger<CreateBasketCommandHandler> logger) : IRequestHandler<CreateBasketCommand, CreateBasketResponse>
{
    public async Task<CreateBasketResponse> Handle(CreateBasketCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "CreateBasketCommand started. BasketName: {Name}, CorrelationId: {CorrelationId}",
            request.Nome, request.CorrelationId);

        var previousBasket = await basketRepository.GetActiveAsync(cancellationToken);
        var isFirstBasket = previousBasket is null;

        var (removedTickers, addedTickers) = CalculateBasketDiff(previousBasket, request.Itens);

        DeactivatedBasketInfo? deactivatedInfo = null;
        if (previousBasket is not null)
        {
            previousBasket.Deactivate();
            deactivatedInfo = new DeactivatedBasketInfo(
                previousBasket.Id,
                previousBasket.Name,
                previousBasket.DeactivatedAt!.Value);

            logger.LogInformation(
                "Previous basket deactivated. Id: {Id}, Name: {Name}",
                previousBasket.Id, previousBasket.Name);
        }

        var newBasket = BuildBasket(request);
        await basketRepository.AddAsync(newBasket, cancellationToken);
        await basketRepository.SaveChangesAsync(cancellationToken);

        var rebalanceTriggered = !isFirstBasket;
        var activeClientCount = rebalanceTriggered
            ? await clientRepository.GetActiveCountAsync(cancellationToken)
            : 0;

        var mensagem = BuildMessage(isFirstBasket, rebalanceTriggered, activeClientCount);

        logger.LogInformation(
            "CreateBasketCommand completed. NewBasketId: {Id}, RebalanceTriggered: {Rebalance}, ActiveClients: {Clients}",
            newBasket.Id, rebalanceTriggered, activeClientCount);

        return new CreateBasketResponse(
            CestaId: newBasket.Id,
            Nome: newBasket.Name,
            Ativa: newBasket.IsActive,
            DataCriacao: newBasket.CreatedAt,
            Itens: request.Itens.Select(i => new BasketItemResponse(i.Ticker, i.Percentual)).ToList(),
            RebalanceamentoDisparado: rebalanceTriggered,
            CestaAnteriorDesativada: deactivatedInfo,
            AtivosRemovidos: rebalanceTriggered ? removedTickers : null,
            AtivosAdicionados: rebalanceTriggered ? addedTickers : null,
            Mensagem: mensagem);
    }

    private static RecommendationBasket BuildBasket(CreateBasketCommand request)
    {
        var items = request.Itens
            .Select(i => new BasketItem(i.Ticker, i.Percentual))
            .ToList();

        return new RecommendationBasket(request.Nome, items);
    }

    private static (List<string> Removed, List<string> Added) CalculateBasketDiff(
        RecommendationBasket? previous,
        List<BasketItemInput> newItems)
    {
        if (previous is null) return ([], []);

        var previousTickers = previous.Items.Select(i => i.Ticker).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newTickers = newItems.Select(i => i.Ticker).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var removed = previousTickers.Except(newTickers).ToList();
        var added = newTickers.Except(previousTickers).ToList();

        return (removed, added);
    }

    private static string BuildMessage(bool isFirst, bool rebalanceTriggered, int activeClientCount)
    {
        if (isFirst) return "Primeira cesta cadastrada com sucesso.";
        return $"Cesta atualizada. Rebalanceamento disparado para {activeClientCount} clientes ativos.";
    }
}
