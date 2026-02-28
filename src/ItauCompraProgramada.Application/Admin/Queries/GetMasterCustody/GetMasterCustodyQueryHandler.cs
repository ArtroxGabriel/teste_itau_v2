using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Interfaces;

using MediatR;

namespace ItauCompraProgramada.Application.Admin.Queries.GetMasterCustody;

public class GetMasterCustodyQueryHandler(ICustodyRepository custodyRepository) 
    : IRequestHandler<GetMasterCustodyQuery, GetMasterCustodyResult>
{
    public async Task<GetMasterCustodyResult> Handle(GetMasterCustodyQuery request, CancellationToken cancellationToken)
    {
        // AccountId = 1 is the Master Account
        var custodies = await custodyRepository.GetByAccountIdAsync(1);

        var items = custodies
            .OrderBy(c => c.Ticker)
            .Select(c => new MasterCustodyItem(c.Ticker, c.Quantity, c.AveragePrice))
            .ToList();

        return new GetMasterCustodyResult(items);
    }
}
