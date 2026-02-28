using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace ItauCompraProgramada.Infrastructure.Persistence.Repositories;

public class CustodyRepository(ItauDbContext dbContext) : ICustodyRepository
{
    public async Task<List<Custody>> GetByAccountIdAsync(long accountId)
    {
        return await dbContext.Custodies
            .Where(c => c.AccountId == accountId)
            .ToListAsync();
    }

    public async Task AddAsync(Custody custody)
    {
        await dbContext.Custodies.AddAsync(custody);
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}