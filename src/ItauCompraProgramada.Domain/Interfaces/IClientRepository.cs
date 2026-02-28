using System.Threading.Tasks;
using ItauCompraProgramada.Domain.Entities;

namespace ItauCompraProgramada.Domain.Interfaces;

public interface IClientRepository
{
    Task AddAsync(Client client);
    Task SaveChangesAsync();
}
