using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

namespace ItauCompraProgramada.Infrastructure.Persistence.Repositories;

public class EventLogRepository(ItauDbContext dbContext) : IEventLogRepository
{
    public async Task<StoredEvent?> GetByCorrelationIdAsync(string correlationId)
    {
        return await dbContext.Set<StoredEvent>()
            .FirstOrDefaultAsync(e => e.CorrelationId == correlationId);
    }

    public async Task SaveAsync(StoredEvent storedEvent)
    {
        await dbContext.Set<StoredEvent>().AddAsync(storedEvent);
        await dbContext.SaveChangesAsync();
    }
}