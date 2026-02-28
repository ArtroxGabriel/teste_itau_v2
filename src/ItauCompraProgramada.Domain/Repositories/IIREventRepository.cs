using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Enums;

namespace ItauCompraProgramada.Domain.Repositories;

public interface IIREventRepository
{
    Task AddAsync(IREvent irEvent);
    Task<List<IREvent>> GetPendingPublicationAsync();
    Task<decimal> GetTotalIRValueInMonthAsync(long clientId, IREventType type, int year, int month);
    Task<decimal> GetTotalBaseValueInMonthAsync(long clientId, IREventType type, int year, int month);
    Task SaveChangesAsync();
}