using FluentAssertions;
using ItauCompraProgramada.Application.Clients.Commands.DeactivateClient;
using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;
using Moq;
using Xunit;

namespace ItauCompraProgramada.UnitTests.Application.Clients.Commands.DeactivateClient;

public class DeactivateClientCommandHandlerTests
{
    private readonly Mock<IClientRepository> _clientRepositoryMock;
    private readonly DeactivateClientCommandHandler _handler;

    public DeactivateClientCommandHandlerTests()
    {
        _clientRepositoryMock = new Mock<IClientRepository>();
        _handler = new DeactivateClientCommandHandler(_clientRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingActiveClient_ShouldDeactivateClient()
    {
        // Arrange
        var client = new Client("John", "12345678901", "john@email.com", 500m);
        var command = new DeactivateClientCommand(1, "corr-id-1");

        _clientRepositoryMock.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(client);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        client.IsActive.Should().BeFalse();
        result.Ativo.Should().BeFalse();
        _clientRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ClientNotFound_ShouldThrowException()
    {
        // Arrange
        var command = new DeactivateClientCommand(999, "corr-id-2");

        _clientRepositoryMock.Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Client)null!);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
