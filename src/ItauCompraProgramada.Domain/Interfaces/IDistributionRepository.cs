using System.Collections.Generic;
using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Entities;

namespace ItauCompraProgramada.Domain.Interfaces;

public interface IDistributionRepository
{
    Task<List<Distribution>> GetByClientIdAsync(long clientId);
    Task SaveChangesAsync();
}
