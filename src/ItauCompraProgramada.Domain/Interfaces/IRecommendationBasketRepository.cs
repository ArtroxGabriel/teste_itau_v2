using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ItauCompraProgramada.Domain.Entities;

namespace ItauCompraProgramada.Domain.Interfaces;

public interface IRecommendationBasketRepository
{
    Task<RecommendationBasket?> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<List<RecommendationBasket>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(RecommendationBasket basket, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}