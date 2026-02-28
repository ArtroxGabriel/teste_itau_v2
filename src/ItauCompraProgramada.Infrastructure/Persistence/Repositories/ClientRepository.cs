using System.Threading.Tasks;
using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ItauCompraProgramada.Infrastructure.Persistence.Repositories;

public class ClientRepository(ItauDbContext dbContext) : IClientRepository
{
    public async Task AddAsync(Client client)
    {
        await dbContext.Clients.AddAsync(client);
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}
