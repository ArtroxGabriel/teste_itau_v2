using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Entities;
using ItauCompraProgramada.Domain.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace ItauCompraProgramada.Infrastructure.Persistence.Repositories;

public class RecommendationBasketRepository(ItauDbContext dbContext) : IRecommendationBasketRepository
{
    public async Task<RecommendationBasket?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.RecommendationBaskets
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.IsActive, cancellationToken);
    }

    public async Task<List<RecommendationBasket>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.RecommendationBaskets
            .Include(b => b.Items)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RecommendationBasket basket, CancellationToken cancellationToken = default)
    {
        await dbContext.RecommendationBaskets.AddAsync(basket, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}