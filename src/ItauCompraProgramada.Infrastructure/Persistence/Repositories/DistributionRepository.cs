using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace ItauCompraProgramada.Infrastructure.Persistence.Repositories;

public class DistributionRepository(ItauDbContext dbContext) : IDistributionRepository
{
    public async Task<List<Distribution>> GetByClientIdAsync(long clientId)
    {
        return await dbContext.Distributions
            .Include(d => d.Custody)
            .ThenInclude(c => c!.Account)
            .Where(d => d.Custody!.Account!.ClienteId == clientId)
            .OrderBy(d => d.DistributedAt)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}
