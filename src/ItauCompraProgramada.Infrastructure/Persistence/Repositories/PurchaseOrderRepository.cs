using System.Linq;
using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ItauCompraProgramada.Infrastructure.Persistence.Repositories;

public class PurchaseOrderRepository(ItauDbContext dbContext) : IPurchaseOrderRepository
{
    public async Task AddAsync(PurchaseOrder purchaseOrder)
    {
        await dbContext.PurchaseOrders.AddAsync(purchaseOrder);
    }

    public async Task<decimal> GetTotalSalesValueInMonthAsync(long accountId, int year, int month)
    {
        return await dbContext.PurchaseOrders
            .Where(o => o.MasterAccountId == accountId && 
                        o.Quantity < 0 && 
                        o.ExecutionDate.Year == year && 
                        o.ExecutionDate.Month == month)
            .SumAsync(o => (decimal)Math.Abs(o.Quantity) * o.UnitPrice);
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}
