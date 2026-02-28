using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Entities;

namespace ItauCompraProgramada.Domain.Interfaces;

public interface IPurchaseOrderRepository
{
    Task AddAsync(PurchaseOrder purchaseOrder);
    Task SaveChangesAsync();
}