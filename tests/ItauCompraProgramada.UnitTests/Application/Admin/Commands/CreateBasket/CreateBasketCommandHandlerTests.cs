using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using ItauCompraProgramada.Application.Admin.Commands.CreateBasket;
using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;

using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace ItauCompraProgramada.UnitTests.Application.Admin.Commands.CreateBasket;

public class CreateBasketCommandHandlerTests
{
    private readonly Mock<IRecommendationBasketRepository> _basketRepositoryMock;
    private readonly Mock<IClientRepository> _clientRepositoryMock;
    private readonly Mock<ILogger<CreateBasketCommandHandler>> _loggerMock;
    private readonly CreateBasketCommandHandler _handler;

    public CreateBasketCommandHandlerTests()
    {
        _basketRepositoryMock = new Mock<IRecommendationBasketRepository>();
        _clientRepositoryMock = new Mock<IClientRepository>();
        _loggerMock = new Mock<ILogger<CreateBasketCommandHandler>>();

        _handler = new CreateBasketCommandHandler(
            _basketRepositoryMock.Object,
            _clientRepositoryMock.Object,
            _loggerMock.Object);
    }

    private static CreateBasketCommand BuildValidCommand(string name = "Top Five - Test") =>
        new CreateBasketCommand(
            Nome: name,
            Itens: new List<BasketItemInput>
            {
                new("PETR4", 30m),
                new("VALE3", 25m),
                new("ITUB4", 20m),
                new("BBDC4", 15m),
                new("WEGE3", 10m),
            },
            CorrelationId: "corr-1");

    [Fact]
    public async Task Handle_FirstBasket_ShouldCreateBasketWithoutRebalancing()
    {
        // Arrange
        _basketRepositoryMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecommendationBasket?)null);

        var command = BuildValidCommand("Top Five - Fevereiro 2026");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Nome.Should().Be("Top Five - Fevereiro 2026");
        result.Ativa.Should().BeTrue();
        result.RebalanceamentoDisparado.Should().BeFalse();
        result.CestaAnteriorDesativada.Should().BeNull();
        result.AtivosRemovidos.Should().BeNull();
        result.AtivosAdicionados.Should().BeNull();
        result.Mensagem.Should().Contain("Primeira cesta cadastrada");
        result.Itens.Should().HaveCount(5);

        _basketRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RecommendationBasket>(), It.IsAny<CancellationToken>()), Times.Once);
        _basketRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SubsequentBasket_ShouldDeactivatePreviousAndTriggerRebalancing()
    {
        // Arrange
        var previousItems = new List<BasketItem>
        {
            new BasketItem("PETR4", 30m), new BasketItem("VALE3", 25m),
            new BasketItem("ITUB4", 20m), new BasketItem("BBDC4", 15m),
            new BasketItem("WEGE3", 10m),
        };
        var previousBasket = new RecommendationBasket("Top Five - Fevereiro 2026", previousItems);

        _basketRepositoryMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousBasket);

        _clientRepositoryMock
            .Setup(r => r.GetActiveCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(150);

        // New basket removes BBDC4 + WEGE3 and adds ABEV3 + RENT3
        var command = new CreateBasketCommand(
            Nome: "Top Five - Marco 2026",
            Itens: new List<BasketItemInput>
            {
                new("PETR4", 25m),
                new("VALE3", 20m),
                new("ITUB4", 20m),
                new("ABEV3", 20m),
                new("RENT3", 15m),
            },
            CorrelationId: "corr-2");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        previousBasket.IsActive.Should().BeFalse();
        previousBasket.DeactivatedAt.Should().NotBeNull();

        result.RebalanceamentoDisparado.Should().BeTrue();
        result.CestaAnteriorDesativada.Should().NotBeNull();
        result.CestaAnteriorDesativada!.Nome.Should().Be("Top Five - Fevereiro 2026");

        result.AtivosRemovidos.Should().NotBeNull();
        result.AtivosRemovidos!.Should().Contain("BBDC4");
        result.AtivosRemovidos!.Should().Contain("WEGE3");

        result.AtivosAdicionados.Should().NotBeNull();
        result.AtivosAdicionados!.Should().Contain("ABEV3");
        result.AtivosAdicionados!.Should().Contain("RENT3");

        result.Mensagem.Should().Contain("150 clientes ativos");
    }

    [Fact]
    public async Task Handle_SubsequentBasketSameTickers_ShouldShowNoRemovedOrAdded()
    {
        // Arrange — same tickers, just different percentages
        var previousItems = new List<BasketItem>
        {
            new BasketItem("PETR4", 30m), new BasketItem("VALE3", 25m),
            new BasketItem("ITUB4", 20m), new BasketItem("BBDC4", 15m),
            new BasketItem("WEGE3", 10m),
        };
        var previousBasket = new RecommendationBasket("Old", previousItems);

        _basketRepositoryMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousBasket);

        _clientRepositoryMock
            .Setup(r => r.GetActiveCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        // Same 5 tickers, different percentages
        var command = new CreateBasketCommand(
            Nome: "Top Five - Updated",
            Itens: new List<BasketItemInput>
            {
                new("PETR4", 20m), new("VALE3", 20m), new("ITUB4", 20m),
                new("BBDC4", 20m), new("WEGE3", 20m),
            },
            CorrelationId: "corr-3");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.RebalanceamentoDisparado.Should().BeTrue();
        result.AtivosRemovidos.Should().BeEmpty();
        result.AtivosAdicionados.Should().BeEmpty();
    }
}
