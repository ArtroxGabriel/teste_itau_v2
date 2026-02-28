using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Enums;
using ItauCompraProgramada.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ItauCompraProgramada.Infrastructure.Persistence.Repositories;

public class IREventRepository : IIREventRepository
{
    private readonly ItauDbContext _context;

    public IREventRepository(ItauDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(IREvent irEvent)
    {
        await _context.IREvents.AddAsync(irEvent);
    }

    public async Task<List<IREvent>> GetPendingPublicationAsync()
    {
        return await _context.IREvents
            .Where(e => !e.PublishedToKafka)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalIRValueInMonthAsync(long clientId, IREventType type, int year, int month)
    {
        return await _context.IREvents
            .Where(e => e.ClienteId == clientId && 
                        e.Type == type && 
                        e.EventDate.Year == year && 
                        e.EventDate.Month == month)
            .SumAsync(e => e.IRValue);
    }

    public async Task<decimal> GetTotalBaseValueInMonthAsync(long clientId, IREventType type, int year, int month)
    {
        return await _context.IREvents
            .Where(e => e.ClienteId == clientId && 
                        e.Type == type && 
                        e.EventDate.Year == year && 
                        e.EventDate.Month == month)
            .SumAsync(e => e.BaseValue);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
