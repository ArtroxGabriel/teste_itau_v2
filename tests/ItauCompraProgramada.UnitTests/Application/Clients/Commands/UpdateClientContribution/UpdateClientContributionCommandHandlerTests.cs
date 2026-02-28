using FluentAssertions;
using ItauCompraProgramada.Application.Clients.Commands.UpdateClientContribution;
using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;
using Moq;
using Xunit;

namespace ItauCompraProgramada.UnitTests.Application.Clients.Commands.UpdateClientContribution;

public class UpdateClientContributionCommandHandlerTests
{
    private readonly Mock<IClientRepository> _clientRepositoryMock;
    private readonly UpdateClientContributionCommandHandler _handler;

    public UpdateClientContributionCommandHandlerTests()
    {
        _clientRepositoryMock = new Mock<IClientRepository>();
        _handler = new UpdateClientContributionCommandHandler(_clientRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidAmount_ShouldUpdateContribution()
    {
        // Arrange
        var client = new Client("John", "12345678901", "john@email.com", 500m);
        var command = new UpdateClientContributionCommand(1, 1500m, "corr-id");

        _clientRepositoryMock.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(client);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        client.MonthlyContribution.Should().Be(1500m);
        client.ContributionHistory.Should().ContainSingle();
        client.ContributionHistory.First().OldValue.Should().Be(500m);
        client.ContributionHistory.First().NewValue.Should().Be(1500m);
        
        result.OldMonthlyContribution.Should().Be(500m);
        result.NewMonthlyContribution.Should().Be(1500m);
        _clientRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_AmountUnder100_ShouldThrowArgumentException()
    {
        // Arrange
        var client = new Client("John", "12345678901", "john@email.com", 500m);
        var command = new UpdateClientContributionCommand(1, 99m, "corr-id");

        _clientRepositoryMock.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(client);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
