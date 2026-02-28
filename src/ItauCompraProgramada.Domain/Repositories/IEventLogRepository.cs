using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Entities;

namespace ItauCompraProgramada.Domain.Repositories;

public interface IEventLogRepository
{
    Task<StoredEvent?> GetByCorrelationIdAsync(string correlationId);
    Task SaveAsync(StoredEvent storedEvent);
}