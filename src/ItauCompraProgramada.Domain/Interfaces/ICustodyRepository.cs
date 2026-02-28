using System.Collections.Generic;
using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Entities;

namespace ItauCompraProgramada.Domain.Interfaces;

public interface ICustodyRepository
{
    Task<List<Custody>> GetByAccountIdAsync(long accountId);
    Task AddAsync(Custody custody);
    Task SaveChangesAsync();
}