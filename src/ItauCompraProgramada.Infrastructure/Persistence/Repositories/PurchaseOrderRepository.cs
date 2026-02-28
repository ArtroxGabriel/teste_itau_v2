using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;

namespace ItauCompraProgramada.Infrastructure.Persistence.Repositories;

public class PurchaseOrderRepository(ItauDbContext dbContext) : IPurchaseOrderRepository
{
    public async Task AddAsync(PurchaseOrder purchaseOrder)
    {
        await dbContext.PurchaseOrders.AddAsync(purchaseOrder);
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}