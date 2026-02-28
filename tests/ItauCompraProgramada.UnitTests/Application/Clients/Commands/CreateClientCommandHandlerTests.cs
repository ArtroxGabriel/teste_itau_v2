using FluentAssertions;

using ItauCompraProgramada.Application.Clients.Commands.CreateClient;
using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;

using Moq;

using Xunit;

namespace ItauCompraProgramada.UnitTests.Application.Clients.Commands;

public class CreateClientCommandHandlerTests
{
    private readonly Mock<IClientRepository> _clientRepositoryMock;
    private readonly CreateClientCommandHandler _handler;

    public CreateClientCommandHandlerTests()
    {
        _clientRepositoryMock = new Mock<IClientRepository>();
        _handler = new CreateClientCommandHandler(_clientRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreateClient()
    {
        // Arrange
        var command = new CreateClientCommand(
            "Test Client",
            "123.456.789-00",
            "test@itau.com.br",
            500m,
            "correlation-123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        _clientRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Client>()), Times.Once);
        _clientRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}