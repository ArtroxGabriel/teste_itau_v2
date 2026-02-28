using System.Linq;
using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace ItauCompraProgramada.Infrastructure.Persistence.Repositories;

public class RecommendationBasketRepository(ItauDbContext dbContext) : IRecommendationBasketRepository
{
    public async Task<RecommendationBasket?> GetActiveAsync()
    {
        return await dbContext.RecommendationBaskets
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.IsActive);
    }

    public async Task AddAsync(RecommendationBasket basket)
    {
        await dbContext.RecommendationBaskets.AddAsync(basket);
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}