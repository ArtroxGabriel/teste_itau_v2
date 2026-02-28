using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace ItauCompraProgramada.Infrastructure.Persistence.Repositories;

public class ClientRepository(ItauDbContext dbContext) : IClientRepository
{
    public async Task<List<Client>> GetClientsForExecutionAsync(int executionDay)
    {
        return await dbContext.Clients
            .Include(c => c.GraphicAccount)
            .Where(c => c.IsActive && c.NextPurchaseDay == executionDay)
            .ToListAsync();
    }

    public async Task<Client?> GetByIdAsync(long id)
    {
        return await dbContext.Clients
            .Include(c => c.GraphicAccount)
                .ThenInclude(ga => ga.Custodies)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task AddAsync(Client client)
    {
        await dbContext.Clients.AddAsync(client);
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}