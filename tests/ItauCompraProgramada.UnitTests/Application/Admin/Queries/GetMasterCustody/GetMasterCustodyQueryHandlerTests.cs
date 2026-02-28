using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using ItauCompraProgramada.Application.Admin.Queries.GetMasterCustody;
using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;

using Moq;

using Xunit;

namespace ItauCompraProgramada.UnitTests.Application.Admin.Queries.GetMasterCustody;

public class GetMasterCustodyQueryHandlerTests
{
    private readonly Mock<ICustodyRepository> _custodyRepositoryMock;
    private readonly GetMasterCustodyQueryHandler _handler;

    public GetMasterCustodyQueryHandlerTests()
    {
        _custodyRepositoryMock = new Mock<ICustodyRepository>();
        _handler = new GetMasterCustodyQueryHandler(_custodyRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnMasterAccountCustody_OrderedByTicker()
    {
        // Arrange
        var masterCustodies = new List<Custody>
        {
            new Custody(1, "VALE3", 0, 62m),
            new Custody(1, "PETR4", 1, 35m)
        };

        _custodyRepositoryMock.Setup(r => r.GetByAccountIdAsync(1))
            .ReturnsAsync(masterCustodies);

        var query = new GetMasterCustodyQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Custodias.Should().HaveCount(2);

        // Should be ordered by Ticker (PETR4, then VALE3)
        result.Custodias[0].Ticker.Should().Be("PETR4");
        result.Custodias[0].Quantidade.Should().Be(1);
        result.Custodias[0].PrecoMedio.Should().Be(35m);

        result.Custodias[1].Ticker.Should().Be("VALE3");
        result.Custodias[1].Quantidade.Should().Be(0);
        result.Custodias[1].PrecoMedio.Should().Be(62m);
    }

    [Fact]
    public async Task Handle_WhenNoCustody_ShouldReturnEmptyList()
    {
        // Arrange
        _custodyRepositoryMock.Setup(r => r.GetByAccountIdAsync(1))
            .ReturnsAsync(new List<Custody>());

        var query = new GetMasterCustodyQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Custodias.Should().BeEmpty();
    }
}
